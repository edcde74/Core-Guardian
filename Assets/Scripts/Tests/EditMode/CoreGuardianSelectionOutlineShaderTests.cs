using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class CoreGuardianSelectionOutlineShaderTests
{
    // Candidate is held after the matched frame comparison worsened. These tests
    // exercise the isolated Editor-only candidate, not the restored runtime shader.
    const string Product = "Assets/Scripts/Tests/EditMode/Fixtures/SelectionOutlineCandidate.shader";
    const string Reference = "Assets/Scripts/Tests/EditMode/Fixtures/SelectionOutlineReference.shader";
    const string Evidence = "docs/development/outline_sampling_20261023/";
    static string Phase => File.ReadAllText(Evidence + "phase.txt").Trim();

    static Texture2D Neighborhoods()
    {
        var texture = new Texture2D(224, 112, TextureFormat.RGBA32, false, true)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };
        var pixels = new Color32[224 * 112];
        for (int n = 0; n < 512; n++)
        {
            for (int bit = 0; bit < 9; bit++)
            {
                if ((n & (1 << bit)) != 0)
                {
                    pixels[(n / 32 * 7 + 2 + bit / 3) * 224 + n % 32 * 7 + 2 + bit % 3] = new Color32(255, 0, 0, 255);
                }
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        return texture;
    }

    static Color32[] Render(Texture2D input, Shader shader, int width)
    {
        Assert.That(shader, Is.Not.Null);
        Assert.That(shader.isSupported, Is.True);
        var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        var rt = new RenderTexture(
            input.width,
            input.height,
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Linear
        )
        {
            filterMode = FilterMode.Point,
            useMipMap = false,
        };
        var read = new Texture2D(input.width, input.height, TextureFormat.RGBA32, false, true);
        var old = RenderTexture.active;
        bool oldSRGB = GL.sRGBWrite;
        try
        {
            rt.Create();
            RenderTexture.active = rt;
            GL.Clear(false, true, Color.clear);
            GL.sRGBWrite = false;
            material.SetInt("_Width", width);
            material.SetColor("_Color", new Color(.8f, .2f, .6f, .9f));
            Graphics.Blit(input, rt, material);
            RenderTexture.active = rt;
            read.ReadPixels(new Rect(0, 0, input.width, input.height), 0, 0);
            read.Apply();
            Assert.That(ShaderUtil.ShaderHasError(shader), Is.False);
            return read.GetPixels32();
        }
        finally
        {
            GL.sRGBWrite = oldSRGB;
            RenderTexture.active = old;
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(read);
            UnityEngine.Object.DestroyImmediate(material);
        }
    }

    static Shader Instrument(string source, string name)
    {
        source = new System.Text.RegularExpressions.Regex("Shader \"[^\"]+\"").Replace(
            source,
            "Shader \"" + name + "\"",
            1
        );
        source = source
            .Replace("SAMPLE_TEXTURE2D_LOD(_MainTex,sampler_MainTex,", "ReadCountedLod(")
            .Replace("SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,", "ReadCounted(");
        source = source.Replace(
            "struct Varyings",
            @"
            static uint logicalReads;
            float4 ReadCounted(float2 uv){logicalReads++;return SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv);}
            float4 ReadCountedLod(float2 uv,float lod){logicalReads++;return SAMPLE_TEXTURE2D_LOD(_MainTex,sampler_MainTex,uv,lod);}
            struct Varyings"
        );
        source = source
            .Replace("half4 Frag(Varyings i):SV_Target", "half4 MeasuredFrag(Varyings i)")
            .Replace(
                "ENDHLSL",
                @"half4 Frag(Varyings i):SV_Target
                {logicalReads=0;half4 result=MeasuredFrag(i);return half4(logicalReads/255.0,0,0,1);}
                ENDHLSL"
            );
        var shader = ShaderUtil.CreateShaderAsset(source);
        shader.hideFlags = HideFlags.HideAndDontSave;
        if (ShaderUtil.ShaderHasError(shader))
        {
            string errors = string.Join("; ", ShaderUtil.GetShaderMessages(shader).Select(m => m.message));
            UnityEngine.Object.DestroyImmediate(shader);
            Assert.Fail(errors);
        }
        return shader;
    }

    static void Compare(Texture2D input, int width, string label)
    {
        var original = Render(input, AssetDatabase.LoadAssetAtPath<Shader>(Reference), width);
        var candidate = Render(input, AssetDatabase.LoadAssetAtPath<Shader>(Product), width);
        int differences = original.Where((p, i) => !p.Equals(candidate[i])).Count();
        File.AppendAllText(
            Evidence + Phase + "_pixels.csv",
            label + "," + width + "," + input.width + "," + input.height + "," + differences + Environment.NewLine
        );
        Assert.That(differences, Is.Zero, label + " width" + width);
        if (label == "all512")
        {
            Assert.That(
                original.Count(p => p.a > 0),
                Is.GreaterThan(100),
                "A blank render cannot validate the outline"
            );
        }
    }

    [Test]
    public void OutputMatchesReference_ForAll512RadiusOneNeighborhoods()
    {
        var input = Neighborhoods();
        try
        {
            Compare(input, 1, "all512");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(input);
        }
    }

    [Test]
    public void OutputMatchesReference_ForAllWidthsAndFloatThresholdsAtClampEdges()
    {
        Assert.That(SystemInfo.SupportsTextureFormat(TextureFormat.RGBAFloat), Is.True, "Float mask fixture required");
        var input = new Texture2D(33, 19, TextureFormat.RGBAFloat, false, true)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };
        float[] values = { 0, .499f, .4999f, .5f, .5001f, 1 };
        var pixels = new Color[33 * 19];
        for (int y = 0; y < 19; y++)
        {
            for (int x = 0; x < 33; x++)
            {
                pixels[y * 33 + x] = new Color(values[(x + 3 * y) % values.Length], 0, 0, 1);
            }
        }

        input.SetPixels(pixels);
        input.Apply(false, false);
        try
        {
            for (int width = 0; width <= 8; width++)
            {
                Compare(input, width, "threshold-clamp");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(input);
        }
    }

    [Test]
    public void OutputMatchesReference_ForIsolatedFloatNeighborsWithoutWinningBinaryPixels()
    {
        var input = new Texture2D(33, 19, TextureFormat.RGBAFloat, false, true)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };
        try
        {
            foreach (float value in new[] { 0, .499f, .4999f, .5f, .5001f, 1 })
            {
                var pixels = new Color[33 * 19];
                // No other larger neighbor can hide this value's half/clip decision.
                pixels[9 * 33 + 16] = pixels[0] = new Color(value, 0, 0, 1);
                input.SetPixels(pixels);
                input.Apply(false, false);
                for (int width = 0; width <= 8; width++)
                {
                    Compare(
                        input,
                        width,
                        "isolated-" + value.ToString("R", System.Globalization.CultureInfo.InvariantCulture)
                    );
                }
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(input);
        }
    }

    static byte[] LogicalReads()
    {
        var input = Neighborhoods();
        Shader shader = null;
        try
        {
            shader = Instrument(File.ReadAllText(Product), "Hidden/CoreGuardianTests/MeasuredOutline");
            var reads = Render(input, shader, 1).Where(p => p.a > 0).Select(p => p.r).ToArray();
            Assert.That(reads.Length, Is.GreaterThan(100));
            File.AppendAllText(
                Evidence + Phase + "_logical_reads.csv",
                reads.Length
                    + ","
                    + reads.Min()
                    + ","
                    + reads.Max()
                    + ","
                    + reads.Sum(p => (long)p)
                    + Environment.NewLine
            );
            return reads;
        }
        finally
        {
            if (shader != null)
            {
                UnityEngine.Object.DestroyImmediate(shader);
            }
            UnityEngine.Object.DestroyImmediate(input);
        }
    }

    [Test]
    public void RadiusOneDoesNotReadCenterTwice()
    {
        Assert.That(LogicalReads().Max(), Is.LessThanOrEqualTo(9));
    }

    [Test]
    public void RadiusOneStopsAfterFirstWinningNeighbor()
    {
        Assert.That(LogicalReads().Min(), Is.EqualTo(2));
    }
}
