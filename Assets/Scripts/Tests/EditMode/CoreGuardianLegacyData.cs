using System.IO;
using UnityEngine;

// Frozen 2026-10-27 string-ID CSVs. Rule tests keep using them so balance/data edits do not
// silently change what those tests verify; the live numeric-ID tables are covered by
// CoreGuardianDataTableTests.
public static class CoreGuardianLegacyData
{
    public static string Text(string name)=>File.ReadAllText(Path.Combine(Application.dataPath,"Scripts/Tests/EditMode/Fixtures/LegacyData",name+".csv"));
}
