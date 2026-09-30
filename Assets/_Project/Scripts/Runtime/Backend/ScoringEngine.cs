using System.Collections.Generic;
using System.Linq;
using SurgicalFoundations.Contracts;
using UnityEngine;

namespace SurgicalFoundations.Backend
{
    /// <summary>
    /// On-device scoring (FR-17). Deterministic from the event log and a versioned <see cref="ScoringConfig"/> (FR-18),
    /// so the result is available offline, instantly, and can be re-derived later from the same inputs.
    /// </summary>
    public static class ScoringEngine
    {
        public class Issue
        {
            public ProtocolEvent evt;
            public float penalty;
        }

        public class Result
        {
            public CompleteSessionRequest request;
            public float passThreshold;
            public bool criticalFailure;
            public List<Issue> issues = new List<Issue>();
            public int scoringConfigVersion;
        }

        static readonly ScenarioStage[] ScoredStages = { ScenarioStage.Prep, ScenarioStage.Access, ScenarioStage.Operate, ScenarioStage.Close };

        /// <param name="stageSeconds">Time spent in each stage that was played. Unplayed stages are left out of the score.</param>
        public static Result Score(ScoringConfig config, IReadOnlyList<ProtocolEvent> events,
            IReadOnlyDictionary<ScenarioStage, float> stageSeconds, long completedAtUnixMs)
        {
            var result = new Result { passThreshold = config.passThreshold, scoringConfigVersion = config.version };
            var played = ScoredStages.Where(stageSeconds.ContainsKey).ToList();
            var stageScores = new List<StageScore>();
            float weighted = 0, weightSum = 0;

            foreach (var stage in played)
            {
                float penalty = 0;
                int deviations = 0, delayed = 0;
                foreach (var e in events.Where(e => e.stage == stage))
                {
                    var p = PenaltyFor(config, e, out var critical);
                    penalty += p;
                    if (critical) result.criticalFailure = true;
                    if (e.eventClass == EventClass.Deviation) deviations++;
                    if (e.eventClass == EventClass.Delayed) delayed++;
                    if (p > 0) result.issues.Add(new Issue { evt = e, penalty = p });
                }
                var score = Mathf.Clamp(100f - penalty, 0f, 100f);
                stageScores.Add(new StageScore { stage = stage, score = score, durationSeconds = stageSeconds[stage], deviations = deviations, delayed = delayed });

                var weight = config.stageWeights?.FirstOrDefault(w => w.stage == stage)?.weight ?? 1f / ScoredStages.Length;
                weighted += weight * score;
                weightSum += weight;
            }

            // A partial run (retrying one stage) is scored over the stages it contains, re-weighted to 100.
            var overall = weightSum > 0 ? weighted / weightSum : 0f;
            result.issues = result.issues.OrderByDescending(i => i.penalty).ThenBy(i => i.evt.sessionTime).ToList();
            result.request = new CompleteSessionRequest
            {
                completedAtUnixMs = completedAtUnixMs,
                durationSeconds = stageSeconds.Values.Sum(),
                overallScore = Mathf.Round(overall * 10f) / 10f,
                passed = played.Count > 0 && !result.criticalFailure && overall >= config.passThreshold,
                stageScores = stageScores,
                topIssues = result.issues.Take(3).Select(i => string.IsNullOrEmpty(i.evt.message) ? i.evt.code : i.evt.message).ToList()
            };
            return result;
        }

        /// <summary>The most specific per-code penalty wins ("prep.contamination" covers "prep.contamination.glove"); else the class default.</summary>
        public static float PenaltyFor(ScoringConfig config, ProtocolEvent e, out bool critical)
        {
            critical = false;
            EventPenalty best = null;
            if (config.eventPenalties != null && !string.IsNullOrEmpty(e.code))
                foreach (var p in config.eventPenalties)
                    if ((e.code == p.code || e.code.StartsWith(p.code + ".")) && (best == null || p.code.Length > best.code.Length))
                        best = p;
            if (best != null)
            {
                critical = best.critical;
                return best.penalty;
            }
            return config.classPenalties?.FirstOrDefault(c => c.eventClass == e.eventClass)?.penalty ?? 0f;
        }
    }
}
