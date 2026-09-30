using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using SurgicalFoundations.Contracts;
using UnityEngine;

namespace SurgicalFoundations.Replay
{
    /// <summary>
    /// A loaded replay, ready for playback: per-track keyframes with interpolated sampling at any time (FR-22 seek).
    /// Used by the replay viewer; also how the recorder is tested.
    /// </summary>
    public class ReplayFile
    {
        public class Track
        {
            public ushort id;
            public TrackKind kind;
            public string name;
            public readonly List<Key> keys = new List<Key>();
        }

        public struct Key
        {
            public float time;
            public Vector3 position;
            public Quaternion rotation;
            public float scalar;
            public PoseFlags flags;
        }

        public string SessionId { get; private set; }
        public float SampleRateHz { get; private set; }
        public long StartedAtUnixMs { get; private set; }
        public float DurationSeconds { get; private set; }
        public int FrameCount { get; private set; }
        /// <summary>False when the file ends without its End record (e.g. a partial download).</summary>
        public bool Complete { get; private set; }
        public readonly Dictionary<ushort, Track> Tracks = new Dictionary<ushort, Track>();
        public readonly List<(float time, ScenarioStage stage)> Stages = new List<(float, ScenarioStage)>();

        public static ReplayFile Load(byte[] compressed)
        {
            using var input = new MemoryStream(compressed);
            return Read(input);
        }

        public static ReplayFile Read(Stream compressed)
        {
            using var gzip = new GZipStream(compressed, CompressionMode.Decompress);
            using var reader = new BinaryReader(gzip);
            var file = new ReplayFile();

            var magic = reader.ReadBytes(4);
            for (int i = 0; i < 4; i++)
                if (magic.Length < 4 || magic[i] != ReplayFormat.Magic[i]) throw new InvalidDataException("Not a Surgical Foundations replay file.");
            var version = reader.ReadUInt16();
            if (version > ReplayFormat.Version) throw new InvalidDataException($"Replay format {version} is newer than this app supports.");
            file.SampleRateHz = reader.ReadSingle();
            file.StartedAtUnixMs = reader.ReadInt64();
            file.SessionId = ReplayFormat.ReadString(reader);

            try
            {
                while (true)
                {
                    var type = reader.ReadByte();
                    switch (type)
                    {
                        case ReplayFormat.RecordTrack:
                        {
                            var track = new Track { id = reader.ReadUInt16(), kind = (TrackKind)reader.ReadByte(), name = ReplayFormat.ReadString(reader) };
                            file.Tracks[track.id] = track;
                            break;
                        }
                        case ReplayFormat.RecordFrame:
                        {
                            var time = reader.ReadSingle();
                            var count = reader.ReadUInt16();
                            for (int i = 0; i < count; i++)
                            {
                                var id = reader.ReadUInt16();
                                var flags = (PoseFlags)reader.ReadByte();
                                var key = new Key
                                {
                                    time = time,
                                    flags = flags,
                                    position = ReplayFormat.ReadVector(reader),
                                    rotation = ReplayFormat.ReadRotation(reader),
                                    scalar = (flags & PoseFlags.HasScalar) != 0 ? reader.ReadByte() / 255f : 0f
                                };
                                if (file.Tracks.TryGetValue(id, out var track)) track.keys.Add(key);
                            }
                            file.FrameCount++;
                            file.DurationSeconds = time;
                            break;
                        }
                        case ReplayFormat.RecordStage:
                            file.Stages.Add((reader.ReadSingle(), (ScenarioStage)reader.ReadByte()));
                            break;
                        case ReplayFormat.RecordEnd:
                            file.DurationSeconds = reader.ReadSingle();
                            reader.ReadUInt32();
                            file.Complete = true;
                            return file;
                        default:
                            throw new InvalidDataException($"Unknown replay record type {type}.");
                    }
                }
            }
            catch (EndOfStreamException)
            {
                return file; // truncated: everything read so far is still playable
            }
        }

        /// <summary>Interpolated pose of a track at a time. False when the track has no data around that time.</summary>
        public bool TrySample(ushort trackId, float time, out Key pose)
        {
            pose = default;
            if (!Tracks.TryGetValue(trackId, out var track) || track.keys.Count == 0) return false;
            var keys = track.keys;
            if (time <= keys[0].time) { pose = keys[0]; return time >= keys[0].time - 0.5f; }
            if (time >= keys[keys.Count - 1].time) { pose = keys[keys.Count - 1]; return time <= keys[keys.Count - 1].time + 0.5f; }

            int lo = 0, hi = keys.Count - 1;
            while (hi - lo > 1)
            {
                var mid = (lo + hi) / 2;
                if (keys[mid].time <= time) lo = mid; else hi = mid;
            }
            var a = keys[lo];
            var b = keys[hi];
            // A gap much longer than one sample means the object was gone (e.g. instrument put away): don't invent motion.
            if (b.time - a.time > Mathf.Max(0.5f, 4f / Mathf.Max(1f, SampleRateHz))) { pose = a; return time - a.time < 0.5f; }
            var t = (time - a.time) / Mathf.Max(1e-5f, b.time - a.time);
            pose = new Key
            {
                time = time,
                flags = a.flags,
                position = Vector3.LerpUnclamped(a.position, b.position, t),
                rotation = Quaternion.Slerp(a.rotation, b.rotation, t),
                scalar = Mathf.Lerp(a.scalar, b.scalar, t)
            };
            return true;
        }
    }
}
