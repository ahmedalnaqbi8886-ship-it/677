using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace EaxReverbEngine
{
	// Token: 0x02000015 RID: 21
	public static class Probe
	{
		// Token: 0x06000089 RID: 137 RVA: 0x000071D8 File Offset: 0x000053D8
		private static int Resolve(string spec, string[] list, out string name)
		{
			name = null;
			int result;
			int num;
			if (string.IsNullOrEmpty(spec))
			{
				result = -1;
			}
			else if (spec.Equals("default", StringComparison.OrdinalIgnoreCase))
			{
				result = -1;
			}
			else if (int.TryParse(spec, out num))
			{
				if (num >= 0 && num < list.Length)
				{
					name = list[num];
					result = num;
				}
				else
				{
					result = -3;
				}
			}
			else
			{
				int num2 = Wave.Find(list, spec);
				if (num2 < 0)
				{
					result = -3;
				}
				else
				{
					name = list[num2];
					result = num2;
				}
			}
			return result;
		}

		// Token: 0x0600008A RID: 138 RVA: 0x00007268 File Offset: 0x00005468
		private static string V(int r)
		{
			string result;
			if (r == 0)
			{
				result = "ok";
			}
			else if (r == 4)
			{
				result = "error 4 (device busy/allocated)";
			}
			else if (r == 2)
			{
				result = "error 2 (bad device id)";
			}
			else if (r == 3)
			{
				result = "error 3 (not enabled)";
			}
			else
			{
				result = "error " + r;
			}
			return result;
		}

		// Token: 0x0600008B RID: 139 RVA: 0x000072D8 File Offset: 0x000054D8
		public static string Capture(string devSpec, int seconds)
		{
			string[] list = Wave.ListIn();
			string text;
			int num = Probe.Resolve(devSpec, list, out text);
			string result;
			int num2;
			int num3;
			if (num == -3)
			{
				result = "capture device not found: " + devSpec;
			}
			else if (!Wave.PickInFormatReal(num, out num2, out num3))
			{
				result = "cannot open capture device " + (text ?? "default") + " with any format";
			}
			else
			{
				EventWaitHandle eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
				WAVEFORMATEX waveformatex = WAVEFORMATEX.Make(num2, num3);
				IntPtr hwi;
				int num4 = Wave.waveInOpen(out hwi, (uint)num, ref waveformatex, eventWaitHandle.SafeWaitHandle.DangerousGetHandle(), IntPtr.Zero, 327680U);
				if (num4 != 0)
				{
					eventWaitHandle.Dispose();
					result = "waveInOpen: " + Probe.V(num4);
				}
				else
				{
					int num5 = num2 * num3 * 2 / 50;
					if (num5 < 512)
					{
						num5 = 512;
					}
					num5 -= num5 % (num3 * 2);
					int num6 = 8;
					WAVEHDR[] array = new WAVEHDR[num6];
					IntPtr[] array2 = new IntPtr[num6];
					for (int i = 0; i < num6; i++)
					{
						array2[i] = Marshal.AllocHGlobal(num5);
						Marshal.Copy(new byte[num5], 0, array2[i], num5);
						array[i].lpData = array2[i];
						array[i].dwBufferLength = num5;
						Wave.waveInPrepareHeader(hwi, ref array[i], Marshal.SizeOf(typeof(WAVEHDR)));
						Wave.waveInAddBuffer(hwi, ref array[i], Marshal.SizeOf(typeof(WAVEHDR)));
					}
					Wave.waveInStart(hwi);
					float num7 = 0f;
					double num8 = 0.0;
					long num9 = 0L;
					int num10 = Environment.TickCount + seconds * 1000;
					while (Environment.TickCount < num10)
					{
						eventWaitHandle.WaitOne(80);
						for (int i = 0; i < num6; i++)
						{
							if ((array[i].dwFlags & 1) != 0)
							{
								int dwBytesRecorded = array[i].dwBytesRecorded;
								short[] array3 = new short[dwBytesRecorded / 2];
								Marshal.Copy(array[i].lpData, array3, 0, array3.Length);
								for (int j = 0; j < array3.Length; j++)
								{
									float num11 = Math.Abs((float)array3[j] / 32768f);
									if (num11 > num7)
									{
										num7 = num11;
									}
									num8 += (double)array3[j] * (double)array3[j];
								}
								num9 += (long)array3.Length;
								Wave.waveInAddBuffer(hwi, ref array[i], Marshal.SizeOf(typeof(WAVEHDR)));
							}
						}
					}
					Wave.waveInReset(hwi);
					for (int i = 0; i < num6; i++)
					{
						Wave.waveInUnprepareHeader(hwi, ref array[i], Marshal.SizeOf(typeof(WAVEHDR)));
						Marshal.FreeHGlobal(array2[i]);
					}
					Wave.waveInClose(hwi);
					eventWaitHandle.Dispose();
					double num12 = (num9 > 0L) ? (Math.Sqrt(num8 / (double)num9) / 32768.0) : 0.0;
					string text2 = (num7 >= 0.001f) ? "SIGNAL OK" : "NO SIGNAL (silence)";
					result = string.Format("capture [{0}] '{1}' {2}Hz ch={3} peak={4:F4} rms={5:F5} samples={6} -> {7}", new object[]
					{
						num,
						text ?? "default",
						num2,
						num3,
						num7,
						num12,
						num9,
						text2
					});
				}
			}
			return result;
		}

		// Token: 0x0600008C RID: 140 RVA: 0x00007704 File Offset: 0x00005904
		public static string Route(string outSpec, string inSpec, int seconds)
		{
			string[] list = Wave.ListOut();
			string[] list2 = Wave.ListIn();
			string text;
			int num = Probe.Resolve(outSpec, list, out text);
			string text2;
			int num2 = Probe.Resolve(inSpec, list2, out text2);
			string result;
			int num3;
			int num4;
			if (num == -3)
			{
				result = "render device not found: " + outSpec;
			}
			else if (num2 == -3)
			{
				result = "capture device not found: " + inSpec;
			}
			else if (!Wave.PickInFormatReal(num2, out num3, out num4))
			{
				result = "cannot open capture " + text2 + " with any format";
			}
			else
			{
				int num5;
				int num6;
				if (Wave.TestOpenOut(num, num3, num4))
				{
					num5 = num3;
					num6 = num4;
				}
				else if (!Wave.PickOutFormatReal(num, num3, num4, out num5, out num6))
				{
					return "cannot open render " + text + " with any format";
				}
				EventWaitHandle eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
				IntPtr callback = eventWaitHandle.SafeWaitHandle.DangerousGetHandle();
				WAVEFORMATEX waveformatex = WAVEFORMATEX.Make(num5, num6);
				IntPtr hwo;
				int num7 = Wave.waveOutOpen(out hwo, (uint)num, ref waveformatex, callback, IntPtr.Zero, 327680U);
				if (num7 != 0)
				{
					eventWaitHandle.Dispose();
					result = "waveOutOpen " + text + ": " + Probe.V(num7);
				}
				else
				{
					WAVEFORMATEX waveformatex2 = WAVEFORMATEX.Make(num3, num4);
					IntPtr hwi;
					num7 = Wave.waveInOpen(out hwi, (uint)num2, ref waveformatex2, callback, IntPtr.Zero, 327680U);
					if (num7 != 0)
					{
						Wave.waveOutClose(hwo);
						eventWaitHandle.Dispose();
						result = "waveInOpen " + text2 + ": " + Probe.V(num7);
					}
					else
					{
						int num8 = num5 * num6 * 2 / 50;
						if (num8 < 512)
						{
							num8 = 512;
						}
						num8 -= num8 % (num6 * 2);
						int num9 = num3 * num4 * 2 / 50;
						if (num9 < 512)
						{
							num9 = 512;
						}
						num9 -= num9 % (num4 * 2);
						int num10 = 8;
						int num11 = 8;
						WAVEHDR[] array = new WAVEHDR[num10];
						IntPtr[] array2 = new IntPtr[num10];
						bool[] array3 = new bool[num10];
						for (int i = 0; i < num10; i++)
						{
							array2[i] = Marshal.AllocHGlobal(num8);
							Marshal.Copy(new byte[num8], 0, array2[i], num8);
							array[i].lpData = array2[i];
							array[i].dwBufferLength = num8;
							Wave.waveOutPrepareHeader(hwo, ref array[i], Marshal.SizeOf(typeof(WAVEHDR)));
						}
						WAVEHDR[] array4 = new WAVEHDR[num11];
						IntPtr[] array5 = new IntPtr[num11];
						for (int i = 0; i < num11; i++)
						{
							array5[i] = Marshal.AllocHGlobal(num9);
							Marshal.Copy(new byte[num9], 0, array5[i], num9);
							array4[i].lpData = array5[i];
							array4[i].dwBufferLength = num9;
							Wave.waveInPrepareHeader(hwi, ref array4[i], Marshal.SizeOf(typeof(WAVEHDR)));
							Wave.waveInAddBuffer(hwi, ref array4[i], Marshal.SizeOf(typeof(WAVEHDR)));
						}
						Wave.waveInStart(hwi);
						uint num12 = 305419896U;
						float num13 = 0f;
						long num14 = 0L;
						long num15 = 0L;
						int num16 = Environment.TickCount + seconds * 1000;
						while (Environment.TickCount < num16)
						{
							for (int i = 0; i < num10; i++)
							{
								if (!array3[i] || (array[i].dwFlags & 1) != 0)
								{
									short[] array6 = new short[num8 / 2];
									for (int j = 0; j < array6.Length; j++)
									{
										num12 = num12 * 1103515245U + 12345U;
										array6[j] = (short)((short)(num12 >> 16 & 65535U) * 3 / 8);
									}
									Marshal.Copy(array6, 0, array2[i], array6.Length);
									Wave.waveOutWrite(hwo, ref array[i], Marshal.SizeOf(typeof(WAVEHDR)));
									array3[i] = true;
									num15 += (long)(num8 / (num6 * 2));
								}
							}
							for (int i = 0; i < num11; i++)
							{
								if ((array4[i].dwFlags & 1) != 0)
								{
									int dwBytesRecorded = array4[i].dwBytesRecorded;
									short[] array6 = new short[dwBytesRecorded / 2];
									Marshal.Copy(array4[i].lpData, array6, 0, array6.Length);
									for (int j = 0; j < array6.Length; j++)
									{
										float num17 = Math.Abs((float)array6[j] / 32768f);
										if (num17 > num13)
										{
											num13 = num17;
										}
									}
									num14 += (long)array6.Length;
									Wave.waveInAddBuffer(hwi, ref array4[i], Marshal.SizeOf(typeof(WAVEHDR)));
								}
							}
							eventWaitHandle.WaitOne(40);
						}
						Wave.waveOutReset(hwo);
						Wave.waveInReset(hwi);
						for (int i = 0; i < num10; i++)
						{
							Wave.waveOutUnprepareHeader(hwo, ref array[i], Marshal.SizeOf(typeof(WAVEHDR)));
							Marshal.FreeHGlobal(array2[i]);
						}
						for (int i = 0; i < num11; i++)
						{
							Wave.waveInUnprepareHeader(hwi, ref array4[i], Marshal.SizeOf(typeof(WAVEHDR)));
							Marshal.FreeHGlobal(array5[i]);
						}
						Wave.waveOutClose(hwo);
						Wave.waveInClose(hwi);
						eventWaitHandle.Dispose();
						string text3 = (num13 >= 0.001f) ? "ROUTING OK (signal arrived)" : "NO SIGNAL (not routed)";
						result = string.Format("out [{0}] '{1}' -> in [{2}] '{3}': outFrames={4} inSamples={5} peak={6:F4} -> {7}", new object[]
						{
							num,
							text,
							num2,
							text2,
							num15,
							num14,
							num13,
							text3
						});
					}
				}
			}
			return result;
		}
	}
}
