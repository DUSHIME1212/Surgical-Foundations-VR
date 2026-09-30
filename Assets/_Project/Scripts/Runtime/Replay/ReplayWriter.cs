using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using SurgicalFoundations.Contracts;

namespace SurgicalFoundations.Replay
{
    /// <summary>A finished, compressed replay file ready for upload.</summary>
    public class ReplayFileInfo
    {
        public string path;
        public long sizeBytes;
        public string sha256;
        public float durationSeconds;
        public int frameCount;
        public float sampleRateHz;
    }

    /// <summary>
    /// Streams replay records to an uncompressed ".part" file while the session runs (flushed every second, so memory
    /// stays flat for any session length), then compresses it into the final ".sfr" in <see cref="Finish"/>.
    /// </summary>
    public sealed class ReplayWriter : IDisposable
    {
        readonly string partPath;
        readonly float sampleRateHz;
        FileStream stream;
        BinaryWriter writer;
        float lastFlushTime;

        public ReplayWriter(string partPath, string sessionId, float sampleRateHz, long startedAtUnixMs)
        {
            this.partPath = partPath;
            this.sampleRateHz = sampleRateHz;
            Directory.CreateDirectory(Path.GetDirectoryName(partPath));
            stream = new FileStream(partPath, FileMode.Create, FileAccess.Write, FileShare.Read, 64 * 1024);
            writer = new BinaryWriter(stream);
            writer.Write(ReplayFormat.Magic);
            writer.Write(ReplayFormat.Version);
            writer.Write(sampleRateHz);
            writer.Write(startedAtUnixMs);
            ReplayFormat.WriteString(writer, sessionId);
        }

        public int FrameCount { get; private set; }
        public float LastTime { get; private set; }

        public void WriteTrack(ushort id, TrackKind kind, string name)
        {
            writer.Write(ReplayFormat.RecordTrack);
            writer.Write(id);
            writer.Write((byte)kind);
            ReplayFormat.WriteString(writer, name);
        }

        public void WriteFrame(float time, List<TrackSample> samples)
        {
            writer.Write(ReplayFormat.RecordFrame);
            writer.Write(time);
            writer.Write((ushort)samples.Count);
            foreach (var s in samples)
            {
                writer.Write(s.track);
                writer.Write((byte)s.flags);
                ReplayFormat.WriteVector(writer, s.position);
                ReplayFormat.WriteRotation(writer, s.rotation);
                if ((s.flags & PoseFlags.HasScalar) != 0) writer.Write(s.scalar);
            }
            FrameCount++;
            LastTime = time;
            if (time - lastFlushTime >= 1f) { writer.Flush(); lastFlushTime = time; }
        }

        public void WriteStage(float time, ScenarioStage stage)
        {
            writer.Write(ReplayFormat.RecordStage);
            writer.Write(time);
            writer.Write((byte)stage);
        }

        /// <summary>
        /// Closes the recording and returns a function that compresses it. The compression is pure file work with no
        /// Unity calls, so the caller can run it off the main thread and avoid a frame hitch in the headset.
        /// </summary>
        public Func<ReplayFileInfo> Finish(string finalPath)
        {
            writer.Write(ReplayFormat.RecordEnd);
            writer.Write(LastTime);
            writer.Write((uint)FrameCount);
            Close();
            var frames = FrameCount;
            var duration = LastTime;
            var rate = sampleRateHz;
            var part = partPath;
            return () =>
            {
                using (var input = File.OpenRead(part))
                using (var output = File.Create(finalPath))
                using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
                    input.CopyTo(gzip);
                File.Delete(part);

                string hash;
                using (var file = File.OpenRead(finalPath))
                using (var sha = SHA256.Create())
                    hash = BitConverter.ToString(sha.ComputeHash(file)).Replace("-", "").ToLowerInvariant();

                return new ReplayFileInfo
                {
                    path = finalPath,
                    sizeBytes = new FileInfo(finalPath).Length,
                    sha256 = hash,
                    durationSeconds = duration,
                    frameCount = frames,
                    sampleRateHz = rate
                };
            };
        }

        /// <summary>Discards the recording (abandoned session, guest, error).</summary>
        public void Abort()
        {
            Close();
            try { File.Delete(partPath); } catch (IOException) { }
        }

        void Close()
        {
            writer?.Flush();
            writer?.Dispose();
            stream?.Dispose();
            writer = null;
            stream = null;
        }

        public void Dispose() => Close();
    }
}
