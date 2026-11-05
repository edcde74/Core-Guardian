using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

/// <summary>
/// Refactoring guard: our shaders must compile to exactly the same D3D bytecode and keep the same
/// property block. Formatting, comments and local renames do not change the bytecode, so any
/// difference means the rendered pixels may differ. If a URP package update changes the baseline,
/// regenerate it with <see cref="Fingerprint"/> only after confirming the pixels are unchanged.
/// </summary>
public class CoreGuardianShaderBytecodeTests
{
    public const string BaselinePath = "Assets/Scripts/Tests/EditMode/Fixtures/ShaderBytecodeBaseline.txt";

    public static readonly string[] ShaderNames =
    {
        "BatchedMaskOutput",
        "BatchedSelectionMask",
        "BodyHitFlash",
        "CoreAmbient",
        "PhotoShadowContact",
        "SelectionMask",
        "SelectionOutline",
        "SharedOutlineMask",
        "SharedOutlineOutput",
        "TowerBaseShade",
        "TowerGlow",
        "UpgradeScan",
    };

    static readonly ShaderType[] Stages = { ShaderType.Vertex, ShaderType.Fragment };

    [Test]
    public void OurShadersCompileToTheBaselineBytecodeAndProperties()
    {
        Assert.That(File.Exists(BaselinePath), Is.True, "Missing baseline " + BaselinePath);
        var expected = File.ReadAllText(BaselinePath).Replace("\r\n", "\n").TrimEnd();
        var actual = Fingerprint().TrimEnd();
        if (expected == actual)
            return;

        var expectedLines = expected.Split('\n');
        var actualLines = actual.Split('\n');
        var differences = new StringBuilder();
        var count = Math.Max(expectedLines.Length, actualLines.Length);
        for (var i = 0; i < count; i++)
        {
            var e = i < expectedLines.Length ? expectedLines[i] : "<none>";
            var a = i < actualLines.Length ? actualLines[i] : "<none>";
            if (e != a)
            {
                differences.AppendLine("expected: " + e).AppendLine("actual:   " + a);
            }
        }

        Assert.Fail("Shader bytecode or properties changed:\n" + differences);
    }

    /// <summary>One line per shader property and per compiled stage/keyword variant.</summary>
    public static string Fingerprint()
    {
        var text = new StringBuilder();
        foreach (var name in ShaderNames)
        {
            var shader = Resources.Load<Shader>("CoreGuardian/" + name);
            Assert.That(shader, Is.Not.Null, "Missing shader CoreGuardian/" + name);
            AppendProperties(text, name, shader);
            AppendVariants(text, name, shader);
        }

        return text.ToString();
    }

    static void AppendProperties(StringBuilder text, string name, Shader shader)
    {
        for (var i = 0; i < shader.GetPropertyCount(); i++)
        {
            text.Append(name)
                .Append(" prop ")
                .Append(shader.GetPropertyName(i))
                .Append(' ')
                .Append(shader.GetPropertyType(i))
                .Append(' ')
                .Append(shader.GetPropertyFlags(i))
                .Append('\n');
        }
    }

    static void AppendVariants(StringBuilder text, string name, Shader shader)
    {
        var keywordSets = KeywordSets(shader);
        var data = ShaderUtil.GetShaderData(shader);
        for (var s = 0; s < data.SerializedSubshaderCount; s++)
        {
            var subshader = data.GetSerializedSubshader(s);
            for (var p = 0; p < subshader.PassCount; p++)
            {
                var pass = subshader.GetPass(p);
                foreach (var stage in Stages)
                {
                    foreach (var keywords in keywordSets)
                    {
                        var result = pass.CompileVariant(
                            stage,
                            keywords,
                            ShaderCompilerPlatform.D3D,
                            BuildTarget.StandaloneWindows64
                        );
                        text.Append(name)
                            .Append(" s")
                            .Append(s)
                            .Append(" p")
                            .Append(p)
                            .Append(' ')
                            .Append(stage)
                            .Append(" [")
                            .Append(string.Join(",", keywords))
                            .Append("] ")
                            .Append(result.Success ? Hash(result.ShaderData) : "compile-failed")
                            .Append('\n');
                    }
                }
            }
        }
    }

    static List<string[]> KeywordSets(Shader shader)
    {
        var sets = new List<string[]> { Array.Empty<string>() };
        foreach (var keyword in shader.keywordSpace.keywords)
        {
            // Stereo/XR keywords are never enabled in this 2D Windows game.
            if (
                keyword.name.StartsWith("STEREO_", StringComparison.Ordinal)
                || keyword.name.StartsWith("UNITY_SINGLE_PASS", StringComparison.Ordinal)
            )
            {
                continue;
            }

            sets.Add(new[] { keyword.name });
        }

        sets.Sort((a, b) => string.CompareOrdinal(string.Join(",", a), string.Join(",", b)));
        return sets;
    }

    static string Hash(byte[] bytes)
    {
        using (var sha = SHA256.Create())
        {
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", string.Empty);
        }
    }
}
