using System;
using System.Collections.Generic;
using UnityEngine;
namespace CoreGuardian
{
    public sealed partial class Session
    {
        ModelPool<EnemyState>[] enemyPools;ModelPool<ProjectileState> projectilePool;
        readonly Dictionary<ProjectileHandle,int> projectileMembership=new Dictionary<ProjectileHandle,int>();
        int operationDepth;bool disposed,retiredEnemy;
        // Bumped by every path that can change enemy state (handle writes, direct slot writes, model operations, pooling).
        int enemyRevision;
        void InitializeModelPools()
        {
            enemyPools=new ModelPool<EnemyState>[4];for(int i=0;i<4;i++)enemyPools[i]=new ModelPool<EnemyState>(this,i==3?4:32);
            projectilePool=new ModelPool<ProjectileState>(this,256);
        }
        void BeginModelOperation(){if(disposed)throw new ObjectDisposedException(nameof(Session));operationDepth++;enemyRevision++;}
        void EndModelOperation()
        {
            if(--operationDepth!=0)return;
            try
            {
                if(retiredEnemy)
                {
                    // Observers/nested ticks may move a dying target after an earlier shot
                    // was processed. Preserve that final position before clearing its slot.
                    foreach(var shot in Projectiles)if(shot.Accessible&&shot.Target.Accessible)shot.Slot.LastTarget=new EnemySnapshot(shot.Target);
                }
            }
            finally
            {
                enemyRevision++;retiredEnemy=false;foreach(var pool in enemyPools)pool.Flush();projectilePool.Flush();
                if(disposed){foreach(var pool in enemyPools)pool.Discard();projectilePool.Discard();}
            }
        }
        internal EnemyState RequireEnemyWrite(EnemyHandle handle)
        {
            if(disposed||!ReferenceEquals(handle.Owner,this)||!handle.IsValid)throw new InvalidOperationException("Expired or foreign enemy handle");enemyRevision++;return handle.Slot;
        }
        internal ProjectileState RequireProjectileWrite(ProjectileHandle handle)
        {
            if(disposed||!ReferenceEquals(handle.Owner,this)||!handle.IsValid)throw new InvalidOperationException("Expired or foreign projectile handle");return handle.Slot;
        }
        public bool TryRead(EnemyHandle handle,out EnemySnapshot snapshot)
        {if(!disposed&&ReferenceEquals(handle.Owner,this)&&handle.IsValid){snapshot=new EnemySnapshot(handle);return true;}snapshot=default;return false;}
        public bool TryRead(ProjectileHandle handle,out ProjectileSnapshot snapshot)
        {if(!disposed&&ReferenceEquals(handle.Owner,this)&&handle.IsValid){snapshot=new ProjectileSnapshot(handle);return true;}snapshot=default;return false;}
        public bool TrySetHealth(EnemyHandle handle,int health)
        {if(disposed||!ReferenceEquals(handle.Owner,this)||!handle.IsValid)return false;enemyRevision++;handle.Slot.Health=health;return true;}
        public bool TryUpdate(EnemyHandle handle,Action<EnemyHandle> update)
        {if(disposed||!ReferenceEquals(handle.Owner,this)||!handle.IsValid)return false;if(update==null)throw new ArgumentNullException(nameof(update));update(handle);return true;}
        public bool TryUpdate(ProjectileHandle handle,Action<ProjectileHandle> update)
        {if(disposed||!ReferenceEquals(handle.Owner,this)||!handle.IsValid)return false;if(update==null)throw new ArgumentNullException(nameof(update));update(handle);return true;}
        public ModelPoolCounters EnemyPool(EnemyKind kind)=>enemyPools[(int)kind].Counters;
        public ModelPoolCounters ProjectilePool=>projectilePool.Counters;
        EnemyHandle RentEnemy(EnemyKind kind,EnemySpec spec,float distance)
        {
            if(disposed)throw new ObjectDisposedException(nameof(Session));
            enemyRevision++;var pool=enemyPools[(int)kind];var slot=pool.Rent();
            try
            {
                slot.PoolKind=kind;slot.Id=nextEnemy++;slot.Kind=kind;slot.Spec=spec;slot.Health=spec.Health;slot.PathTravelled=Mathf.Clamp(distance,0,pathLength);
                var handle=new EnemyHandle(slot);PositionEnemy(handle);Enemies.Add(handle);return handle;
            }
            catch{pool.Return(slot);if(operationDepth==0)pool.Flush();throw;}
        }
        ProjectileHandle RentProjectile(EnemyHandle target,Vector2 position,float damage,float speed,string source)
        {
            var slot=projectilePool.Rent();ProjectileHandle handle=default;
            try
            {
                slot.Id=nextProjectile++;slot.Target=target;slot.Position=position;slot.Damage=damage;slot.Speed=speed;slot.Source=source;
                slot.LastTarget=target.Accessible?new EnemySnapshot(target):default;
                handle=new ProjectileHandle(slot);Projectiles.Add(handle);projectileMembership.Add(handle,1);return handle;
            }
            catch{Projectiles.Remove(handle);projectileMembership.Remove(handle);projectilePool.Return(slot);if(operationDepth==0)projectilePool.Flush();throw;}
        }
        void RetireEnemy(EnemyHandle handle)
        {if(ReferenceEquals(handle.Owner,this)&&handle.IsValid){enemyRevision++;enemyPools[(int)handle.Slot.PoolKind].Return(handle.Slot);retiredEnemy=true;}}
        void ReleaseProjectileMembership(ProjectileHandle handle)
        {
            if(!projectileMembership.TryGetValue(handle,out int count))return;
            if(count>1){projectileMembership[handle]=count-1;return;}
            projectileMembership.Remove(handle);if(handle.IsValid)projectilePool.Return(handle.Slot);
        }
        void RemoveProjectile(ProjectileHandle handle)
        {if(Projectiles.Remove(handle))ReleaseProjectileMembership(handle);}
        void ClearProjectiles()
        {foreach(var handle in Projectiles)ReleaseProjectileMembership(handle);Projectiles.Clear();}
        void RemoveDeadEnemies()
        {
            int write=0,count=Enemies.Count;
            for(int read=0;read<count;read++){var enemy=Enemies[read];if(!enemy.Alive){RetireEnemy(enemy);continue;}if(read!=write)Enemies[write]=enemy;write++;}
            if(write<count)Enemies.RemoveRange(write,count-write);
        }
        void ClearEnemies(){foreach(var enemy in Enemies)RetireEnemy(enemy);Enemies.Clear();}
        public void DebugClearProjectiles(){BeginModelOperation();try{ClearProjectiles();}finally{EndModelOperation();}}
        public bool DebugRemoveEnemy(EnemyHandle enemy)
        {
            BeginModelOperation();try{if(!Enemies.Remove(enemy))return false;RetireEnemy(enemy);return true;}finally{EndModelOperation();}
        }
        public void DebugReverseEnemies(){if(disposed)throw new ObjectDisposedException(nameof(Session));enemyRevision++;Enemies.Reverse();}
        public ProjectileHandle DebugProjectile(EnemyHandle target,Vector2 position,float damage=1,float speed=0,string source="Test")
        {if(disposed)throw new ObjectDisposedException(nameof(Session));if(target!=default&&!ReferenceEquals(target.Owner,this))throw new ArgumentException("Foreign target");return RentProjectile(target,position,damage,speed,source);}
        public void DebugAddProjectile(ProjectileHandle handle)
        {
            RequireProjectileWrite(handle);Projectiles.Add(handle);projectileMembership.TryGetValue(handle,out int count);projectileMembership[handle]=count+1;
        }
        public void DebugReplaceProjectiles(IEnumerable<ProjectileHandle> handles)
        {
            BeginModelOperation();try
            {
                var desired=new List<ProjectileHandle>(handles);var retained=new HashSet<ProjectileHandle>(desired);
                foreach(var handle in desired)RequireProjectileWrite(handle);
                foreach(var handle in Projectiles)if(!retained.Contains(handle))ReleaseProjectileMembership(handle);
                Projectiles.Clear();projectileMembership.Clear();
                foreach(var handle in desired){Projectiles.Add(handle);projectileMembership.TryGetValue(handle,out int count);projectileMembership[handle]=count+1;}
            }
            finally{EndModelOperation();}
        }
        public void Dispose()
        {
            if(disposed)return;
            operationDepth++;
            try
            {
                ClearProjectiles();ClearEnemies();playerTarget=default;foreach(var tower in Towers)tower.Target=default;
                EndTowerViews();towerStamps.Clear();towerLookup.Clear();sourceIds.Clear();disposed=true;
                DamageApplied=null;ProjectileLaunched=null;AttackLaunched=null;DefeatReached=null;CombatStarted=null;RoundCompleted=null;VictoryReached=null;
            }
            finally{EndModelOperation();}
        }
    }
}
