using UnityEditor;
using UnityEngine;

namespace SurgicalFoundations.EditorTools
{
    /// <summary>
    /// Import conventions for everything under Assets/_Project so nobody has to remember per-file settings.
    /// UI sprites: 9-slice borders from the file name. Audio: load type and compression by folder (Quest memory budget).
    /// </summary>
    public class AssetImportRules : AssetPostprocessor
    {
        const string ProjectRoot = "Assets/_Project/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ProjectRoot)) return;
            var ti = (TextureImporter)assetImporter;

            if (assetPath.Contains("/UI/"))
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.filterMode = FilterMode.Bilinear;
                ti.textureCompression = TextureImporterCompression.Uncompressed; // tiny white masks; keeps edges crisp
                ti.spritePixelsPerUnit = 100;
                var settings = new TextureImporterSettings();
                ti.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                ti.SetTextureSettings(settings);
                ti.spriteBorder = BorderFor(System.IO.Path.GetFileNameWithoutExtension(assetPath));
            }
            else if (assetPath.Contains("/Art/Textures/"))
            {
                ti.mipmapEnabled = true;
                ti.anisoLevel = 4;
                ti.wrapMode = TextureWrapMode.Repeat;
            }
        }

        static Vector4 BorderFor(string file) => file switch
        {
            // left, bottom, right, top
            "panel_r28" or "panel_r28_outline" => new Vector4(32, 32, 32, 32),
            "card_r14" or "card_r14_outline" => new Vector4(18, 18, 18, 18),
            "pill" or "pill_outline" => new Vector4(32, 31, 32, 31),
            "bar_r8" => new Vector4(8, 7, 8, 7),
            _ => Vector4.zero
        };

        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(ProjectRoot)) return;
            var ai = (AudioImporter)assetImporter;
            ai.forceToMono = true;
            ai.loadInBackground = assetPath.Contains("/Ambience/") || assetPath.Contains("/Voice/");

            var s = ai.defaultSampleSettings;
            if (assetPath.Contains("/Ambience/") || assetPath.EndsWith("_Loop.wav"))
            {
                s.loadType = AudioClipLoadType.CompressedInMemory;
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = 0.55f;
            }
            else if (assetPath.Contains("/Voice/"))
            {
                s.loadType = AudioClipLoadType.CompressedInMemory;
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = 0.6f;
            }
            else
            {
                // Short, frequently triggered one-shots: decompress once, zero decode cost at play time.
                s.loadType = AudioClipLoadType.DecompressOnLoad;
                s.compressionFormat = AudioCompressionFormat.ADPCM;
            }
            s.preloadAudioData = true;
            ai.defaultSampleSettings = s;
        }
    }
}
