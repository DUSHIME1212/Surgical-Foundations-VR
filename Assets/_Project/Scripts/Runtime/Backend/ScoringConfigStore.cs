using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SurgicalFoundations.Contracts;
using UnityEngine;

namespace SurgicalFoundations.Backend
{
    /// <summary>
    /// The scoring config the headset scores with (FR-18): the last one downloaded, else the copy shipped in the build,
    /// so a brand-new headset can score offline on first launch. Each session records the version it used.
    /// </summary>
    public class ScoringConfigStore
    {
        public const string BundledResource = "ScoringConfigDefault";
        readonly string cachePath;

        public ScoringConfigStore(string cachePath)
        {
            this.cachePath = cachePath;
            Current = LoadCached() ?? LoadBundled() ?? Fallback();
        }

        public ScoringConfig Current { get; private set; }

        public async Task RefreshAsync(ApiClient api, CancellationToken ct)
        {
            var response = await api.GetAsync(ApiRoutes.LatestScoringConfig, null, ct);
            if (!response.Ok) return;
            var latest = response.Read<ScoringConfig>();
            if (latest == null || latest.version <= Current.version) return;
            Current = latest;
            try { File.WriteAllText(cachePath, ApiJson.Serialize(latest)); }
            catch (IOException e) { Debug.LogWarning("[ScoringConfig] Could not cache: " + e.Message); }
        }

        ScoringConfig LoadCached()
        {
            try { return File.Exists(cachePath) ? ApiJson.Deserialize<ScoringConfig>(File.ReadAllText(cachePath)) : null; }
            catch (Exception) { return null; }
        }

        static ScoringConfig LoadBundled()
        {
            var asset = Resources.Load<TextAsset>(BundledResource);
            return asset == null ? null : ApiJson.Deserialize<ScoringConfig>(asset.text);
        }

        // Last resort so scoring never crashes: equal weights, pass at 70.
        static ScoringConfig Fallback() => new ScoringConfig
        {
            version = 1,
            scenarioId = "lap-foundations",
            passThreshold = 70,
            classPenalties =
            {
                new ClassPenalty { eventClass = EventClass.Delayed, penalty = 2 },
                new ClassPenalty { eventClass = EventClass.Deviation, penalty = 5 }
            }
        };
    }
}
