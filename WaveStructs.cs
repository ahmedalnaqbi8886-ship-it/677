using System;
using System.Runtime.InteropServices;

namespace EaxReverbEngine
{
    internal struct WAVEFORMATEX
    {
        public ushort wFormatTag;
        public ushort nChannels;
        public int nSamplesPerSec;
        public int nAvgBytesPerSec;
        public ushort nBlockAlign;
        public ushort wBitsPerSample;
        public ushort cbSize;

        public static WAVEFORMATEX Make(int rate, int channels)
        {
            WAVEFORMATEX r = default(WAVEFORMATEX);
            r.wFormatTag = 1;
            r.nChannels = (ushort)channels;
            r.nSamplesPerSec = rate;
            r.wBitsPerSample = 16;
            r.nBlockAlign = (ushort)(channels * 2);
            r.nAvgBytesPerSec = rate * (int)r.nBlockAlign;
            r.cbSize = 0;
            return r;
        }
    }

    internal struct WAVEHDR
    {
        public IntPtr lpData;
        public int dwBufferLength;
        public int dwBytesRecorded;
        public IntPtr dwUser;
        public int dwFlags;
        public int dwLoops;
        public IntPtr lpNext;
        public int reserved;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct WAVEINCAPSW
    {
        public ushort wMid;
        public ushort wPid;
        public uint vDriverVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szPname;
        public uint dwFormats;
        public ushort wChannels;
        public ushort wReserved1;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct WAVEOUTCAPSW
    {
        public ushort wMid;
        public ushort wPid;
        public uint vDriverVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szPname;
        public uint dwFormats;
        public ushort wChannels;
        public ushort wReserved1;
        public uint dwSupport;
    }
}
