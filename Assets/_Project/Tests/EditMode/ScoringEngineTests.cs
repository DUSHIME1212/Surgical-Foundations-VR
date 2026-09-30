using System.Collections.Generic;
using NUnit.Framework;
using SurgicalFoundations.Backend;
using SurgicalFoundations.Contracts;

namespace SurgicalFoundations.Tests
{
    public class ScoringEngineTests
    {
        static ScoringConfig Config() => new ScoringConfig
        {
            version = 1,
            passThreshold = 70,
            stageWeights =
            {
                new StageWeight { stage = ScenarioStage.Prep, weight = 0.25f },
                new StageWeight { stage = ScenarioStage.Access, weight = 0.25f },
                new StageWeight { stage = ScenarioStage.Operate, weight = 0.35f },
                new StageWeight { stage = ScenarioStage.Close, weight = 0.15f }
            },
            classPenalties =
            {
                new ClassPenalty { eventClass = EventClass.Delayed, penalty = 2 },
                new ClassPenalty { eventClass = EventClass.Deviation, penalty = 5 }
            },
            eventPenalties =
            {
                new EventPenalty { code = "prep.contamination", penalty = 10 },
                new EventPenalty { code = "prep.contamination.glove.major", penalty = 20 },
                new EventPenalty { code = "close.count.mismatch_unresolved", penalty = 100, critical = true }
            }
        };

        static Dictionary<ScenarioStage, float> AllStages() => new Dictionary<ScenarioStage, float>
        {
            [ScenarioStage.Prep] = 240, [ScenarioStage.Access] = 200, [ScenarioStage.Operate] = 330, [ScenarioStage.Close] = 135
        };

        static ProtocolEvent E(ScenarioStage stage, EventClass cls, string code, float t = 1f) =>
            ProtocolEvent.Create(t, stage, cls, code, code);

        [Test]
        public void Clean_run_scores_100_and_passes()
        {
            var r = ScoringEngine.Score(Config(), new List<ProtocolEvent>(), AllStages(), 1);
            Assert.AreEqual(100f, r.request.overallScore);
            Assert.IsTrue(r.request.passed);
            Assert.AreEqual(905f, r.request.durationSeconds);
        }

        [Test]
        public void Class_penalties_and_weights_combine()
        {
            var events = new List<ProtocolEvent>
            {
                E(ScenarioStage.Prep, EventClass.Deviation, "demo.deviation"),
                E(ScenarioStage.Prep, EventClass.Delayed, "demo.delayed"),
                E(ScenarioStage.Access, EventClass.Deviation, "demo.deviation"),
                E(ScenarioStage.Operate, EventClass.OnProtocol, "demo.onprotocol")
            };
            var r = ScoringEngine.Score(Config(), events, AllStages(), 1);
            // Prep 93, Access 95, Operate 100, Close 100 → .25·93 + .25·95 + .35·100 + .15·100 = 97
            Assert.AreEqual(97f, r.request.overallScore, 0.05f);
            Assert.AreEqual(93f, r.request.stageScores[0].score);
            Assert.AreEqual(1, r.request.stageScores[0].deviations);
            Assert.AreEqual(1, r.request.stageScores[0].delayed);
        }

        [Test]
        public void The_most_specific_code_penalty_wins()
        {
            var config = Config();
            Assert.AreEqual(10f, ScoringEngine.PenaltyFor(config, E(ScenarioStage.Prep, EventClass.Deviation, "prep.contamination"), out _));
            Assert.AreEqual(10f, ScoringEngine.PenaltyFor(config, E(ScenarioStage.Prep, EventClass.Deviation, "prep.contamination.gown"), out _));
            Assert.AreEqual(20f, ScoringEngine.PenaltyFor(config, E(ScenarioStage.Prep, EventClass.Deviation, "prep.contamination.glove.major"), out _));
            Assert.AreEqual(5f, ScoringEngine.PenaltyFor(config, E(ScenarioStage.Prep, EventClass.Deviation, "prep.contaminationish"), out _),
                "prefixes match on whole segments only");
        }

        [Test]
        public void A_critical_event_fails_the_run_whatever_the_score()
        {
            var events = new List<ProtocolEvent> { E(ScenarioStage.Close, EventClass.Deviation, "close.count.mismatch_unresolved") };
            var r = ScoringEngine.Score(Config(), events, AllStages(), 1);
            Assert.AreEqual(85f, r.request.overallScore, 0.05f); // Close 0 × 0.15
            Assert.IsTrue(r.criticalFailure);
            Assert.IsFalse(r.request.passed);
        }

        [Test]
        public void A_partial_run_is_scored_over_the_stages_it_contains()
        {
            var events = new List<ProtocolEvent> { E(ScenarioStage.Access, EventClass.Deviation, "demo.deviation") };
            var r = ScoringEngine.Score(Config(), events, new Dictionary<ScenarioStage, float> { [ScenarioStage.Access] = 120 }, 1);
            Assert.AreEqual(1, r.request.stageScores.Count);
            Assert.AreEqual(95f, r.request.overallScore);
        }

        [Test]
        public void Top_issues_are_the_three_costliest()
        {
            var events = new List<ProtocolEvent>
            {
                E(ScenarioStage.Prep, EventClass.Delayed, "a", 1),
                E(ScenarioStage.Prep, EventClass.Deviation, "prep.contamination", 2),
                E(ScenarioStage.Access, EventClass.Deviation, "b", 3),
                E(ScenarioStage.Operate, EventClass.Delayed, "c", 4)
            };
            var r = ScoringEngine.Score(Config(), events, AllStages(), 1);
            CollectionAssert.AreEqual(new[] { "prep.contamination", "b", "a" }, r.request.topIssues);
        }
    }
}
