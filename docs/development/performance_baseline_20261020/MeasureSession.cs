        if(UnityEditor.EditorApplication.isPlaying||UnityEditor.EditorApplication.isCompiling)throw new InvalidOperationException("Idle editor required");
        var root=UnityEngine.Object.FindFirstObjectByType<CoreGuardianPrototype>();
        var csv=new[]{root.enemiesCsv.text,root.towersCsv.text,root.stageCsv.text,root.upgradesCsv.text,root.wavesCsv.text};
        var output=new List<object>();
        foreach(int count in new[]{32,64,128,256})
        {
            var times=new List<double>();var allocations=new List<long>();
            for(int repeat=0;repeat<3;repeat++)
            {
                var data=CoreGuardian.CombatData.Load(csv[0],csv[1],csv[2],csv[3],csv[4]);
                data.StartCrystals=100000;data.CoreEnergy=100000;
                foreach(var e in data.Enemies.Values)e.Health=100000;
                data.Waves.Clear();data.Waves.Add(new CoreGuardian.WaveSpec{Wave=1,Kind=CoreGuardian.EnemyKind.Attack,Count=1,Start=100000,Interval=1});
                var session=new CoreGuardian.Session(data,17);session.SetBoard(40,25,new Vector2Int(39,12),new[]{new Vector2Int(0,12),new Vector2Int(39,12)});
                for(int i=0;i<32;i++){string reason;session.TryInstall(i%5==4?CoreGuardian.TowerKind.Support:CoreGuardian.TowerKind.Multi,new Vector2Int(1+i/4,new[]{10,11,13,14}[i%4]),out reason);}
                for(int i=0;i<count;i++)session.DebugSpawn(i%8==0?CoreGuardian.EnemyKind.Buff:CoreGuardian.EnemyKind.Attack,1+(i%20)*.5f);
                session.StartCombat();for(int i=0;i<10;i++)session.Tick(1f/60,new Vector2(8,12));
                long before=GC.GetAllocatedBytesForCurrentThread();var watch=System.Diagnostics.Stopwatch.StartNew();
                for(int i=0;i<180;i++)session.Tick(1f/60,new Vector2(8,12));
                watch.Stop();allocations.Add((GC.GetAllocatedBytesForCurrentThread()-before)/180);times.Add(watch.Elapsed.TotalMilliseconds/180);
            }
            times.Sort();allocations.Sort();output.Add(new{enemies=count,buff=count/8,towers=32,ticks=180,repeats=3,medianMsPerTick=times[1],medianAllocatedBytesPerTick=allocations[1],allMs=times,allBytes=allocations});
        }
        var result=new{kind="isolated real CoreGuardian.Session CPU/allocation stress diagnostic; synthetic straight board, injected resources/HP/enemies; no rendering, not balance or frame FPS",seed=17,step=1f/60,rows=output};
        System.IO.File.WriteAllText("docs/development/performance_baseline_20261020/session.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
