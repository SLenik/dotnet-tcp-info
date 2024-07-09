using System.Runtime.InteropServices;

namespace TcpInfoExample
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TcpInfoV0
    {
        public uint State;
        public uint Mss;
        public ulong ConnectionTimeMs;
        public byte TimestampsEnabled;
        public uint RttUs;
        public uint MinRttUs;
        public uint BytesInFlight;
        public uint Cwnd;
        public uint SndWnd;
        public uint RcvWnd;
        public uint RcvBuf;
        public ulong BytesOut;
        public ulong BytesIn;
        public uint BytesReordered;
        public uint BytesRetrans;
        public uint FastRetrans;
        public uint DupAcksIn;
        public uint TimeoutEpisodes;
        public byte SynRetrans;
    }
}
