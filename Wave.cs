using System;
using System.Runtime.InteropServices;

namespace EaxReverbEngine
{
	// Token: 0x0200000E RID: 14
	internal static class Wave
	{
		// Token: 0x0600003E RID: 62
		[DllImport("winmm.dll")]
		public static extern int waveInGetNumDevs();

		// Token: 0x0600003F RID: 63
		[DllImport("winmm.dll", CharSet = CharSet.Unicode)]
		public static extern int waveInGetDevCapsW(IntPtr uDeviceId, out WAVEINCAPSW caps, int cb);

		// Token: 0x06000040 RID: 64
		[DllImport("winmm.dll")]
		public static extern int waveInOpen(out IntPtr hwi, uint uDeviceId, ref WAVEFORMATEX fmt, IntPtr callback, IntPtr instance, uint flags);

		// Token: 0x06000041 RID: 65
		[DllImport("winmm.dll")]
		public static extern int waveInPrepareHeader(IntPtr hwi, ref WAVEHDR hdr, int cbwh);

		// Token: 0x06000042 RID: 66
		[DllImport("winmm.dll")]
		public static extern int waveInUnprepareHeader(IntPtr hwi, ref WAVEHDR hdr, int cbwh);

		// Token: 0x06000043 RID: 67
		[DllImport("winmm.dll")]
		public static extern int waveInAddBuffer(IntPtr hwi, ref WAVEHDR hdr, int cbwh);

		// Token: 0x06000044 RID: 68
		[DllImport("winmm.dll")]
		public static extern int waveInStart(IntPtr hwi);

		// Token: 0x06000045 RID: 69
		[DllImport("winmm.dll")]
		public static extern int waveInReset(IntPtr hwi);

		// Token: 0x06000046 RID: 70
		[DllImport("winmm.dll")]
		public static extern int waveInStop(IntPtr hwi);

		// Token: 0x06000047 RID: 71
		[DllImport("winmm.dll")]
		public static extern int waveInClose(IntPtr hwi);

		// Token: 0x06000048 RID: 72
		[DllImport("winmm.dll")]
		public static extern int waveOutGetNumDevs();

		// Token: 0x06000049 RID: 73
		[DllImport("winmm.dll", CharSet = CharSet.Unicode)]
		public static extern int waveOutGetDevCapsW(IntPtr uDeviceId, out WAVEOUTCAPSW caps, int cb);

		// Token: 0x0600004A RID: 74
		[DllImport("winmm.dll")]
		public static extern int waveOutOpen(out IntPtr hwo, uint uDeviceId, ref WAVEFORMATEX fmt, IntPtr callback, IntPtr instance, uint flags);

		// Token: 0x0600004B RID: 75
		[DllImport("winmm.dll")]
		public static extern int waveOutPrepareHeader(IntPtr hwo, ref WAVEHDR hdr, int cbwh);

		// Token: 0x0600004C RID: 76
		[DllImport("winmm.dll")]
		public static extern int waveOutUnprepareHeader(IntPtr hwo, ref WAVEHDR hdr, int cbwh);

		// Token: 0x0600004D RID: 77
		[DllImport("winmm.dll")]
		public static extern int waveOutWrite(IntPtr hwo, ref WAVEHDR hdr, int cbwh);

		// Token: 0x0600004E RID: 78
		[DllImport("winmm.dll")]
		public static extern int waveOutReset(IntPtr hwo);

		// Token: 0x0600004F RID: 79
		[DllImport("winmm.dll")]
		public static extern int waveOutClose(IntPtr hwo);

		// Token: 0x06000050 RID: 80 RVA: 0x00002E60 File Offset: 0x00001060
		public static string[] ListIn()
		{
			int num = Wave.waveInGetNumDevs();
			string[] array = new string[num];
			for (int i = 0; i < num; i++)
			{
				WAVEINCAPSW waveincapsw;
				array[i] = ((Wave.waveInGetDevCapsW(new IntPtr(i), out waveincapsw, Marshal.SizeOf(typeof(WAVEINCAPSW))) == 0) ? waveincapsw.szPname : "?");
			}
			return array;
		}

		// Token: 0x06000051 RID: 81 RVA: 0x00002EC8 File Offset: 0x000010C8
		public static string[] ListOut()
		{
			int num = Wave.waveOutGetNumDevs();
			string[] array = new string[num];
			for (int i = 0; i < num; i++)
			{
				WAVEOUTCAPSW waveoutcapsw;
				array[i] = ((Wave.waveOutGetDevCapsW(new IntPtr(i), out waveoutcapsw, Marshal.SizeOf(typeof(WAVEOUTCAPSW))) == 0) ? waveoutcapsw.szPname : "?");
			}
			return array;
		}

		// Token: 0x06000052 RID: 82 RVA: 0x00002F30 File Offset: 0x00001130
		public static int Find(string[] list, string substr)
		{
			int result;
			if (string.IsNullOrEmpty(substr))
			{
				result = -1;
			}
			else
			{
				for (int i = 0; i < list.Length; i++)
				{
					if (list[i] != null && list[i].IndexOf(substr, StringComparison.OrdinalIgnoreCase) >= 0)
					{
						return i;
					}
				}
				result = -2;
			}
			return result;
		}

		// Token: 0x06000053 RID: 83 RVA: 0x00002F88 File Offset: 0x00001188
		public static bool QueryIn(int dev, int rate, int ch)
		{
			WAVEFORMATEX waveformatex = WAVEFORMATEX.Make(rate, ch);
			IntPtr intPtr;
			return Wave.waveInOpen(out intPtr, (uint)dev, ref waveformatex, IntPtr.Zero, IntPtr.Zero, 1U) == 0;
		}

		// Token: 0x06000054 RID: 84 RVA: 0x00002FBC File Offset: 0x000011BC
		public static bool QueryOut(int dev, int rate, int ch)
		{
			WAVEFORMATEX waveformatex = WAVEFORMATEX.Make(rate, ch);
			IntPtr intPtr;
			return Wave.waveOutOpen(out intPtr, (uint)dev, ref waveformatex, IntPtr.Zero, IntPtr.Zero, 1U) == 0;
		}

		// Token: 0x06000055 RID: 85 RVA: 0x00002FF0 File Offset: 0x000011F0
		public static bool TestOpenIn(int dev, int rate, int ch)
		{
			WAVEFORMATEX waveformatex = WAVEFORMATEX.Make(rate, ch);
			IntPtr hwi;
			int num = Wave.waveInOpen(out hwi, (uint)dev, ref waveformatex, IntPtr.Zero, IntPtr.Zero, 0U);
			bool result;
			if (num != 0)
			{
				result = false;
			}
			else
			{
				Wave.waveInClose(hwi);
				result = true;
			}
			return result;
		}

		// Token: 0x06000056 RID: 86 RVA: 0x00003038 File Offset: 0x00001238
		public static bool TestOpenOut(int dev, int rate, int ch)
		{
			WAVEFORMATEX waveformatex = WAVEFORMATEX.Make(rate, ch);
			IntPtr hwo;
			int num = Wave.waveOutOpen(out hwo, (uint)dev, ref waveformatex, IntPtr.Zero, IntPtr.Zero, 0U);
			bool result;
			if (num != 0)
			{
				result = false;
			}
			else
			{
				Wave.waveOutClose(hwo);
				result = true;
			}
			return result;
		}

		// Token: 0x06000057 RID: 87 RVA: 0x00003080 File Offset: 0x00001280
		public static bool PickInFormat(int dev, out int rate, out int ch)
		{
			int[] array = new int[]
			{
				1,
				2
			};
			for (int i = 0; i < array.Length; i++)
			{
				for (int j = 0; j < Wave.Rates.Length; j++)
				{
					if (Wave.QueryIn(dev, Wave.Rates[j], array[i]))
					{
						rate = Wave.Rates[j];
						ch = array[i];
						return true;
					}
				}
			}
			rate = 0;
			ch = 0;
			return false;
		}

		// Token: 0x06000058 RID: 88 RVA: 0x00003108 File Offset: 0x00001308
		public static bool PickOutFormat(int dev, int wantRate, int wantCh, out int rate, out int ch)
		{
			bool result;
			if (wantCh >= 1 && Wave.QueryOut(dev, wantRate, wantCh))
			{
				rate = wantRate;
				ch = wantCh;
				result = true;
			}
			else
			{
				int[] array = new int[]
				{
					2,
					1
				};
				for (int i = 0; i < array.Length; i++)
				{
					for (int j = 0; j < Wave.Rates.Length; j++)
					{
						if (Wave.QueryOut(dev, Wave.Rates[j], array[i]))
						{
							rate = Wave.Rates[j];
							ch = array[i];
							return true;
						}
					}
				}
				rate = 0;
				ch = 0;
				result = false;
			}
			return result;
		}

		// Token: 0x06000059 RID: 89 RVA: 0x000031B8 File Offset: 0x000013B8
		public static bool PickInFormatReal(int dev, out int rate, out int ch)
		{
			int[] array = new int[]
			{
				1,
				2
			};
			for (int i = 0; i < array.Length; i++)
			{
				for (int j = 0; j < Wave.Rates.Length; j++)
				{
					if (Wave.TestOpenIn(dev, Wave.Rates[j], array[i]))
					{
						rate = Wave.Rates[j];
						ch = array[i];
						return true;
					}
				}
			}
			rate = 0;
			ch = 0;
			return false;
		}

		// Token: 0x0600005A RID: 90 RVA: 0x00003240 File Offset: 0x00001440
		public static bool PickOutFormatReal(int dev, int wantRate, int wantCh, out int rate, out int ch)
		{
			bool result;
			if (wantCh >= 1 && Wave.TestOpenOut(dev, wantRate, wantCh))
			{
				rate = wantRate;
				ch = wantCh;
				result = true;
			}
			else
			{
				int[] array = new int[]
				{
					2,
					1
				};
				for (int i = 0; i < array.Length; i++)
				{
					for (int j = 0; j < Wave.Rates.Length; j++)
					{
						if (Wave.TestOpenOut(dev, Wave.Rates[j], array[i]))
						{
							rate = Wave.Rates[j];
							ch = array[i];
							return true;
						}
					}
				}
				rate = 0;
				ch = 0;
				result = false;
			}
			return result;
		}

		// Token: 0x04000048 RID: 72
		public const uint WAVE_MAPPER = 4294967295U;

		// Token: 0x04000049 RID: 73
		public const uint CALLBACK_EVENT = 327680U;

		// Token: 0x0400004A RID: 74
		public const int WHDR_DONE = 1;

		// Token: 0x0400004B RID: 75
		public const int WHDR_PREPARED = 2;

		// Token: 0x0400004C RID: 76
		public const uint WAVE_FORMAT_QUERY = 1U;

		// Token: 0x0400004D RID: 77
		private static readonly int[] Rates = new int[]
		{
			48000,
			44100,
			32000,
			24000,
			22050,
			16000,
			8000
		};
	}
}
