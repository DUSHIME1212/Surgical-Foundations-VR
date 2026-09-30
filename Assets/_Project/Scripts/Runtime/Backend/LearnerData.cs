using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SurgicalFoundations.Contracts;
using UnityEngine;

namespace SurgicalFoundations.Backend
{
    /// <summary>
    /// What the lobby shows: the learner's assignments and recent attempts. Cached per user on disk, so the lobby
    /// still shows them offline.
    /// </summary>
    public class LearnerData
    {
        [Serializable]
        class Snapshot
        {
            public AssignmentList assignments = new AssignmentList();
            public SessionList attempts = new SessionList();
        }

        readonly string directory;
        string loadedFor;
        Snapshot data = new Snapshot();

        public LearnerData(string directory)
        {
            this.directory = directory;
            Directory.CreateDirectory(directory);
        }

        public event Action Changed;

        public AssignmentList Assignments => data.assignments;
        public SessionList Attempts => data.attempts;

        /// <summary>Switches to a user's cached data (or clears it for guests/signed-out).</summary>
        public void UseCacheFor(string userId)
        {
            if (loadedFor == userId) return;
            loadedFor = userId;
            data = new Snapshot();
            if (userId != null)
            {
                try
                {
                    var path = PathFor(userId);
                    if (File.Exists(path)) data = ApiJson.Deserialize<Snapshot>(File.ReadAllText(path)) ?? new Snapshot();
                }
                catch (Exception e) { Debug.LogWarning("[LearnerData] Ignoring unreadable cache: " + e.Message); }
            }
            Changed?.Invoke();
        }

        public async Task RefreshAsync(ApiClient api, AuthService auth, CancellationToken ct)
        {
            var userId = auth.UserId;
            if (userId == null) return;
            var token = await auth.GetAccessTokenAsync(ct);
            if (token == null) return;

            var assignments = await api.GetAsync(ApiRoutes.MyAssignments, token, ct);
            var attempts = await api.GetAsync(ApiRoutes.Sessions + "?limit=10", token, ct);
            if (auth.UserId != userId) return; // signed out meanwhile

            UseCacheFor(userId);
            if (assignments.Ok) data.assignments = assignments.Read<AssignmentList>() ?? new AssignmentList();
            if (attempts.Ok) data.attempts = attempts.Read<SessionList>() ?? new SessionList();
            if (!assignments.Ok && !attempts.Ok) return;
            try { File.WriteAllText(PathFor(userId), ApiJson.Serialize(data)); }
            catch (IOException e) { Debug.LogWarning("[LearnerData] Could not cache: " + e.Message); }
            Changed?.Invoke();
        }

        /// <summary>
        /// The assignment a new session counts toward: the earliest-due unfinished one for this scenario and mode.
        /// Linking explicitly lets instructors see exactly which runs were for their assignment.
        /// </summary>
        public AssignmentSummary NextAssignment(TrainingMode mode, string scenarioId) =>
            data.assignments?.assignments?
                .Where(a => a.status != AssignmentStatus.Completed && a.mode == mode && a.scenarioId == scenarioId)
                .OrderBy(a => a.dueAtUnixMs == 0 ? long.MaxValue : a.dueAtUnixMs)
                .FirstOrDefault();

        public AssignmentSummary NextAssignmentAnyMode(string scenarioId) =>
            data.assignments?.assignments?
                .Where(a => a.status != AssignmentStatus.Completed && a.scenarioId == scenarioId)
                .OrderBy(a => a.dueAtUnixMs == 0 ? long.MaxValue : a.dueAtUnixMs)
                .FirstOrDefault();

        string PathFor(string userId) => Path.Combine(directory, "learner-" + userId + ".json");
    }
}
