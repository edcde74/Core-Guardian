param([ValidateSet('install','remove')] [string]$Mode)
$ErrorActionPreference='Stop'
$root='docs/development/view_sync_20261023'
$p='Assets/Scripts/Runtime/Bootstrap/CoreGuardianPrototype.cs'
$s='Assets/Scripts/Runtime/Domain/Rules/Session.cs'
if($Mode -eq 'remove'){
 Copy-Item "$root/diagnostic_prototype_restore.cs" $p
 Copy-Item "$root/diagnostic_session_restore.cs" $s
 'Diagnostic source removed byte-for-byte.'
 exit
}
Copy-Item $p "$root/diagnostic_prototype_restore.cs"
Copy-Item $s "$root/diagnostic_session_restore.cs"
$text=[IO.File]::ReadAllText((Join-Path $pwd $p)).Replace("`r`n","`n")
$text=$text.Replace("    public Vector3 World(Vector2 tile)`n    {","    public Vector3 World(Vector2 tile)`n    {`n        if(CoreGuardian.Session.InViews)CoreGuardian.Session.ViewCounts[0]++;")
$text=$text.Replace("        using(SyncMarker.Auto())","        CoreGuardian.Session.InViews=true;System.Array.Clear(CoreGuardian.Session.ViewCounts,0,5);`n        using(SyncMarker.Auto())")
$text=$text.Replace("        combatAnimation.Player();`n        }","        combatAnimation.Player();`n        }`n        CoreGuardian.Session.InViews=false;")
[IO.File]::WriteAllText((Join-Path $pwd $p),$text,[Text.UTF8Encoding]::new($false))
$text=[IO.File]::ReadAllText((Join-Path $pwd $s)).Replace("`r`n","`n")
$text=$text.Replace("    public sealed partial class Session : IDisposable`n    {","    public sealed partial class Session : IDisposable`n    {`n        public static bool InViews;public static readonly int[] ViewCounts=new int[5];")
$text=$text.Replace("        public TowerState FindTower(int id)`n        {","        public TowerState FindTower(int id)`n        {`n            if(InViews){ViewCounts[1]++;ViewCounts[4]+=Towers.Count;}")
$text=$text.Replace("            bool Matches(IReadOnlyList<EnemyHandle> enemies)`n            {","            bool Matches(IReadOnlyList<EnemyHandle> enemies)`n            {`n                if(InViews)ViewCounts[2]++;")
$text=$text.Replace('{supportCache.Refresh(Towers);return supportCache.Get(Towers,tower.Position).InRange;}','{if(InViews)ViewCounts[3]++;supportCache.Refresh(Towers);return supportCache.Get(Towers,tower.Position).InRange;}')
[IO.File]::WriteAllText((Join-Path $pwd $s),$text,[Text.UTF8Encoding]::new($false))
$text=[IO.File]::ReadAllText((Join-Path $pwd "$root/Benchmark.cs"))
$text=$text.Replace('CGViewBenchmark','CGViewDiagnostic').Replace('Start("before_"+repetition)','Start("diagnostic_before")').Replace('Start("after_"+repetition)','Start("diagnostic_after")').Replace('warm=15;','warm=0;').Replace('frames.Count<90','frames.Count<15').Replace('samples=90','samples=15').Replace('warm=15,samples=15','warm=0,samples=15').Replace('frames.Add(new{frame=last,','frames.Add(new{viewCounts=((int[])typeof(Session).GetField("ViewCounts").GetValue(null)).ToArray(),frame=last,')
[IO.File]::WriteAllText((Join-Path $pwd "$root/Diagnostic.cs"),$text,[Text.UTF8Encoding]::new($false))
'Separate 15-frame counter source ready; performance runs have no counter increments.'
