using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace SurgicalFoundations.Replay
{
    /// <summary>What a track follows. Values are part of the file format; never renumber.</summary>
    public enum TrackKind : byte { Head = 0, LeftHand = 1, RightHand = 2, Instrument = 3, LaparoscopeCamera = 4 }

    [Flags]
    public enum PoseFlags : byte
    {
        None = 0,
        /// <summary>The sample carries a 0–255 scalar (instrument jaw openness).</summary>
        HasScalar = 1,
        /// <summary>Hand pose came from hand tracking rather than a controller.</summary>
        HandTracking = 2
    }

    public struct TrackSample
    {
        public ushort track;
        public PoseFlags flags;
        public Vector3 position;
        public Quaternion rotation;
        public byte scalar;
    }

    /// <summary>
    /// Session replay file, version 1 (".sfr"). The whole file is gzip-compressed; inside, little-endian binary:
    /// <code>
    /// header : "SFR1" · u16 version · f32 sampleRateHz · i64 startedAtUnixMs · str sessionId
    /// record : u8 type, then
    ///   1 Track  : u16 trackId · u8 TrackKind · str name                       (tracks can appear mid-session)
    ///   2 Frame  : f32 time · u16 count · count × sample
    ///   3 Stage  : f32 time · u8 ScenarioStage                                  (stage boundaries, for seeking)
    ///   255 End  : f32 duration · u32 frameCount
    /// sample : u16 trackId · u8 PoseFlags · 3×f32 world position (m) · 3×i16 rotation · [u8 scalar if HasScalar]
    /// str    : u16 byte length · UTF-8 bytes
    /// </code>
    /// Times are session seconds (pauses excluded), the same clock as protocol events, so events line up with poses.
    /// Rotations store x, y, z of the unit quaternion with w ≥ 0 (w is rebuilt): ≤ 0.5° error, 6 bytes instead of 16.
    /// Full spec for other readers (dashboard, WebGL viewer): Studium XR Backend/docs/replay-format.md.
    /// </summary>
    public static class ReplayFormat
    {
        public static readonly byte[] Magic = { (byte)'S', (byte)'F', (byte)'R', (byte)'1' };
        public const ushort Version = 1;
        public const byte RecordTrack = 1, RecordFrame = 2, RecordStage = 3, RecordEnd = 255;
        public const string ContentType = "application/vnd.studiumxr.replay+gzip";
        public const string FileExtension = ".sfr";

        public static void WriteString(BinaryWriter w, string s)
        {
            var bytes = Encoding.UTF8.GetBytes(s ?? "");
            w.Write((ushort)Math.Min(bytes.Length, ushort.MaxValue));
            w.Write(bytes, 0, Math.Min(bytes.Length, ushort.MaxValue));
        }

        public static string ReadString(BinaryReader r) => Encoding.UTF8.GetString(r.ReadBytes(r.ReadUInt16()));

        public static void WriteRotation(BinaryWriter w, Quaternion q)
        {
            var n = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
            if (n < 1e-6f) q = Quaternion.identity;
            else { q.x /= n; q.y /= n; q.z /= n; q.w /= n; }
            if (q.w < 0) { q.x = -q.x; q.y = -q.y; q.z = -q.z; q.w = -q.w; } // q and −q are the same rotation
            w.Write(Pack(q.x));
            w.Write(Pack(q.y));
            w.Write(Pack(q.z));
        }

        public static Quaternion ReadRotation(BinaryReader r)
        {
            float x = r.ReadInt16() / 32767f, y = r.ReadInt16() / 32767f, z = r.ReadInt16() / 32767f;
            return new Quaternion(x, y, z, Mathf.Sqrt(Mathf.Max(0f, 1f - x * x - y * y - z * z)));
        }

        public static void WriteVector(BinaryWriter w, Vector3 v) { w.Write(v.x); w.Write(v.y); w.Write(v.z); }
        public static Vector3 ReadVector(BinaryReader r) => new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());

        static short Pack(float v) => (short)Mathf.Clamp(Mathf.RoundToInt(v * 32767f), -32767, 32767);
    }
}
