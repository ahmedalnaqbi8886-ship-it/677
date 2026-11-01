using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace EaxReverbEngine
{
	// Token: 0x0200000F RID: 15
	internal sealed class AudioEngine : IDisposable
	{
		// Token: 0x0600005C RID: 92 RVA: 0x00003324 File Offset: 0x00001524
		public void Start(VstPlugin plugin, int inDev, int inRate, int inCh, int inDev2, int in2Ch, int outDev, int outRate, int outCh, int bufMs, int bufCount)
		{
			this._plugin = plugin;
			this._inRate = inRate;
			this._inCh = inCh;
			this._outRate = outRate;
			this._outCh = outCh;
			this._inBlock = inCh * 2;
			this._outBlock = outCh * 2;
			this._hdrSize = Marshal.SizeOf(typeof(WAVEHDR));
			int num = Math.Max(64, inRate * bufMs / 1000);
			this._maxInFrames = num;
			this._maxOutFrames = (int)Math.Ceiling((double)num * (double)outRate / (double)inRate) + 8;
			this._fL = new float[this._maxInFrames];
			this._fR = new float[this._maxInFrames];
			this._gL = new float[this._maxInFrames];
			this._gR = new float[this._maxInFrames];
			this._rsL = new float[this._maxOutFrames];
			this._rsR = new float[this._maxOutFrames];
			this._inShorts = new short[this._maxInFrames * inCh];
			this._outShorts = new short[this._maxOutFrames * outCh];
			this._evIn = new EventWaitHandle(false, EventResetMode.AutoReset);
			this._evOut = new EventWaitHandle(false, EventResetMode.AutoReset);
			try
			{
				WAVEFORMATEX waveformatex = WAVEFORMATEX.Make(inRate, inCh);
				int num2 = Wave.waveInOpen(out this._hIn, (uint)inDev, ref waveformatex, this._evIn.SafeWaitHandle.DangerousGetHandle(), IntPtr.Zero, 327680U);
				if (num2 != 0)
				{
					throw new InvalidOperationException("waveInOpen error " + num2);
				}
				if (inDev2 >= 0 && in2Ch > 0)
				{
					this._in2Ch = in2Ch;
					this._inBlock2 = in2Ch * 2;
					this._inShorts2 = new short[this._maxInFrames * in2Ch];
					this._mixQueueCap = this._maxInFrames * 4;
					this._mixQueueL = new float[this._mixQueueCap];
					this._mixQueueR = new float[this._mixQueueCap];
					WAVEFORMATEX waveformatex2 = WAVEFORMATEX.Make(inRate, in2Ch);
					num2 = Wave.waveInOpen(out this._hIn2, (uint)inDev2, ref waveformatex2, this._evIn.SafeWaitHandle.DangerousGetHandle(), IntPtr.Zero, 327680U);
					if (num2 != 0)
					{
						Log.Write("waveIn2 open failed error {0}, continuing without second input", new object[]
						{
							num2
						});
						this._hIn2 = IntPtr.Zero;
					}
				}
				WAVEFORMATEX waveformatex3 = WAVEFORMATEX.Make(outRate, outCh);
				num2 = Wave.waveOutOpen(out this._hOut, (uint)outDev, ref waveformatex3, this._evOut.SafeWaitHandle.DangerousGetHandle(), IntPtr.Zero, 327680U);
				if (num2 != 0)
				{
					if (this._hIn2 != IntPtr.Zero)
					{
						Wave.waveInClose(this._hIn2);
						this._hIn2 = IntPtr.Zero;
					}
					Wave.waveInClose(this._hIn);
					this._hIn = IntPtr.Zero;
					throw new InvalidOperationException("waveOutOpen error " + num2);
				}
				Log.Write("waveIn dev={0} {1}Hz ch={2}; waveOut dev={3} {4}Hz ch={5}", new object[]
				{
					inDev,
					inRate,
					inCh,
					outDev,
					outRate,
					outCh
				});
				if (this._hIn2 != IntPtr.Zero)
				{
					Log.Write("waveIn2 dev={0} {1}Hz ch={2} (mixed into reverb input)", new object[]
					{
						inDev2,
						inRate,
						in2Ch
					});
				}
				this._inHdr = new WAVEHDR[bufCount];
				this._outHdr = new WAVEHDR[bufCount];
				this._inData = new IntPtr[bufCount];
				this._outData = new IntPtr[bufCount];
				this._outBusy = new bool[bufCount];
				this._pinInHdr = GCHandle.Alloc(this._inHdr, GCHandleType.Pinned);
				this._pinOutHdr = GCHandle.Alloc(this._outHdr, GCHandleType.Pinned);
				int num3 = this._maxInFrames * this._inBlock;
				int num4 = this._maxOutFrames * this._outBlock;
				for (int i = 0; i < bufCount; i++)
				{
					this._inData[i] = Marshal.AllocHGlobal(num3);
					this._inHdr[i].lpData = this._inData[i];
					this._inHdr[i].dwBufferLength = num3;
					num2 = Wave.waveInPrepareHeader(this._hIn, ref this._inHdr[i], this._hdrSize);
					if (num2 != 0)
					{
						throw new InvalidOperationException("waveInPrepareHeader error " + num2);
					}
					num2 = Wave.waveInAddBuffer(this._hIn, ref this._inHdr[i], this._hdrSize);
					if (num2 != 0)
					{
						throw new InvalidOperationException("waveInAddBuffer error " + num2);
					}
				}
				if (this._hIn2 != IntPtr.Zero)
				{
					this._inHdr2 = new WAVEHDR[bufCount];
					this._inData2 = new IntPtr[bufCount];
					int num5 = this._maxInFrames * this._inBlock2;
					for (int i = 0; i < bufCount; i++)
					{
						this._inData2[i] = Marshal.AllocHGlobal(num5);
						this._inHdr2[i].lpData = this._inData2[i];
						this._inHdr2[i].dwBufferLength = num5;
						num2 = Wave.waveInPrepareHeader(this._hIn2, ref this._inHdr2[i], this._hdrSize);
						if (num2 != 0)
						{
							throw new InvalidOperationException("waveInPrepareHeader2 error " + num2);
						}
						num2 = Wave.waveInAddBuffer(this._hIn2, ref this._inHdr2[i], this._hdrSize);
						if (num2 != 0)
						{
							throw new InvalidOperationException("waveInAddBuffer2 error " + num2);
						}
					}
				}
				for (int i = 0; i < bufCount; i++)
				{
					this._outData[i] = Marshal.AllocHGlobal(num4);
					this._outHdr[i].lpData = this._outData[i];
					this._outHdr[i].dwBufferLength = num4;
					num2 = Wave.waveOutPrepareHeader(this._hOut, ref this._outHdr[i], this._hdrSize);
					if (num2 != 0)
					{
						throw new InvalidOperationException("waveOutPrepareHeader error " + num2);
					}
				}
				this._running = true;
				this._thread = new Thread(new ThreadStart(this.ThreadMain));
				this._thread.IsBackground = true;
				this._thread.Name = "EaxAudioPump";
				this._thread.Start();
				num2 = Wave.waveInStart(this._hIn);
				if (num2 != 0)
				{
					throw new InvalidOperationException("waveInStart error " + num2);
				}
				if (this._hIn2 != IntPtr.Zero)
				{
					num2 = Wave.waveInStart(this._hIn2);
					if (num2 != 0)
					{
						Log.Write("waveIn2 start error " + num2);
					}
				}
				Log.Write("audio pump started: {0} buffers x {1}ms", new object[]
				{
					bufCount,
					bufMs
				});
			}
			catch (Exception ex)
			{
				Log.Write("audio start failed: " + ex.Message);
				this.Stop();
				throw;
			}
		}

		// Token: 0x0600005D RID: 93 RVA: 0x00003B4C File Offset: 0x00001D4C
		private void ThreadMain()
		{
			while (this._running)
			{
				if (this._evIn.WaitOne(250))
				{
					if (!this._running)
					{
						break;
					}
					if (this._hIn2 != IntPtr.Zero && this._inHdr2 != null)
					{
						for (int i = 0; i < this._inHdr2.Length; i++)
						{
							if (!this._running)
							{
								break;
							}
							int dwFlags = this._inHdr2[i].dwFlags;
							if ((dwFlags & 1) != 0)
							{
								int dwBytesRecorded = this._inHdr2[i].dwBytesRecorded;
								try
								{
									if (dwBytesRecorded > 0)
									{
										this.ProcessIn2(i, dwBytesRecorded);
									}
								}
								catch (Exception ex)
								{
									Log.Write("process2 error: " + ex.Message);
									Thread.Sleep(50);
								}
								this._inHdr2[i].dwBytesRecorded = 0;
								Wave.waveInAddBuffer(this._hIn2, ref this._inHdr2[i], this._hdrSize);
							}
						}
					}
					for (int i = 0; i < this._inHdr.Length; i++)
					{
						if (!this._running)
						{
							break;
						}
						int dwFlags = this._inHdr[i].dwFlags;
						if ((dwFlags & 1) != 0)
						{
							int dwBytesRecorded = this._inHdr[i].dwBytesRecorded;
							try
							{
								if (dwBytesRecorded > 0)
								{
									this.ProcessIn(i, dwBytesRecorded);
								}
							}
							catch (Exception ex)
							{
								Log.Write("process error: " + ex.Message);
								Thread.Sleep(50);
							}
							this._inHdr[i].dwBytesRecorded = 0;
							Wave.waveInAddBuffer(this._hIn, ref this._inHdr[i], this._hdrSize);
						}
					}
				}
			}
		}

		// Token: 0x0600005E RID: 94 RVA: 0x00003D9C File Offset: 0x00001F9C
		private void ProcessIn2(int index, int bytes)
		{
			int num = bytes / this._inBlock2;
			if (num > this._maxInFrames)
			{
				num = this._maxInFrames;
			}
			if (num > 0)
			{
				Marshal.Copy(this._inData2[index], this._inShorts2, 0, num * this._in2Ch);
				float num2 = 0f;
				while (Interlocked.CompareExchange(ref this._mixQueueLock, 1, 0) != 0)
				{
					Thread.Sleep(0);
				}
				for (int i = 0; i < num; i++)
				{
					if (this._mixQueueCount >= this._mixQueueCap)
					{
						Interlocked.Exchange(ref this._mixQueueLock, 0);
						if (num2 > this._in2Peak)
						{
							this._in2Peak = num2;
						}
						return;
					}
					float num4;
					float num3;
					if (this._in2Ch == 1)
					{
						num3 = (num4 = (float)this._inShorts2[i] / 32768f);
					}
					else
					{
						num4 = (float)this._inShorts2[i * 2] / 32768f;
						num3 = (float)this._inShorts2[i * 2 + 1] / 32768f;
					}
					float num5 = (num4 < 0f) ? (-num4) : num4;
					if (num5 > num2)
					{
						num2 = num5;
					}
					num5 = ((num3 < 0f) ? (-num3) : num3);
					if (num5 > num2)
					{
						num2 = num5;
					}
					this._mixQueueL[this._mixQueueCount] = num4;
					this._mixQueueR[this._mixQueueCount] = num3;
					this._mixQueueCount++;
				}
				Interlocked.Exchange(ref this._mixQueueLock, 0);
				if (num2 > this._in2Peak)
				{
					this._in2Peak = num2;
				}
			}
		}

		// Token: 0x0600005F RID: 95 RVA: 0x00003F74 File Offset: 0x00002174
		public float TakeIn2Peak()
		{
			float in2Peak = this._in2Peak;
			this._in2Peak = 0f;
			return in2Peak;
		}

		// Token: 0x06000060 RID: 96 RVA: 0x00003FA0 File Offset: 0x000021A0
		private void MixFromQueue(float[] fL, float[] fR, int frames)
		{
			if (this._mixQueueL != null && !(this._hIn2 == IntPtr.Zero))
			{
				int num = 0;
				while (Interlocked.CompareExchange(ref this._mixQueueLock, 1, 0) != 0)
				{
					Thread.Sleep(0);
				}
				int num2 = Math.Min(frames, this._mixQueueCount);
				for (int i = 0; i < num2; i++)
				{
					fL[i] += this._mixQueueL[i];
					fR[i] += this._mixQueueR[i];
					num++;
				}
				if (num < this._mixQueueCount)
				{
					int num3 = this._mixQueueCount - num;
					Array.Copy(this._mixQueueL, num, this._mixQueueL, 0, num3);
					Array.Copy(this._mixQueueR, num, this._mixQueueR, 0, num3);
					this._mixQueueCount = num3;
				}
				else
				{
					this._mixQueueCount = 0;
				}
				Interlocked.Exchange(ref this._mixQueueLock, 0);
			}
		}

		// Token: 0x06000061 RID: 97 RVA: 0x000040C0 File Offset: 0x000022C0
		private void ProcessIn(int index, int bytes)
		{
			int num = bytes / this._inBlock;
			if (num > this._maxInFrames)
			{
				num = this._maxInFrames;
			}
			if (num > 0)
			{
				Marshal.Copy(this._inData[index], this._inShorts, 0, num * this._inCh);
				double num2 = 0.0;
				float num3 = 0f;
				if (this._inCh == 1)
				{
					for (int i = 0; i < num; i++)
					{
						float num4 = (float)this._inShorts[i] / 32768f;
						this._fL[i] = num4;
						this._fR[i] = num4;
						num2 += (double)num4 * (double)num4;
						float num5 = (num4 < 0f) ? (-num4) : num4;
						if (num5 > num3)
						{
							num3 = num5;
						}
					}
				}
				else
				{
					for (int i = 0; i < num; i++)
					{
						float num6 = (float)this._inShorts[i * 2] / 32768f;
						float num7 = (float)this._inShorts[i * 2 + 1] / 32768f;
						this._fL[i] = num6;
						this._fR[i] = num7;
						num2 += (double)num6 * (double)num6 + (double)num7 * (double)num7;
						float num5 = (num6 < 0f) ? (-num6) : num6;
						if (num5 > num3)
						{
							num3 = num5;
						}
						num5 = ((num7 < 0f) ? (-num7) : num7);
						if (num5 > num3)
						{
							num3 = num5;
						}
					}
				}
				if (this._hIn2 != IntPtr.Zero)
				{
					this.MixFromQueue(this._fL, this._fR, num);
				}
				this._plugin.Process(this._fL, this._fR, this._gL, this._gR, num);
				if (this.Fx != null)
				{
					this.Fx.Process(this._gL, this._gR, num, this._inRate);
				}
				float[] array = this._gL;
				float[] array2 = this._gR;
				int num8 = num;
				if (this._outRate != this._inRate)
				{
					num8 = this.Resample(num, this._gL, this._gR, this._rsL, this._rsR);
					array = this._rsL;
					array2 = this._rsR;
				}
				double num9 = 0.0;
				float num10 = 0f;
				for (int i = 0; i < num8; i++)
				{
					float num6 = array[i];
					float num7 = array2[i];
					if (float.IsNaN(num6))
					{
						num6 = 0f;
					}
					if (float.IsNaN(num7))
					{
						num7 = 0f;
					}
					if (num6 > 1f)
					{
						num6 = 1f;
					}
					else if (num6 < -1f)
					{
						num6 = -1f;
					}
					if (num7 > 1f)
					{
						num7 = 1f;
					}
					else if (num7 < -1f)
					{
						num7 = -1f;
					}
					array[i] = num6;
					array2[i] = num7;
					num9 += (double)num6 * (double)num6 + (double)num7 * (double)num7;
					float num5 = (num6 < 0f) ? (-num6) : num6;
					if (num5 > num10)
					{
						num10 = num5;
					}
					num5 = ((num7 < 0f) ? (-num7) : num7);
					if (num5 > num10)
					{
						num10 = num5;
					}
				}
				if (this._outCh == 1)
				{
					for (int i = 0; i < num8; i++)
					{
						float s = (array[i] + array2[i]) * 0.5f;
						this._outShorts[i] = AudioEngine.ToShort(s);
					}
				}
				else
				{
					for (int i = 0; i < num8; i++)
					{
						this._outShorts[i * 2] = AudioEngine.ToShort(array[i]);
						this._outShorts[i * 2 + 1] = AudioEngine.ToShort(array2[i]);
					}
				}
				int num11 = this.AcquireOut();
				if (num11 >= 0)
				{
					int num12 = num8 * this._outCh;
					Marshal.Copy(this._outShorts, 0, this._outData[num11], num12);
					this._outHdr[num11].dwBufferLength = num12 * 2;
					this._outBusy[num11] = true;
					int num13 = Wave.waveOutWrite(this._hOut, ref this._outHdr[num11], this._hdrSize);
					if (num13 != 0)
					{
						this._outBusy[num11] = false;
						if (!this._writeErrLogged)
						{
							this._writeErrLogged = true;
							Log.Write("waveOutWrite error " + num13);
						}
					}
				}
				lock (this._statsLock)
				{
					this._accIn += num2;
					this._accOut += num9;
					this._accFrames += (long)num;
					if (num3 > this._inPeak)
					{
						this._inPeak = num3;
					}
					if (num10 > this._outPeak)
					{
						this._outPeak = num10;
					}
				}
				Interlocked.Add(ref this._totalFrames, (long)num);
			}
		}

		// Token: 0x06000062 RID: 98 RVA: 0x00004654 File Offset: 0x00002854
		private static short ToShort(float s)
		{
			int num = (int)(s * 32767f);
			if (num > 32767)
			{
				num = 32767;
			}
			else if (num < -32768)
			{
				num = -32768;
			}
			return (short)num;
		}

		// Token: 0x06000063 RID: 99 RVA: 0x000046A0 File Offset: 0x000028A0
		private int AcquireOut()
		{
			while (this._running)
			{
				for (int i = 0; i < this._outHdr.Length; i++)
				{
					if (!this._outBusy[i] || (this._outHdr[i].dwFlags & 1) != 0)
					{
						return i;
					}
				}
				this._evOut.WaitOne(100);
			}
			return -1;
		}

		// Token: 0x06000064 RID: 100 RVA: 0x00004714 File Offset: 0x00002914
		private int Resample(int n, float[] inL, float[] inR, float[] outL, float[] outR)
		{
			double num = (double)this._inRate / (double)this._outRate;
			int num2 = 0;
			double num3 = this._t;
			float prevL = this._prevL;
			float prevR = this._prevR;
			while (num3 <= (double)(n - 1) && num2 < outL.Length)
			{
				int num4 = (int)Math.Floor(num3);
				double num5 = num3 - (double)num4;
				float num6;
				float num7;
				float num8;
				float num9;
				if (num4 < 0)
				{
					num6 = prevL;
					num7 = prevR;
					num8 = inL[0];
					num9 = inR[0];
				}
				else
				{
					num6 = inL[num4];
					num7 = inR[num4];
					num8 = ((num4 + 1 < n) ? inL[num4 + 1] : inL[n - 1]);
					num9 = ((num4 + 1 < n) ? inR[num4 + 1] : inR[n - 1]);
				}
				outL[num2] = (float)((double)num6 + (double)(num8 - num6) * num5);
				outR[num2] = (float)((double)num7 + (double)(num9 - num7) * num5);
				num2++;
				num3 += num;
			}
			this._prevL = inL[n - 1];
			this._prevR = inR[n - 1];
			this._t = num3 - (double)n;
			return num2;
		}

		// Token: 0x06000065 RID: 101 RVA: 0x00004830 File Offset: 0x00002A30
		public void GetRms(out double inRms, out double outRms, out long frames)
		{
			lock (this._statsLock)
			{
				frames = this._accFrames;
				inRms = ((frames > 0L) ? Math.Sqrt(this._accIn / (double)frames) : 0.0);
				outRms = ((frames > 0L) ? Math.Sqrt(this._accOut / (double)frames) : 0.0);
				this._accIn = 0.0;
				this._accOut = 0.0;
				this._accFrames = 0L;
			}
		}

		// Token: 0x06000066 RID: 102 RVA: 0x000048EC File Offset: 0x00002AEC
		public void GetPeaks(out float inPeak, out float outPeak)
		{
			inPeak = this._inPeak;
			outPeak = this._outPeak;
			this._inPeak = 0f;
			this._outPeak = 0f;
		}

		// Token: 0x17000009 RID: 9
		// (get) Token: 0x06000067 RID: 103 RVA: 0x00004920 File Offset: 0x00002B20
		public long TotalFrames
		{
			get
			{
				return Interlocked.Read(ref this._totalFrames);
			}
		}

		// Token: 0x06000068 RID: 104 RVA: 0x00004940 File Offset: 0x00002B40
		public void Stop()
		{
			if (!this._stopped)
			{
				this._stopped = true;
				this._running = false;
				try
				{
					if (this._hIn != IntPtr.Zero)
					{
						Wave.waveInReset(this._hIn);
					}
				}
				catch (Exception ex)
				{
					Log.Write("waveInReset: " + ex.Message);
				}
				try
				{
					if (this._hIn2 != IntPtr.Zero)
					{
						Wave.waveInReset(this._hIn2);
					}
				}
				catch
				{
				}
				try
				{
					if (this._evIn != null)
					{
						this._evIn.Set();
					}
				}
				catch
				{
				}
				try
				{
					if (this._evOut != null)
					{
						this._evOut.Set();
					}
				}
				catch
				{
				}
				if (this._thread != null)
				{
					if (!this._thread.Join(3000))
					{
						Log.Write("audio thread join timeout");
					}
					this._thread = null;
				}
				try
				{
					if (this._hOut != IntPtr.Zero)
					{
						Wave.waveOutReset(this._hOut);
					}
				}
				catch
				{
				}
				if (this._hIn2 != IntPtr.Zero)
				{
					if (this._inHdr2 != null)
					{
						for (int i = 0; i < this._inHdr2.Length; i++)
						{
							if (this._inData2 != null && this._inData2[i] != IntPtr.Zero)
							{
								Wave.waveInUnprepareHeader(this._hIn2, ref this._inHdr2[i], this._hdrSize);
							}
						}
					}
					Wave.waveInClose(this._hIn2);
					this._hIn2 = IntPtr.Zero;
				}
				if (this._hIn != IntPtr.Zero)
				{
					if (this._inHdr != null)
					{
						for (int i = 0; i < this._inHdr.Length; i++)
						{
							if (this._inData != null && this._inData[i] != IntPtr.Zero)
							{
								Wave.waveInUnprepareHeader(this._hIn, ref this._inHdr[i], this._hdrSize);
							}
						}
					}
					Wave.waveInClose(this._hIn);
					this._hIn = IntPtr.Zero;
				}
				if (this._hOut != IntPtr.Zero)
				{
					if (this._outHdr != null)
					{
						for (int i = 0; i < this._outHdr.Length; i++)
						{
							if (this._outData != null && this._outData[i] != IntPtr.Zero)
							{
								Wave.waveOutUnprepareHeader(this._hOut, ref this._outHdr[i], this._hdrSize);
							}
						}
					}
					Wave.waveOutClose(this._hOut);
					this._hOut = IntPtr.Zero;
				}
				if (this._inData != null)
				{
					for (int i = 0; i < this._inData.Length; i++)
					{
						if (this._inData[i] != IntPtr.Zero)
						{
							Marshal.FreeHGlobal(this._inData[i]);
							this._inData[i] = IntPtr.Zero;
						}
					}
				}
				if (this._inData2 != null)
				{
					for (int i = 0; i < this._inData2.Length; i++)
					{
						if (this._inData2[i] != IntPtr.Zero)
						{
							Marshal.FreeHGlobal(this._inData2[i]);
							this._inData2[i] = IntPtr.Zero;
						}
					}
				}
				if (this._outData != null)
				{
					for (int i = 0; i < this._outData.Length; i++)
					{
						if (this._outData[i] != IntPtr.Zero)
						{
							Marshal.FreeHGlobal(this._outData[i]);
							this._outData[i] = IntPtr.Zero;
						}
					}
				}
				if (this._pinInHdr.IsAllocated)
				{
					this._pinInHdr.Free();
				}
				if (this._pinOutHdr.IsAllocated)
				{
					this._pinOutHdr.Free();
				}
				if (this._evIn != null)
				{
					this._evIn.Close();
					this._evIn = null;
				}
				if (this._evOut != null)
				{
					this._evOut.Close();
					this._evOut = null;
				}
				Log.Write("audio pump stopped, total frames={0}", new object[]
				{
					Interlocked.Read(ref this._totalFrames)
				});
			}
		}

		// Token: 0x06000069 RID: 105 RVA: 0x00004EB8 File Offset: 0x000030B8
		public void Dispose()
		{
			this.Stop();
		}

		// Token: 0x0400004E RID: 78
		private VstPlugin _plugin;
		public FxChain Fx;

		// Token: 0x0400004F RID: 79
		private IntPtr _hIn;

		// Token: 0x04000050 RID: 80
		private IntPtr _hIn2;

		// Token: 0x04000051 RID: 81
		private IntPtr _hOut;

		// Token: 0x04000052 RID: 82
		private EventWaitHandle _evIn;

		// Token: 0x04000053 RID: 83
		private EventWaitHandle _evOut;

		// Token: 0x04000054 RID: 84
		private WAVEHDR[] _inHdr;

		// Token: 0x04000055 RID: 85
		private WAVEHDR[] _inHdr2;

		// Token: 0x04000056 RID: 86
		private WAVEHDR[] _outHdr;

		// Token: 0x04000057 RID: 87
		private GCHandle _pinInHdr;

		// Token: 0x04000058 RID: 88
		private GCHandle _pinOutHdr;

		// Token: 0x04000059 RID: 89
		private IntPtr[] _inData;

		// Token: 0x0400005A RID: 90
		private IntPtr[] _inData2;

		// Token: 0x0400005B RID: 91
		private IntPtr[] _outData;

		// Token: 0x0400005C RID: 92
		private bool[] _outBusy;

		// Token: 0x0400005D RID: 93
		private int _inRate;

		// Token: 0x0400005E RID: 94
		private int _inCh;

		// Token: 0x0400005F RID: 95
		private int _outRate;

		// Token: 0x04000060 RID: 96
		private int _outCh;

		// Token: 0x04000061 RID: 97
		private int _in2Ch;

		// Token: 0x04000062 RID: 98
		private int _inBlock2;

		// Token: 0x04000063 RID: 99
		private int _inBlock;

		// Token: 0x04000064 RID: 100
		private int _outBlock;

		// Token: 0x04000065 RID: 101
		private int _maxInFrames;

		// Token: 0x04000066 RID: 102
		private int _maxOutFrames;

		// Token: 0x04000067 RID: 103
		private int _hdrSize;

		// Token: 0x04000068 RID: 104
		private short[] _inShorts;

		// Token: 0x04000069 RID: 105
		private short[] _inShorts2;

		// Token: 0x0400006A RID: 106
		private short[] _outShorts;

		// Token: 0x0400006B RID: 107
		private float[] _fL;

		// Token: 0x0400006C RID: 108
		private float[] _fR;

		// Token: 0x0400006D RID: 109
		private float[] _gL;

		// Token: 0x0400006E RID: 110
		private float[] _gR;

		// Token: 0x0400006F RID: 111
		private float[] _rsL;

		// Token: 0x04000070 RID: 112
		private float[] _rsR;

		// Token: 0x04000071 RID: 113
		private float[] _mixQueueL;

		// Token: 0x04000072 RID: 114
		private float[] _mixQueueR;

		// Token: 0x04000073 RID: 115
		private int _mixQueueCount;

		// Token: 0x04000074 RID: 116
		private int _mixQueueCap;

		// Token: 0x04000075 RID: 117
		private int _mixQueueLock;

		// Token: 0x04000076 RID: 118
		private double _t;

		// Token: 0x04000077 RID: 119
		private float _prevL;

		// Token: 0x04000078 RID: 120
		private float _prevR;

		// Token: 0x04000079 RID: 121
		private volatile bool _running;

		// Token: 0x0400007A RID: 122
		private Thread _thread;

		// Token: 0x0400007B RID: 123
		private readonly object _statsLock = new object();

		// Token: 0x0400007C RID: 124
		private double _accIn;

		// Token: 0x0400007D RID: 125
		private double _accOut;

		// Token: 0x0400007E RID: 126
		private long _accFrames;

		// Token: 0x0400007F RID: 127
		private volatile float _inPeak;

		// Token: 0x04000080 RID: 128
		private volatile float _outPeak;

		// Token: 0x04000081 RID: 129
		private volatile float _in2Peak;

		// Token: 0x04000082 RID: 130
		private long _totalFrames;

		// Token: 0x04000083 RID: 131
		private bool _stopped;

		// Token: 0x04000084 RID: 132
		private bool _writeErrLogged;
	}
}
