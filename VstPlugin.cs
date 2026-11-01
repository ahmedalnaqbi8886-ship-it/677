using System;
using System.Runtime.InteropServices;

namespace EaxReverbEngine
{
	// Token: 0x02000003 RID: 3
	internal sealed class VstPlugin : IDisposable
	{
		// Token: 0x06000005 RID: 5
		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern IntPtr LoadLibrary(string fileName);

		// Token: 0x06000006 RID: 6
		[DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
		private static extern IntPtr GetProcAddress(IntPtr module, string name);

		// Token: 0x06000007 RID: 7
		[DllImport("kernel32.dll")]
		private static extern bool FreeLibrary(IntPtr module);

		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000008 RID: 8 RVA: 0x00002174 File Offset: 0x00000374
		// (set) Token: 0x06000009 RID: 9 RVA: 0x0000218B File Offset: 0x0000038B
		public int ProgramCount { get; private set; }

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x0600000A RID: 10 RVA: 0x00002194 File Offset: 0x00000394
		// (set) Token: 0x0600000B RID: 11 RVA: 0x000021AB File Offset: 0x000003AB
		public int ParameterCount { get; private set; }

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x0600000C RID: 12 RVA: 0x000021B4 File Offset: 0x000003B4
		// (set) Token: 0x0600000D RID: 13 RVA: 0x000021CB File Offset: 0x000003CB
		public int InputCount { get; private set; }

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x0600000E RID: 14 RVA: 0x000021D4 File Offset: 0x000003D4
		// (set) Token: 0x0600000F RID: 15 RVA: 0x000021EB File Offset: 0x000003EB
		public int OutputCount { get; private set; }

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x06000010 RID: 16 RVA: 0x000021F4 File Offset: 0x000003F4
		// (set) Token: 0x06000011 RID: 17 RVA: 0x0000220B File Offset: 0x0000040B
		public int UniqueId { get; private set; }

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x06000012 RID: 18 RVA: 0x00002214 File Offset: 0x00000414
		// (set) Token: 0x06000013 RID: 19 RVA: 0x0000222B File Offset: 0x0000042B
		public int Version { get; private set; }

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x06000014 RID: 20 RVA: 0x00002234 File Offset: 0x00000434
		// (set) Token: 0x06000015 RID: 21 RVA: 0x0000224B File Offset: 0x0000044B
		public string EffectName { get; private set; }

		// Token: 0x17000008 RID: 8
		// (get) Token: 0x06000016 RID: 22 RVA: 0x00002254 File Offset: 0x00000454
		// (set) Token: 0x06000017 RID: 23 RVA: 0x0000226B File Offset: 0x0000046B
		public int MaxFrames { get; private set; }

		// Token: 0x06000018 RID: 24 RVA: 0x00002274 File Offset: 0x00000474
		private static IntPtr OnHost(IntPtr effect, int opcode, int index, IntPtr value, IntPtr ptr, float opt)
		{
			IntPtr result;
			if (opcode == 1)
			{
				result = new IntPtr(2400);
			}
			else
			{
				result = IntPtr.Zero;
			}
			return result;
		}

		// Token: 0x06000019 RID: 25 RVA: 0x000022A4 File Offset: 0x000004A4
		private int Off(string field)
		{
			switch (field)
			{
			case "dispatch":
				return (IntPtr.Size == 8) ? 8 : 4;
			case "setParam":
				return (IntPtr.Size == 8) ? 24 : 12;
			case "getParam":
				return (IntPtr.Size == 8) ? 32 : 16;
			case "numProg":
				return (IntPtr.Size == 8) ? 40 : 20;
			case "numParam":
				return (IntPtr.Size == 8) ? 44 : 24;
			case "numIn":
				return (IntPtr.Size == 8) ? 48 : 28;
			case "numOut":
				return (IntPtr.Size == 8) ? 52 : 32;
			case "unique":
				return (IntPtr.Size == 8) ? 112 : 72;
			case "version":
				return (IntPtr.Size == 8) ? 116 : 76;
			case "procReplace":
				return (IntPtr.Size == 8) ? 120 : 80;
			}
			return -1;
		}

		// Token: 0x0600001A RID: 26 RVA: 0x00002454 File Offset: 0x00000654
		public void Load(string path, int sampleRate, int maxFrames)
		{
			this.MaxFrames = maxFrames;
			this._lib = VstPlugin.LoadLibrary(path);
			if (this._lib == IntPtr.Zero)
			{
				throw new InvalidOperationException(string.Concat(new object[]
				{
					"LoadLibrary failed: ",
					path,
					" err=",
					Marshal.GetLastWin32Error()
				}));
			}
			IntPtr procAddress = VstPlugin.GetProcAddress(this._lib, "VSTPluginMain");
			if (procAddress == IntPtr.Zero)
			{
				procAddress = VstPlugin.GetProcAddress(this._lib, "main");
			}
			if (procAddress == IntPtr.Zero)
			{
				throw new InvalidOperationException("VSTPluginMain export not found in " + path);
			}
			VstPlugin._hostKeepAlive = VstPlugin.HostCallback;
			VstPlugin.VstEntry vstEntry = (VstPlugin.VstEntry)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(VstPlugin.VstEntry));
			this._eff = vstEntry(Marshal.GetFunctionPointerForDelegate<VstPlugin.HostProc>(VstPlugin.HostCallback));
			if (this._eff == IntPtr.Zero)
			{
				throw new InvalidOperationException("VSTPluginMain returned null");
			}
			int num = Marshal.ReadInt32(this._eff, 0);
			if (num != 1450406992)
			{
				throw new InvalidOperationException("bad AEffect magic 0x" + num.ToString("X8"));
			}
			IntPtr intPtr = Marshal.ReadIntPtr(this._eff, this.Off("dispatch"));
			IntPtr intPtr2 = Marshal.ReadIntPtr(this._eff, this.Off("setParam"));
			IntPtr intPtr3 = Marshal.ReadIntPtr(this._eff, this.Off("getParam"));
			IntPtr intPtr4 = Marshal.ReadIntPtr(this._eff, this.Off("procReplace"));
			if (intPtr == IntPtr.Zero || intPtr2 == IntPtr.Zero || intPtr3 == IntPtr.Zero || intPtr4 == IntPtr.Zero)
			{
				throw new InvalidOperationException("AEffect function pointers missing");
			}
			this._dispatch = (VstPlugin.DispatcherProc)Marshal.GetDelegateForFunctionPointer(intPtr, typeof(VstPlugin.DispatcherProc));
			this._setParam = (VstPlugin.SetParamProc)Marshal.GetDelegateForFunctionPointer(intPtr2, typeof(VstPlugin.SetParamProc));
			this._getParam = (VstPlugin.GetParamProc)Marshal.GetDelegateForFunctionPointer(intPtr3, typeof(VstPlugin.GetParamProc));
			this._processReplacing = (VstPlugin.ProcessProc)Marshal.GetDelegateForFunctionPointer(intPtr4, typeof(VstPlugin.ProcessProc));
			this.ProgramCount = Marshal.ReadInt32(this._eff, this.Off("numProg"));
			this.ParameterCount = Marshal.ReadInt32(this._eff, this.Off("numParam"));
			this.InputCount = Marshal.ReadInt32(this._eff, this.Off("numIn"));
			this.OutputCount = Marshal.ReadInt32(this._eff, this.Off("numOut"));
			this.UniqueId = Marshal.ReadInt32(this._eff, this.Off("unique"));
			this.Version = Marshal.ReadInt32(this._eff, this.Off("version"));
			Log.Write("plugin loaded: progs={0} params={1} in={2} out={3} uid=0x{4:X8} ver={5}", new object[]
			{
				this.ProgramCount,
				this.ParameterCount,
				this.InputCount,
				this.OutputCount,
				this.UniqueId,
				this.Version
			});
			this._inL = new float[maxFrames];
			this._inR = new float[maxFrames];
			this._outL = new float[maxFrames];
			this._outR = new float[maxFrames];
			this._pinInL = GCHandle.Alloc(this._inL, GCHandleType.Pinned);
			this._pinInR = GCHandle.Alloc(this._inR, GCHandleType.Pinned);
			this._pinOutL = GCHandle.Alloc(this._outL, GCHandleType.Pinned);
			this._pinOutR = GCHandle.Alloc(this._outR, GCHandleType.Pinned);
			this._inPtrs = Marshal.AllocHGlobal(IntPtr.Size * 2);
			this._outPtrs = Marshal.AllocHGlobal(IntPtr.Size * 2);
			Marshal.WriteIntPtr(this._inPtrs, 0, this._pinInL.AddrOfPinnedObject());
			Marshal.WriteIntPtr(this._inPtrs, IntPtr.Size, this._pinInR.AddrOfPinnedObject());
			Marshal.WriteIntPtr(this._outPtrs, 0, this._pinOutL.AddrOfPinnedObject());
			Marshal.WriteIntPtr(this._outPtrs, IntPtr.Size, this._pinOutR.AddrOfPinnedObject());
			this.EffectName = this.QueryText(45, 0);
			Log.Write("plugin name=\"{0}\"", new object[]
			{
				this.EffectName
			});
			this.Dispatch(0, 0, 0L, IntPtr.Zero, 0f);
			this.Dispatch(10, 0, 0L, IntPtr.Zero, (float)sampleRate);
			this.Dispatch(11, 0, (long)maxFrames, IntPtr.Zero, 0f);
			this.Dispatch(12, 0, 1L, IntPtr.Zero, 0f);
		}

		// Token: 0x0600001B RID: 27 RVA: 0x00002974 File Offset: 0x00000B74
		public string GetProgramName(int index)
		{
			return this.QueryText(29, index);
		}

		// Token: 0x0600001C RID: 28 RVA: 0x00002990 File Offset: 0x00000B90
		private string QueryText(int opcode, int index)
		{
			IntPtr intPtr = Marshal.AllocHGlobal(64);
			string result;
			try
			{
				for (int i = 0; i < 64; i++)
				{
					Marshal.WriteByte(intPtr, i, 0);
				}
				this.Dispatch(opcode, index, 0L, intPtr, 0f);
				result = (Marshal.PtrToStringAnsi(intPtr) ?? "");
			}
			catch (Exception ex)
			{
				result = "<err:" + ex.Message + ">";
			}
			finally
			{
				Marshal.FreeHGlobal(intPtr);
			}
			return result;
		}

		// Token: 0x0600001D RID: 29 RVA: 0x00002A28 File Offset: 0x00000C28
		public long Dispatch(int opcode, int index, long value, IntPtr ptr, float opt)
		{
			long result;
			if (this._disposed || this._dispatch == null)
			{
				result = 0L;
			}
			else
			{
				result = this._dispatch(this._eff, opcode, index, new IntPtr(value), ptr, opt).ToInt64();
			}
			return result;
		}

		// Token: 0x0600001E RID: 30 RVA: 0x00002A80 File Offset: 0x00000C80
		public void SetProgram(int index)
		{
			if (index >= 0 && index < this.ProgramCount)
			{
				this.Dispatch(2, 0, (long)index, IntPtr.Zero, 0f);
			}
		}

		// Token: 0x0600001F RID: 31 RVA: 0x00002ABC File Offset: 0x00000CBC
		public void SetParameter(int index, float share)
		{
			if (!this._disposed && this._setParam != null)
			{
				this._setParam(this._eff, index, share);
			}
		}

		// Token: 0x06000020 RID: 32 RVA: 0x00002AFC File Offset: 0x00000CFC
		public float GetParameter(int index)
		{
			float result;
			if (this._disposed || this._getParam == null)
			{
				result = 0f;
			}
			else
			{
				result = this._getParam(this._eff, index);
			}
			return result;
		}

		// Token: 0x06000021 RID: 33 RVA: 0x00002B44 File Offset: 0x00000D44
		public void Process(float[] inL, float[] inR, float[] outL, float[] outR, int frames)
		{
			if (this._disposed || this._processReplacing == null || frames <= 0)
			{
				if (frames > 0)
				{
					Array.Clear(outL, 0, frames);
					Array.Clear(outR, 0, frames);
				}
			}
			else
			{
				if (frames > this.MaxFrames)
				{
					frames = this.MaxFrames;
				}
				Buffer.BlockCopy(inL, 0, this._inL, 0, frames * 4);
				Buffer.BlockCopy(inR, 0, this._inR, 0, frames * 4);
				this._processReplacing(this._eff, this._inPtrs, this._outPtrs, frames);
				Buffer.BlockCopy(this._outL, 0, outL, 0, frames * 4);
				Buffer.BlockCopy(this._outR, 0, outR, 0, frames * 4);
			}
		}

		// Token: 0x06000022 RID: 34 RVA: 0x00002C20 File Offset: 0x00000E20
		public void Dispose()
		{
			if (!this._disposed)
			{
				this._disposed = true;
				try
				{
					if (this._dispatch != null)
					{
						this._dispatch(this._eff, 1, 0, IntPtr.Zero, IntPtr.Zero, 0f);
					}
				}
				catch (Exception ex)
				{
					Log.Write("plugin close error: " + ex.Message);
				}
				this._dispatch = null;
				this._setParam = null;
				this._getParam = null;
				this._processReplacing = null;
				this._eff = IntPtr.Zero;
				if (this._pinInL.IsAllocated)
				{
					this._pinInL.Free();
				}
				if (this._pinInR.IsAllocated)
				{
					this._pinInR.Free();
				}
				if (this._pinOutL.IsAllocated)
				{
					this._pinOutL.Free();
				}
				if (this._pinOutR.IsAllocated)
				{
					this._pinOutR.Free();
				}
				if (this._inPtrs != IntPtr.Zero)
				{
					Marshal.FreeHGlobal(this._inPtrs);
					this._inPtrs = IntPtr.Zero;
				}
				if (this._outPtrs != IntPtr.Zero)
				{
					Marshal.FreeHGlobal(this._outPtrs);
					this._outPtrs = IntPtr.Zero;
				}
				if (this._lib != IntPtr.Zero)
				{
					VstPlugin.FreeLibrary(this._lib);
					this._lib = IntPtr.Zero;
				}
				Log.Write("plugin unloaded");
			}
		}

		// Token: 0x04000003 RID: 3
		public const int EffOpen = 0;

		// Token: 0x04000004 RID: 4
		public const int EffClose = 1;

		// Token: 0x04000005 RID: 5
		public const int EffSetProgram = 2;

		// Token: 0x04000006 RID: 6
		public const int EffGetProgram = 3;

		// Token: 0x04000007 RID: 7
		public const int EffGetProgramName = 5;

		// Token: 0x04000008 RID: 8
		public const int EffGetParamName = 8;

		// Token: 0x04000009 RID: 9
		public const int EffGetParamDisplay = 7;

		// Token: 0x0400000A RID: 10
		public const int EffSetSampleRate = 10;

		// Token: 0x0400000B RID: 11
		public const int EffSetBlockSize = 11;

		// Token: 0x0400000C RID: 12
		public const int EffMainsChanged = 12;

		// Token: 0x0400000D RID: 13
		public const int EffGetEffectName = 45;

		// Token: 0x0400000E RID: 14
		public const int EffGetProgramNameIndexed = 29;

		// Token: 0x0400000F RID: 15
		private static VstPlugin.HostProc _hostKeepAlive;

		// Token: 0x04000010 RID: 16
		private static readonly VstPlugin.HostProc HostCallback = new VstPlugin.HostProc(VstPlugin.OnHost);

		// Token: 0x04000011 RID: 17
		private IntPtr _lib;

		// Token: 0x04000012 RID: 18
		private IntPtr _eff;

		// Token: 0x04000013 RID: 19
		private VstPlugin.DispatcherProc _dispatch;

		// Token: 0x04000014 RID: 20
		private VstPlugin.SetParamProc _setParam;

		// Token: 0x04000015 RID: 21
		private VstPlugin.GetParamProc _getParam;

		// Token: 0x04000016 RID: 22
		private VstPlugin.ProcessProc _processReplacing;

		// Token: 0x04000017 RID: 23
		private GCHandle _pinInL;

		// Token: 0x04000018 RID: 24
		private GCHandle _pinInR;

		// Token: 0x04000019 RID: 25
		private GCHandle _pinOutL;

		// Token: 0x0400001A RID: 26
		private GCHandle _pinOutR;

		// Token: 0x0400001B RID: 27
		private IntPtr _inPtrs;

		// Token: 0x0400001C RID: 28
		private IntPtr _outPtrs;

		// Token: 0x0400001D RID: 29
		private float[] _inL;

		// Token: 0x0400001E RID: 30
		private float[] _inR;

		// Token: 0x0400001F RID: 31
		private float[] _outL;

		// Token: 0x04000020 RID: 32
		private float[] _outR;

		// Token: 0x04000021 RID: 33
		private bool _disposed;

		// Token: 0x02000004 RID: 4
		// (Invoke) Token: 0x06000026 RID: 38
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate IntPtr DispatcherProc(IntPtr effect, int opcode, int index, IntPtr value, IntPtr ptr, float opt);

		// Token: 0x02000005 RID: 5
		// (Invoke) Token: 0x0600002A RID: 42
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void ProcessProc(IntPtr effect, IntPtr inputs, IntPtr outputs, int frames);

		// Token: 0x02000006 RID: 6
		// (Invoke) Token: 0x0600002E RID: 46
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void SetParamProc(IntPtr effect, int index, float value);

		// Token: 0x02000007 RID: 7
		// (Invoke) Token: 0x06000032 RID: 50
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate float GetParamProc(IntPtr effect, int index);

		// Token: 0x02000008 RID: 8
		// (Invoke) Token: 0x06000036 RID: 54
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate IntPtr HostProc(IntPtr effect, int opcode, int index, IntPtr value, IntPtr ptr, float opt);

		// Token: 0x02000009 RID: 9
		// (Invoke) Token: 0x0600003A RID: 58
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate IntPtr VstEntry(IntPtr host);
	}
}
