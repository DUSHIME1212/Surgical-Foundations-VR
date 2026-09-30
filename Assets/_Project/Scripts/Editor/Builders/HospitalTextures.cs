using System.IO;
using UnityEditor;
using UnityEngine;

namespace SurgicalFoundations.EditorTools
{
    /// <summary>
    /// Tileable surface textures for the hospital-room furniture (the FBX ships untextured): fabric weave, vinyl,
    /// wood grain, laminate and powder coat. Generated on the CPU from periodic value noise, so they tile seamlessly;
    /// albedos are near-neutral where the material tints them (MaterialLibrary "Hosp_*").
    /// </summary>
    public static class HospitalTextures
    {
        public const string Folder = SFPaths.Textures + "/Hospital";
        public static string Path(string name) => $"{Folder}/T_Hosp_{name}.png";

        const int Size = 512;

        public static bool Exist() => File.Exists(Path("Fabric_Normal"));

        public static void Build()
        {
            Directory.CreateDirectory(Folder);
            Fabric();
            Vinyl();
            Wood();
            Laminate();
            PowderCoat();
            AssetDatabase.Refresh();
            foreach (var n in new[] { "Fabric", "Vinyl", "Wood", "Laminate", "PowderCoat" })
            {
                Configure(Path(n + "_Albedo"), false);
                Configure(Path(n + "_Normal"), true);
            }
        }

        // ───────────── surfaces ─────────────

        static void Fabric()
        {
            const int threads = 64; // per tile; integer so the weave wraps
            var h = Field((u, v) =>
            {
                float cu = u * threads, cv = v * threads;
                float fu = cu - Mathf.Floor(cu), fv = cv - Mathf.Floor(cv);
                bool over = (((int)cu + (int)cv) & 1) == 0;
                float across = 1f - Sq(2f * fv - 1f), along = 1f - Sq(2f * fu - 1f);
                return (over ? across : along) * 0.85f + Fbm(u, v, 32, 32, 3, 11) * 0.15f;
            });
            Save("Fabric_Albedo", h, x => Gray(0.82f + 0.18f * x));
            SaveNormal("Fabric_Normal", h, 2.5f);
        }

        static void Vinyl()
        {
            var h = Field((u, v) => Fbm(u, v, 48, 48, 3, 21));
            Save("Vinyl_Albedo", h, x => Gray(0.93f + 0.07f * x));
            SaveNormal("Vinyl_Normal", h, 0.9f);
        }

        static void Wood()
        {
            // Fine, nearly straight grain along U (sawn beech veneer); a gentle low-frequency warp keeps it from looking ruled.
            var h = Field((u, v) =>
            {
                float warp = Fbm(u, v, 2, 3, 2, 31);
                float grain = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * (v * 18f + warp * 2.5f));
                float fibres = Fbm(u, v, 3, 48, 4, 37);
                float blotch = Fbm(u, v, 3, 3, 2, 43);
                return Mathf.Clamp01(Mathf.Pow(grain, 6f) * 0.3f + fibres * 0.5f + blotch * 0.2f);
            });
            var light = new Color(0.82f, 0.68f, 0.52f);
            var dark = new Color(0.66f, 0.50f, 0.35f);
            Save("Wood_Albedo", h, x => Color.Lerp(light, dark, x));
            SaveNormal("Wood_Normal", h, 0.12f);
        }

        static void Laminate()
        {
            var h = Field((u, v) => Fbm(u, v, 96, 96, 2, 41));
            Save("Laminate_Albedo", h, x => Gray(0.955f + 0.045f * x));
            SaveNormal("Laminate_Normal", h, 0.25f);
        }

        static void PowderCoat()
        {
            var h = Field((u, v) => Fbm(u, v, 24, 24, 4, 51)); // orange-peel
            Save("PowderCoat_Albedo", h, x => Gray(0.96f + 0.04f * x));
            SaveNormal("PowderCoat_Normal", h, 0.5f);
        }

        // ───────────── periodic noise ─────────────

        static float Sq(float x) => x * x;
        static Color Gray(float g) => new Color(g, g, g, 1f);

        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 982451653);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / 16777215f;
            }
        }

        /// <summary>Value noise on a lattice that repeats every (px, py) cells across the unit tile.</summary>
        static float Value(float u, float v, int px, int py, int seed)
        {
            float x = u * px, y = v * py;
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            int Wx(int i) => ((i % px) + px) % px;
            int Wy(int i) => ((i % py) + py) % py;
            float a = Hash(Wx(x0), Wy(y0), seed), b = Hash(Wx(x0 + 1), Wy(y0), seed);
            float c = Hash(Wx(x0), Wy(y0 + 1), seed), d = Hash(Wx(x0 + 1), Wy(y0 + 1), seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        static float Fbm(float u, float v, int px, int py, int octaves, int seed)
        {
            float sum = 0f, amp = 0.5f, norm = 0f;
            for (int o = 0; o < octaves; o++)
            {
                sum += Value(u, v, px << o, py << o, seed + o) * amp;
                norm += amp;
                amp *= 0.5f;
            }
            return sum / norm;
        }

        // ───────────── output ─────────────

        static float[,] Field(System.Func<float, float, float> f)
        {
            var h = new float[Size, Size];
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
                h[x, y] = f((float)x / Size, (float)y / Size);
            return h;
        }

        static void Save(string name, float[,] h, System.Func<float, Color> map)
        {
            var px = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
                px[y * Size + x] = map(h[x, y]);
            Write(name, px);
        }

        static void SaveNormal(string name, float[,] h, float strength)
        {
            var px = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                float dx = h[(x + 1) % Size, y] - h[(x - 1 + Size) % Size, y];
                float dy = h[x, (y + 1) % Size] - h[x, (y - 1 + Size) % Size];
                var n = new Vector3(-dx * strength * 8f, -dy * strength * 8f, 1f).normalized;
                px[y * Size + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
            }
            Write(name, px);
        }

        static void Write(string name, Color[] px)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false, true);
            tex.SetPixels(px);
            tex.Apply();
            File.WriteAllBytes(Path(name), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        static void Configure(string path, bool normal)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter ti)) return;
            ti.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            ti.sRGBTexture = !normal;
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.mipmapEnabled = true;
            ti.anisoLevel = 4;
            ti.maxTextureSize = 512;
            ti.SaveAndReimport();
        }
    }
}
