using System;
using System.Collections.Generic;
using System.IO;

namespace EaxReverbEngine
{
	// Token: 0x02000011 RID: 17
	public sealed class Engine : IDisposable
	{
		// Token: 0x0600006E RID: 110 RVA: 0x00005173 File Offset: 0x00003373
		public Engine(string dir)
		{
			this._dir = dir;
			try { this.Fx.Load(dir); } catch (Exception) { }
		}

		// Token: 0x1700000A RID: 10
		// (get) Token: 0x0600006F RID: 111 RVA: 0x000051A8 File Offset: 0x000033A8
		public bool AudioStarted
		{
			get
			{
				return this._audioStarted;
			}
		}

		// Token: 0x06000070 RID: 112 RVA: 0x000051C0 File Offset: 0x000033C0
		public void StartAudio(EngineConfig cfg)
		{
			string[] array = Wave.ListIn();
			string[] array2 = Wave.ListOut();
			Log.Write("capture devices:");
			for (int i = 0; i < array.Length; i++)
			{
				Log.Write("  [{0}] {1}", new object[]
				{
					i,
					array[i]
				});
			}
			Log.Write("render devices:");
			for (int i = 0; i < array2.Length; i++)
			{
				Log.Write("  [{0}] {1}", new object[]
				{
					i,
					array2[i]
				});
			}
			int num = -1;
			bool flag = false;
			if (!string.IsNullOrEmpty(cfg.CaptureDevice))
			{
				num = Wave.Find(array, cfg.CaptureDevice);
				if (num < 0)
				{
					Log.Write("capture device '{0}' not found, aborting audio", new object[]
					{
						cfg.CaptureDevice
					});
					return;
				}
				flag = true;
			}
			else
			{
				int num2 = Wave.Find(array, "Voicemeeter Out B1");
				int num3 = 0;
				int num4 = 0;
				if (num2 >= 0 && Wave.PickInFormatReal(num2, out num3, out num4))
				{
					num = num2;
					flag = true;
					Log.Write("capture auto: VoiceMeeter Out B1 (everything routed to B1), {0}Hz ch={1}", new object[]
					{
						num3,
						num4
					});
				}
			}
			int num5;
			int num6;
			if (!Wave.PickInFormatReal(num, out num5, out num6))
			{
				if (flag)
				{
					Log.Write("capture device [{0}] busy or unsupported, aborting audio", new object[]
					{
						num
					});
					return;
				}
				num = -2;
				for (int i = 0; i < array.Length; i++)
				{
					if (Wave.PickInFormatReal(i, out num5, out num6))
					{
						num = i;
						break;
					}
				}
				if (num < 0)
				{
					Log.Write("no usable capture device, aborting audio");
					return;
				}
				Log.Write("default capture busy, using [{0}] '{1}'", new object[]
				{
					num,
					array[num]
				});
			}
			if (num == -1)
			{
				Log.Write("capture: Windows default, {0}Hz ch={1}", new object[]
				{
					num5,
					num6
				});
			}
			else
			{
				Log.Write("capture: [{0}] '{1}', {2}Hz ch={3}", new object[]
				{
					num,
					array[num],
					num5,
					num6
				});
			}
			int num7 = -1;
			int num8 = 0;
			string text = cfg.CaptureDevice2;
			bool flag2 = text.Equals("none", StringComparison.OrdinalIgnoreCase) || text.Equals("off", StringComparison.OrdinalIgnoreCase) || text.Equals("-", StringComparison.Ordinal);
			bool flag3 = num >= 0 && array[num].StartsWith("Voicemeeter Out", StringComparison.OrdinalIgnoreCase);
			if (!flag2 && string.IsNullOrEmpty(text) && flag3)
			{
				flag2 = true;
				Log.Write("second capture disabled (primary already = VoiceMeeter bus)");
			}
			if (!flag2 && string.IsNullOrEmpty(text))
			{
				string[] array3 = new string[]
				{
					"Voicemeeter Out B1",
					"Voicemeeter Out B2",
					"Voicemeeter Out B3",
					"CABLE Output"
				};
				for (int i = 0; i < array3.Length; i++)
				{
					int num9 = Wave.Find(array, array3[i]);
					if (num9 >= 0 && num9 != num)
					{
						text = array3[i];
						break;
					}
				}
				if (!string.IsNullOrEmpty(text))
				{
					Log.Write("second capture auto-selected: " + text);
				}
			}
			if (!flag2 && !string.IsNullOrEmpty(text))
			{
				num7 = Wave.Find(array, text);
				if (num7 < 0)
				{
					Log.Write("second capture '{0}' not found, continuing with mic only", new object[]
					{
						text
					});
					num7 = -1;
				}
				else if (Wave.TestOpenIn(num7, num5, 1))
				{
					num8 = 1;
				}
				else if (Wave.TestOpenIn(num7, num5, 2))
				{
					num8 = 2;
				}
				else
				{
					Log.Write("second capture [{0}] '{1}' does not support {2}Hz, continuing with mic only", new object[]
					{
						num7,
						array[num7],
						num5
					});
					num7 = -1;
				}
				if (num7 >= 0)
				{
					Log.Write("second capture: [{0}] '{1}', {2}Hz ch={3}", new object[]
					{
						num7,
						array[num7],
						num5,
						num8
					});
				}
			}
			int num10;
			int num11;
			int num12;
			if (!string.IsNullOrEmpty(cfg.RenderDevice))
			{
				num10 = Wave.Find(array2, cfg.RenderDevice);
				if (num10 < 0)
				{
					Log.Write("render device '{0}' not found, aborting audio", new object[]
					{
						cfg.RenderDevice
					});
					return;
				}
				if (!Wave.PickOutFormatReal(num10, num5, 2, out num11, out num12))  // نفضل ستيريو دائماً لجهاز الاخراج، حتى لو المايك مونو
				{
					Log.Write("render device [{0}] '{1}' busy or unsupported, aborting audio", new object[]
					{
						num10,
						array2[num10]
					});
					return;
				}
				Log.Write("render: [{0}] '{1}', {2}Hz ch={3}", new object[]
				{
					num10,
					array2[num10],
					num11,
					num12
				});
			}
			else
			{
				string[] array4 = new string[]
				{
					"Voicemeeter AUX Input",
					"Voicemeeter VAIO3 Input",
					"CABLE Input",
					"Voicemeeter Input",
					"SteelSeries Sonar - Gaming",
					"SteelSeries Sonar - Chat"
				};
				List<int> list = new List<int>();
				num10 = -2;
				num11 = 0;
				num12 = 0;
				int num13 = 0;
				while (num13 < array4.Length && num10 < 0)
				{
					int num9 = Wave.Find(array2, array4[num13]);
					if (num9 >= 0 && !list.Contains(num9))
					{
						list.Add(num9);
						int num14;
						int num15;
						if (Wave.PickOutFormatReal(num9, num5, 2, out num14, out num15))  // نفضل ستيريو دائماً لجهاز الاخراج
						{
							num10 = num9;
							num11 = num14;
							num12 = num15;
							Log.Write("render: [{0}] '{1}' (matched '{2}'), {3}Hz ch={4}", new object[]
							{
								num9,
								array2[num9],
								array4[num13],
								num14,
								num15
							});
						}
						else
						{
							Log.Write("render candidate [{0}] '{1}' busy or unsupported, trying next", new object[]
							{
								num9,
								array2[num9]
							});
						}
					}
					num13++;
				}
				if (num10 < 0)
				{
					Log.Write("no usable virtual render device (all CABLE/VoiceMeeter/Sonar busy or missing).");
					Log.Write("Close apps holding the device (e.g. FL Studio), or set RenderDevice= in {0}\\engine.ini, then restart.", new object[]
					{
						this._dir
					});
					return;
				}
			}
			string text2 = Engine.LocatePlugin(cfg.PluginPath);
			if (text2 == null)
			{
				Log.Write("ReverbPresets VST dll not found (set PluginPath= in engine.ini), aborting audio");
			}
			else
			{
				int num16 = Math.Max(64, num5 * cfg.BufferMs / 1000);
				this._plugin = new VstPlugin();
				this._plugin.Load(text2, num5, num16 + 64);
				if (this._plugin.InputCount != 2 || this._plugin.OutputCount != 2)
				{
					Log.Write("unexpected plugin channels in={0} out={1}, aborting audio", new object[]
					{
						this._plugin.InputCount,
						this._plugin.OutputCount
					});
					this._plugin.Dispose();
					this._plugin = null;
				}
				else
				{
					this.LoadProgramNames();
					this.LoadMap();
					this._audio = new AudioEngine();
					this._audio.Fx = this.Fx;
					this._audio.Start(this._plugin, num, num5, num6, num7, num8, num10, num11, num12, cfg.BufferMs, cfg.BufferCount);
					this._audioStarted = true;
				}
			}
		}

		// Token: 0x06000071 RID: 113 RVA: 0x00005A1C File Offset: 0x00003C1C
		public static string LocatePlugin(string configured)
		{
			string text = (IntPtr.Size == 8) ? "ReverbPresets64.dll" : "ReverbPresets32.dll";
			List<string> list = new List<string>();
			if (!string.IsNullOrEmpty(configured))
			{
				list.Add(configured);
			}
			string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
			list.Add(Path.Combine(baseDirectory, text));
			list.Add(Path.Combine(Path.Combine(baseDirectory, "Release"), text));
			list.Add("C:\\Users\\yosef\\Downloads\\ReverbPresets-1.0.0\\ReverbPresets-1.0.0\\Release\\" + text);
			list.Add("C:\\Users\\yosef\\OneDrive\\Pictures\\ظ…ط¬ظ„ط¯ ط¬ط¯ظٹط¯\\ReverbPresets\\Release\\" + text);
			foreach (string text2 in list)
			{
				try
				{
					if (!string.IsNullOrEmpty(text2) && File.Exists(text2))
					{
						Log.Write("using plugin: " + text2);
						return text2;
					}
				}
				catch
				{
				}
			}
			return null;
		}

		// Token: 0x06000072 RID: 114 RVA: 0x00005B44 File Offset: 0x00003D44
		private void LoadProgramNames()
		{
			this._progNames = new string[this._plugin.ProgramCount];
			for (int i = 0; i < this._progNames.Length; i++)
			{
				string text = this._plugin.GetProgramName(i);
				if (text == null)
				{
					text = "";
				}
				text = text.Trim();
				this._progNames[i] = text;
				Log.Write("program[{0}] = \"{1}\"", new object[]
				{
					i,
					text
				});
			}
		}

		// Token: 0x06000073 RID: 115 RVA: 0x00005BD0 File Offset: 0x00003DD0
		private void LoadMap()
		{
			this._map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			string path = Path.Combine(this._dir, "preset_map.ini");
			try
			{
				if (!File.Exists(path))
				{
					List<string> list = new List<string>();
					list.Add("# maps Creative EAX Settings presets to Reverb Presets programs");
					list.Add("# value = program index, or exact program name");
					foreach (KeyValuePair<string, string> keyValuePair in Engine.DefaultMap)
					{
						list.Add(keyValuePair.Key + "=" + keyValuePair.Value);
					}
					File.WriteAllLines(path, list.ToArray());
					Log.Write("created default preset_map.ini");
				}
				foreach (string text in File.ReadAllLines(path))
				{
					string text2 = text.Trim();
					if (text2.Length != 0 && !text2.StartsWith("#"))
					{
						int num = text2.IndexOf('=');
						if (num > 0)
						{
							this._map[text2.Substring(0, num).Trim()] = text2.Substring(num + 1).Trim();
						}
					}
				}
				Log.Write("preset_map.ini entries: " + this._map.Count);
			}
			catch (Exception ex)
			{
				Log.Write("preset map load: " + ex.Message);
			}
		}

		// Token: 0x06000074 RID: 116 RVA: 0x00005DB0 File Offset: 0x00003FB0
		public void SetState(bool enabled, string presetName, int amount)
		{
			if (this._plugin != null)
			{
				if (amount < 0)
				{
					amount = 0;
				}
				else if (amount > 100)
				{
					amount = 100;
				}
				int num = this.ResolveProgram(presetName);
				bool flag = false;
				if (num >= 0 && num != this._lastProg)
				{
					this._plugin.SetProgram(num);
					this._lastProg = num;
					flag = true;
				}
				if (amount != this._lastAmount)
				{
					this._plugin.SetParameter(1, (float)amount / 100f);
					this._lastAmount = amount;
					flag = true;
				}
				int num2 = enabled ? 1 : 0;
				if (num2 != this._lastEnabled)
				{
					this._plugin.SetParameter(2, (float)num2);
					this._lastEnabled = num2;
					flag = true;
				}
				if (flag)
				{
					Log.Write("state: enabled={0} preset=\"{1}\" prog={2} amount={3}", new object[]
					{
						num2,
						presetName ?? "",
						this._lastProg,
						amount
					});
				}
			}
		}

		// Token: 0x06000075 RID: 117 RVA: 0x00005ED8 File Offset: 0x000040D8
		private int ResolveProgram(string presetName)
		{
			int result;
			if (string.IsNullOrEmpty(presetName))
			{
				result = ((this._lastProg >= 0) ? this._lastProg : 6);
			}
			else
			{
				for (int i = 0; i < this._progNames.Length; i++)
				{
					if (string.Equals(this._progNames[i], presetName, StringComparison.OrdinalIgnoreCase))
					{
						return i;
					}
				}
				string text;
				if (this._map != null && this._map.TryGetValue(presetName, out text))
				{
					int num;
					if (int.TryParse(text, out num) && num >= 0 && num < this._progNames.Length)
					{
						return num;
					}
					for (int i = 0; i < this._progNames.Length; i++)
					{
						if (string.Equals(this._progNames[i], text, StringComparison.OrdinalIgnoreCase))
						{
							return i;
						}
					}
				}
				if (this._lastProg >= 0)
				{
					result = this._lastProg;
				}
				else
				{
					Log.Write("no mapping for preset \"{0}\", using Concert Hall", new object[]
					{
						presetName
					});
					result = 6;
				}
			}
			return result;
		}

		// Token: 0x06000076 RID: 118 RVA: 0x00006004 File Offset: 0x00004204
		public bool GetStats(out double inRms, out double outRms, out long frames, out float inPeak, out float outPeak)
		{
			inRms = 0.0;
			outRms = 0.0;
			frames = 0L;
			inPeak = 0f;
			outPeak = 0f;
			bool result;
			if (this._audio == null)
			{
				result = false;
			}
			else
			{
				this._audio.GetRms(out inRms, out outRms, out frames);
				this._audio.GetPeaks(out inPeak, out outPeak);
				result = true;
			}
			return result;
		}

		// Token: 0x1700000B RID: 11
		// (get) Token: 0x06000077 RID: 119 RVA: 0x00006074 File Offset: 0x00004274
		public long TotalFrames
		{
			get
			{
				return (this._audio != null) ? this._audio.TotalFrames : 0L;
			}
		}

		// Token: 0x1700000C RID: 12
		// (get) Token: 0x06000078 RID: 120 RVA: 0x000060A0 File Offset: 0x000042A0
		public float In2Peak
		{
			get
			{
				return (this._audio != null) ? this._audio.TakeIn2Peak() : 0f;
			}
		}

		// Token: 0x06000079 RID: 121 RVA: 0x000060D0 File Offset: 0x000042D0
		public void Stop()
		{
			if (!this._disposed)
			{
				this._disposed = true;
				try { this.Fx.Save(this._dir); } catch (Exception) { }
				try
				{
					if (this._audio != null)
					{
						this._audio.Stop();
					}
				}
				catch (Exception ex)
				{
					Log.Write("audio stop: " + ex.Message);
				}
				try
				{
					if (this._plugin != null)
					{
						this._plugin.Dispose();
					}
				}
				catch (Exception ex)
				{
					Log.Write("plugin stop: " + ex.Message);
				}
				this._audio = null;
				this._plugin = null;
				this._audioStarted = false;
			}
		}

		// Token: 0x0600007A RID: 122 RVA: 0x0000619C File Offset: 0x0000439C
		public void Dispose()
		{
			this.Stop();
		}

		// Token: 0x0400008C RID: 140
		private static readonly Dictionary<string, string> DefaultMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{
				"Alley",
				"5"
			},
			{
				"Arena",
				"9"
			},
			{
				"Bedroom",
				"13"
			},
			{
				"Cabin",
				"13"
			},
			{
				"Canyon",
				"1"
			},
			{
				"Carpeted Hallway",
				"11"
			},
			{
				"Locker Room",
				"2"
			},
			{
				"Mountains",
				"8"
			},
			{
				"Padded Cell",
				"11"
			},
			{
				"Plain",
				"11"
			},
			{
				"Sewer Pipe",
				"5"
			},
			{
				"Stone Corridor",
				"4"
			},
			{
				"Stone Room",
				"7"
			},
			{
				"Tunnel",
				"5"
			},
			{
				"Underwater",
				"4"
			}
		};

		// Token: 0x0400008D RID: 141
		private readonly string _dir;
		public readonly FxChain Fx = new FxChain();

		// Token: 0x0400008E RID: 142
		private VstPlugin _plugin;

		// Token: 0x0400008F RID: 143
		private AudioEngine _audio;

		// Token: 0x04000090 RID: 144
		private string[] _progNames = new string[0];

		// Token: 0x04000091 RID: 145
		private Dictionary<string, string> _map;

		// Token: 0x04000092 RID: 146
		private int _lastProg = -1;

		// Token: 0x04000093 RID: 147
		private int _lastAmount = -1;

		// Token: 0x04000094 RID: 148
		private int _lastEnabled = -1;

		// Token: 0x04000095 RID: 149
		private bool _audioStarted;

		// Token: 0x04000096 RID: 150
		private bool _disposed;
	}
}
