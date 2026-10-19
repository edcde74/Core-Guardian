from pathlib import Path
root=Path(__file__).resolve().parents[3]
def edit(rel,pairs):
 p=root/rel;s=p.read_bytes().decode('utf-8')
 for old,new in pairs:
  assert old in s,old[:90]
  s=s.replace(old,new)
 p.write_bytes(s.encode('utf-8'))
edit('Assets/Game/Tests/EditMode/CoreGuardianSessionTests.cs',[
 ('"Support,100","9,100"','"Support,75","9,75"'),
 ('Csv("waves").Replace("wave1_attack,1,","wave1_attack,2,")','Csv("waves").Replace(",1,Attack,",",2,Attack,")'),
 ('new[]{80,140,190,250,320,400}','new[]{40,65,85,110,140,175}'),
 ('s.PlayerUpgradePrice("damage"),Is.EqualTo(140)','s.PlayerUpgradePrice("damage"),Is.EqualTo(65)'),
 ('s.PlayerUpgradePrice("range"),Is.EqualTo(80)','s.PlayerUpgradePrice("range"),Is.EqualTo(40)'),
 ('Assert.That(s.Gold,Is.EqualTo(93));Assert.That(s.Crystals,Is.EqualTo(40))','Assert.That(s.Gold,Is.EqualTo(93));Assert.That(s.Crystals,Is.EqualTo(80))'),
 ('Assert.That(s.Gold,Is.EqualTo(13))','Assert.That(s.Gold,Is.EqualTo(53))'),
 ('Assert.That(s.UpgradeTower(t.Id,"damage",out reason),Is.True,reason);Assert.That(s.Crystals,Is.Zero)','Assert.That(s.UpgradeTower(t.Id,"damage",out reason),Is.True,reason);Assert.That(s.Crystals,Is.EqualTo(20))'),
 ('s.TowerUpgradePrice(t.Id,"damage"),Is.EqualTo(100)','s.TowerUpgradePrice(t.Id,"damage"),Is.EqualTo(90)'),
 ('s.TowerUpgradePrice(t.Id,"range"),Is.EqualTo(40)','s.TowerUpgradePrice(t.Id,"range"),Is.EqualTo(60)'),
 ('legacy.PlayerUpgradePrice(1),Is.EqualTo(100)','legacy.PlayerUpgradePrice(1),Is.EqualTo(50)'),
 ('s.TowerUpgradePrice(t.Id,"damage"),Is.EqualTo(80)','s.TowerUpgradePrice(t.Id,"damage"),Is.EqualTo(60)'),
 ('s.Tick(50.1f,Away)','s.Tick(s.Data.PreparationSeconds+.1f,Away)'),
 ('Assert.That(s.Crystals,Is.EqualTo(50))','Assert.That(s.Crystals,Is.EqualTo(70))'),
 ('crystals-s.Crystals,Is.EqualTo(40)','crystals-s.Crystals,Is.EqualTo(60)'),
 ('s.TowerUpgradePrice(b.Id,stat),Is.EqualTo(100)','s.TowerUpgradePrice(b.Id,stat),Is.EqualTo(90)'),
 ('s.TowerUpgradePrice(replacement.Id,stat),Is.EqualTo(100)','s.TowerUpgradePrice(replacement.Id,stat),Is.EqualTo(90)'),
 ('s.TowerUpgradePrice(reset.Id,stat),Is.EqualTo(40)','s.TowerUpgradePrice(reset.Id,stat),Is.EqualTo(60)'),
 ('t.Spent,Is.EqualTo(100)','t.Spent,Is.EqualTo(60)'),
 ('s.Crystals-before,Is.EqualTo(50)','s.Crystals-before,Is.EqualTo(30)'),
 ('Assert.That(s.Crystals,Is.Zero);Assert.That(a.Stats.Damage','Assert.That(s.Crystals,Is.EqualTo(40));Assert.That(a.Stats.Damage'),
 ('s.TowerUpgradePrice(a.Id,"damage"),Is.EqualTo(40)','s.TowerUpgradePrice(a.Id,"damage"),Is.EqualTo(60)'),
 ('int[] counts={16,17,17,18,18,19,20};int[] gold={50,54,55,61,61,65,71}','int[] counts={23,26,31,35,38,42,46};int[] gold={72,82,102,116,126,142,156}'),
 ('Is.LessThanOrEqualTo(15.21f)','Is.LessThanOrEqualTo(16f)'),
 ('d.Waves.Single(w=>w.Wave==11&&w.Kind==EnemyKind.Attack).Interval,Is.EqualTo(1.1f)','d.Waves.First(w=>w.Wave==11&&w.Kind==EnemyKind.Attack).Interval,Is.EqualTo(.34f)'),
 ('schedule.Sum(w=>w.Count),Is.EqualTo(15)','schedule.Sum(w=>w.Count),Is.EqualTo(38)')])
edit('Assets/Game/Tests/EditMode/CoreGuardianRoundFlowTests.cs',[
 ('PrototypeIncludesBossTenAndFullElevenSchedule','PrototypeIncludesThirtyRoundsAndAllBossMilestones'),
 ('d.Waves.Max(w=>w.Wave),Is.EqualTo(11)','d.Waves.Max(w=>w.Wave),Is.EqualTo(30)'),
 ('w.Wave==10).Sum(w=>w.Count),Is.EqualTo(15)','w.Wave==10).Sum(w=>w.Count),Is.EqualTo(38)'),
 ('w.Wave==11).Sum(w=>w.Count),Is.EqualTo(21)','w.Wave==11).Sum(w=>w.Count),Is.EqualTo(45)'),
 ('d.RewardForWave(10),Is.EqualTo(35)','d.RewardForWave(10),Is.EqualTo(70)'),
 ('d.RewardForWave(11),Is.EqualTo(30)','d.RewardForWave(11),Is.EqualTo(50)'),
 ('var d=Data();d.Waves.RemoveAll(w=>w.Wave>9);var s=Game(d);int[] rewards={20,20,20,20,25,25,25,25,25};','var d=Data();d.AugmentationEvery=10;d.Waves.RemoveAll(w=>w.Wave>9);var expectedGold=d.Waves.Sum(w=>w.Count*(w.Enemy??d.Enemies[w.Kind]).Gold);var s=Game(d);int[] rewards={40,40,40,40,45,45,45,45,45};'),
 ('s.Gold,Is.EqualTo(510)','s.Gold,Is.EqualTo(expectedGold)'),
 ('s.Crystals,Is.EqualTo(305)','s.Crystals,Is.EqualTo(485)'),
 ('crystals+20','crystals+d.RewardForWave(1)'),
 ('Is.InRange(48f,50f)','Is.InRange(d.PreparationSeconds-2,d.PreparationSeconds)'),
 ('s.Tick(50.1f,Away)','s.Tick(d.PreparationSeconds+.1f,Away)')])
edit('Assets/Game/Tests/EditMode/CoreGuardianAugmentationTests.cs',[
 ('static CombatData Data()=>CombatData.Load(Resources.Load<TextAsset>("CoreGuardian/enemies").text,Resources.Load<TextAsset>("CoreGuardian/towers").text,Resources.Load<TextAsset>("CoreGuardian/stage").text,Resources.Load<TextAsset>("CoreGuardian/upgrades").text,Resources.Load<TextAsset>("CoreGuardian/waves").text);','// These card/boss-gate regression fixtures deliberately select the supported legacy cadence. Current five-round progression is tested in CoreGuardianThirtyRoundTests.\n    static CombatData Data(){var d=CombatData.Load(Resources.Load<TextAsset>("CoreGuardian/enemies").text,Resources.Load<TextAsset>("CoreGuardian/towers").text,Resources.Load<TextAsset>("CoreGuardian/stage").text,Resources.Load<TextAsset>("CoreGuardian/upgrades").text,Resources.Load<TextAsset>("CoreGuardian/waves").text);d.AugmentationEvery=10;d.AugmentationRequiresBoss=true;d.Waves.RemoveAll(w=>w.Wave>11);return d;}'),
 ('s.Crystals,Is.EqualTo(340)','s.Crystals,Is.EqualTo(555)'),
 ('s.PreparationRemaining,Is.EqualTo(50)','s.PreparationRemaining,Is.EqualTo(s.Data.PreparationSeconds)'),
 ('Is.InRange(49.9f,50)','Is.InRange(s.Data.PreparationSeconds-.1f,s.Data.PreparationSeconds)'),
 ('s.Crystals,Is.EqualTo(135)','s.Crystals,Is.EqualTo(155)'),
 ('s.Crystals,Is.EqualTo(155));s.StartDiagnosticEncounter()','s.Crystals,Is.EqualTo(195));s.StartDiagnosticEncounter()')])
