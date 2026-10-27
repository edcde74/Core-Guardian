using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace CoreGuardian
{
    // Storage never escapes the assembly. A handle pins an identity, not a lifetime.
    internal abstract class ModelSlot
    {
        internal Session Owner;internal ulong Generation;internal bool Active,Pending;
        internal abstract void Clear();
    }
    internal sealed class EnemyState : ModelSlot
    {
        public EnemyState(){}internal int Id,Health;internal EnemyKind Kind,PoolKind;internal Vector2 Position;internal float PathTravelled,PathRemaining,SlowRemaining;internal bool Buffed,Exited;internal EnemySpec Spec;
        // Status effects (전투 피해 계산 규칙 효과 태그). SlowRemaining is the player-hit slow timer.
        internal float SlowPlayer,SlowFrost,SlowFrostTime,BurnTime,BurnPerStack,BurnTick,BurnCarry,StunTime,StunImmune,FrozenDamage,AbilityTimer,ShieldTime;
        internal int BurnStacks,ShieldUsed,Route,SummonerId;internal bool Frozen,BurnKill;internal string BurnSource;
        internal override void Clear(){Id=Health=0;Kind=default;Position=default;PathTravelled=PathRemaining=SlowRemaining=0;Buffed=Exited=false;Spec=null;
            SlowPlayer=SlowFrost=SlowFrostTime=BurnTime=BurnPerStack=BurnTick=BurnCarry=StunTime=StunImmune=FrozenDamage=AbilityTimer=ShieldTime=0;
            BurnStacks=ShieldUsed=Route=SummonerId=0;Frozen=BurnKill=false;BurnSource=null;}
    }
    internal sealed class ProjectileState : ModelSlot
    {
        public ProjectileState(){}internal int Id;internal Vector2 Position;internal EnemyHandle Target;internal EnemySnapshot LastTarget;internal string Source;internal float Damage,Speed;
        // Line mode (관통): flies straight for Remaining tiles and hits each enemy once per pass.
        internal bool Line,Reflected;internal Vector2 Direction;internal float Remaining,Range;internal int HitsLeft;internal HitPayload Payload;
        internal readonly System.Collections.Generic.List<int> Hits=new System.Collections.Generic.List<int>(8);
        internal override void Clear(){Id=0;Position=default;Target=default;LastTarget=default;Source=null;Damage=Speed=0;Line=Reflected=false;Direction=default;Remaining=Range=0;HitsLeft=0;Payload=default;Hits.Clear();}
    }
    public readonly struct EnemyHandle : IEquatable<EnemyHandle>
    {
        internal readonly EnemyState Slot;internal readonly ulong Generation;readonly int id;
        internal EnemyHandle(EnemyState slot){Slot=slot;Generation=slot.Generation;id=slot.Id;}
        public Session Owner=>Slot?.Owner;
        public bool IsValid=>Slot!=null&&Slot.Generation==Generation&&Slot.Active;
        internal bool Accessible=>Slot!=null&&Slot.Generation==Generation&&(Slot.Active||Slot.Pending);
        EnemyState Read=>Accessible?Slot:throw new InvalidOperationException("Expired enemy handle");
        EnemyState Write=>Owner?.RequireEnemyWrite(this)??throw new InvalidOperationException("Invalid enemy handle");
        public int Id=>id;
        public int Health {get=>Read.Health;set=>Write.Health=value;}
        public EnemyKind Kind {get=>Read.Kind;set=>Write.Kind=value;}
        public Vector2 Position {get=>Read.Position;set=>Write.Position=value;}
        public float PathTravelled {get=>Read.PathTravelled;set=>Write.PathTravelled=value;}
        public float PathRemaining {get=>Read.PathRemaining;set=>Write.PathRemaining=value;}
        public float SlowRemaining {get=>Read.SlowRemaining;set=>Write.SlowRemaining=value;}
        public bool Buffed {get=>Read.Buffed;set=>Write.Buffed=value;}
        public bool Exited {get=>Read.Exited;set=>Write.Exited=value;}
        public EnemySpec Spec {get=>Read.Spec;set=>Write.Spec=value;}
        public bool Alive=>Accessible&&Slot.Health>0&&!Slot.Exited;
        public bool Enraged=>Kind==EnemyKind.Boss&&Health<=Spec.Health*Spec.RagePercent/100f;
        public float MovementSpeed=>Read.StunTime>0?0:Spec.Speed*(Enraged?1+Spec.RageBoost/100f:1)*(1-Owner.SlowFraction(Slot));
        public int BurnStacks=>Read.BurnStacks;
        public bool Stunned=>Read.StunTime>0;
        public bool Frozen=>Read.Frozen;
        public bool Shielded=>Read.ShieldTime>0;
        public bool Flying=>Read.Route==1;
        public bool Summoned=>Read.SummonerId!=0;
        public float SlowPercent=>Owner.SlowFraction(Read)*100;
        public bool Equals(EnemyHandle other)=>ReferenceEquals(Slot,other.Slot)&&Generation==other.Generation;
        public override bool Equals(object obj)=>obj is EnemyHandle other&&Equals(other);
        public override int GetHashCode()=>((Slot?.GetHashCode()??0)*397)^Generation.GetHashCode();
        public static bool operator ==(EnemyHandle a,EnemyHandle b)=>a.Equals(b);
        public static bool operator !=(EnemyHandle a,EnemyHandle b)=>!a.Equals(b);
    }
    public readonly struct ProjectileHandle : IEquatable<ProjectileHandle>
    {
        internal readonly ProjectileState Slot;internal readonly ulong Generation;readonly int id;
        internal ProjectileHandle(ProjectileState slot){Slot=slot;Generation=slot.Generation;id=slot.Id;}
        public Session Owner=>Slot?.Owner;
        public bool IsValid=>Slot!=null&&Slot.Generation==Generation&&Slot.Active;
        internal bool Accessible=>Slot!=null&&Slot.Generation==Generation&&(Slot.Active||Slot.Pending);
        ProjectileState Read=>Accessible?Slot:throw new InvalidOperationException("Expired projectile handle");
        ProjectileState Write=>Owner?.RequireProjectileWrite(this)??throw new InvalidOperationException("Invalid projectile handle");
        public int Id=>id;
        public Vector2 Position {get=>Read.Position;set=>Write.Position=value;}
        public EnemyHandle Target
        {
            get=>Read.Target;
            set{var slot=Write;if(value!=default&&!ReferenceEquals(value.Owner,Owner))throw new ArgumentException("Foreign target");slot.Target=value;slot.LastTarget=value.Accessible?new EnemySnapshot(value):default;}
        }
        public EnemySnapshot TargetSnapshot=>Target.Accessible?new EnemySnapshot(Target):Read.LastTarget;
        public Vector2 TargetPosition=>Read.Line?Read.Position+Read.Direction:Target.Accessible?Target.Position:Read.LastTarget.Position;
        public bool Line=>Read.Line;
        public Vector2 Direction=>Read.Direction;
        public string Source {get=>Read.Source;set=>Write.Source=value;}
        public float Damage {get=>Read.Damage;set=>Write.Damage=value;}
        public float Speed {get=>Read.Speed;set=>Write.Speed=value;}
        public bool Equals(ProjectileHandle other)=>ReferenceEquals(Slot,other.Slot)&&Generation==other.Generation;
        public override bool Equals(object obj)=>obj is ProjectileHandle other&&Equals(other);
        public override int GetHashCode()=>((Slot?.GetHashCode()??0)*397)^Generation.GetHashCode();
        public static bool operator ==(ProjectileHandle a,ProjectileHandle b)=>a.Equals(b);
        public static bool operator !=(ProjectileHandle a,ProjectileHandle b)=>!a.Equals(b);
    }
    public readonly struct EnemySpecSnapshot
    {
        public readonly int Health,Gold,CoreDamage;public readonly float Speed,Defense,BuffRadius,BuffSpeed;
        internal EnemySpecSnapshot(EnemySpec spec){Health=spec.Health;Gold=spec.Gold;CoreDamage=spec.CoreDamage;Speed=spec.Speed;Defense=spec.Defense;BuffRadius=spec.BuffRadius;BuffSpeed=spec.BuffSpeed;}
    }
    public readonly struct EnemySnapshot
    {
        public readonly EnemyHandle Handle;public readonly int Id,Health;public readonly EnemyKind Kind;public readonly Vector2 Position;public readonly float PathTravelled,PathRemaining,SlowRemaining;public readonly bool Buffed,Exited,Alive,Enraged,HasSpec;public readonly EnemySpecSnapshot Spec;
        internal EnemySnapshot(EnemyHandle enemy){Handle=enemy;Id=enemy.Id;Health=enemy.Health;Kind=enemy.Kind;Position=enemy.Position;PathTravelled=enemy.PathTravelled;PathRemaining=enemy.PathRemaining;SlowRemaining=enemy.SlowRemaining;Buffed=enemy.Buffed;Exited=enemy.Exited;Alive=enemy.Alive;var spec=enemy.Spec;HasSpec=spec!=null;Enraged=Kind==EnemyKind.Boss&&HasSpec&&Health<=spec.Health*spec.RagePercent/100f;Spec=HasSpec?new EnemySpecSnapshot(spec):default;}
    }
    // Effects fixed when an attack is launched (치명타·버프 시점).
    internal struct HitPayload
    {
        public float Crit,BurnPerStack,BurnDuration,SlowPercent,SlowDuration,Execute,PierceFalloff,ChainFalloff,ExtraLine;
        public int BurnStacks,Chain,ExtraPierce;public bool Player,Skill,NoBounce,PierceGrowth,CritNoFalloff,Rail;
    }
    public readonly struct ProjectileSnapshot
    {
        public readonly ProjectileHandle Handle;public readonly int Id;public readonly Vector2 Position;public readonly EnemySnapshot Target;public readonly bool TargetIsValid;public readonly string Source;public readonly float Damage,Speed;
        public readonly bool Line;public readonly Vector2 Direction;
        internal ProjectileSnapshot(ProjectileHandle shot){Handle=shot;Id=shot.Id;Position=shot.Position;Target=shot.TargetSnapshot;TargetIsValid=shot.Target.IsValid;Source=shot.Source;Damage=shot.Damage;Speed=shot.Speed;Line=shot.Line;Direction=shot.Direction;}
    }
    // Public enumeration is read-only; all mutations remain assembly-internal.
    public sealed class ModelList<T> : IReadOnlyList<T>
    {
        readonly List<T> values=new List<T>();
        // Bumped by every membership/order change; lets cached views skip re-validation when nothing changed.
        internal int Version;
        public int Count=>values.Count;public T this[int index]{get=>values[index];internal set{values[index]=value;Version++;}}
        public List<T>.Enumerator GetEnumerator()=>values.GetEnumerator();
        IEnumerator<T> IEnumerable<T>.GetEnumerator()=>values.GetEnumerator();IEnumerator IEnumerable.GetEnumerator()=>values.GetEnumerator();
        internal void Add(T value){values.Add(value);Version++;}internal void AddRange(IEnumerable<T> source){values.AddRange(source);Version++;}
        internal void AddRange(ModelList<T> source){values.AddRange(source.values);Version++;}
        internal bool Remove(T value){Version++;return values.Remove(value);}internal void RemoveRange(int index,int count){values.RemoveRange(index,count);Version++;}
        internal void Clear(){values.Clear();Version++;}internal void Reverse(){values.Reverse();Version++;}
    }
    public readonly struct ModelPoolCounters
    {
        public readonly int Active,Retained,PeakActive;public readonly long Created,Reused;
        internal ModelPoolCounters(int active,int retained,int peak,long created,long reused){Active=active;Retained=retained;PeakActive=peak;Created=created;Reused=reused;}
    }
    internal sealed class ModelPool<T> where T:ModelSlot,new()
    {
        readonly Session owner;readonly Stack<T> free=new Stack<T>();readonly List<T> pending=new List<T>();readonly int limit;
        int active,peak;long created,reused;
        internal ModelPool(Session owner,int limit){this.owner=owner;this.limit=limit;}
        internal T Rent()
        {
            T slot=null;while(free.Count>0&&slot==null){var candidate=free.Pop();if(candidate.Generation<ulong.MaxValue)slot=candidate;}
            if(slot==null){slot=new T{Owner=owner};created++;}else reused++;
            slot.Generation++;slot.Active=true;slot.Pending=false;active++;peak=Math.Max(peak,active);return slot;
        }
        internal void Return(T slot)
        {
            if(!ReferenceEquals(slot.Owner,owner)||!slot.Active)return;
            slot.Active=false;slot.Pending=true;active--;pending.Add(slot);
        }
        internal void Flush()
        {
            foreach(var slot in pending){slot.Clear();slot.Pending=false;if(free.Count<limit&&slot.Generation<ulong.MaxValue)free.Push(slot);}
            pending.Clear();
        }
        internal void Discard(){Flush();free.Clear();}
        internal ModelPoolCounters Counters=>new ModelPoolCounters(active,free.Count,peak,created,reused);
    }
}
