using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace EaxReverbEngine
{
	// Token: 0x02000012 RID: 18
	public static class EngineBoot
	{
		// Token: 0x0600007C RID: 124 RVA: 0x000062C8 File Offset: 0x000044C8
		public static void Start(object form)
		{
			lock (EngineBoot.Sync)
			{
				if (EngineBoot._started)
				{
					return;
				}
				EngineBoot._started = true;
			}
			try
			{
				string text = EngineConfig.DirForApp();
				Directory.CreateDirectory(text);
				Log.Init(text);
				Log.Write("=== EAX reverb engine boot, bitness={0} ===", new object[]
				{
					(IntPtr.Size == 8) ? "64" : "32"
				});
				if (!LicenseGate.Ensure())
				{
					Log.Write("license: not activated - engine not started");
					return;
				}
				EngineBoot._cfg = EngineConfig.Load(text);
				EngineBoot._engine = new Engine(text);
				EngineBoot._engine.StartAudio(EngineBoot._cfg);
				LicenseGate.Watch(delegate() { try { EngineBoot._engine.Stop(); } catch (Exception) { } });
				Log.Write(EngineBoot._engine.AudioStarted ? "engine audio running" : "engine idle (no audio started)");
				if (form != null)
				{
					EngineBoot.AttachForm(form);
					EngineBoot.ShowPanel(form);
				}
			}
			catch (Exception arg)
			{
				Log.Write("FATAL start: " + arg);
			}
		}

		// Token: 0x0600007D RID: 125 RVA: 0x000063E8 File Offset: 0x000045E8
		public static Engine StartStandalone(bool enabled, string presetName, int amount)
		{
			lock (EngineBoot.Sync)
			{
				if (EngineBoot._started)
				{
					return EngineBoot._engine;
				}
				EngineBoot._started = true;
			}
			try
			{
				string text = EngineConfig.DirForApp();
				Directory.CreateDirectory(text);
				Log.Init(text);
				Log.Write("=== EAX reverb engine standalone, bitness={0} ===", new object[]
				{
					(IntPtr.Size == 8) ? "64" : "32"
				});
				if (!LicenseGate.Ensure())
				{
					Log.Write("license: not activated - standalone engine not started");
					return null;
				}
				EngineBoot._cfg = EngineConfig.Load(text);
				EngineBoot._engine = new Engine(text);
				EngineBoot._engine.StartAudio(EngineBoot._cfg);
				EngineBoot._engine.SetState(enabled, presetName, amount);
				LicenseGate.Watch(delegate() { try { EngineBoot._engine.Stop(); } catch (Exception) { } });
			}
			catch (Exception arg)
			{
				Log.Write("FATAL start: " + arg);
			}
			return EngineBoot._engine;
		}

		// Token: 0x0600007E RID: 126 RVA: 0x000064F8 File Offset: 0x000046F8
		public static void SetState(bool enabled, string presetName, int amount)
		{
			Engine engine = EngineBoot._engine;
			if (engine != null)
			{
				engine.SetState(enabled, presetName, amount);
			}
		}

		// Token: 0x1700000D RID: 13
		// (get) Token: 0x0600007F RID: 127 RVA: 0x00006520 File Offset: 0x00004720
		public static Engine Current
		{
			get
			{
				return EngineBoot._engine;
			}
		}

		// Token: 0x06000080 RID: 128 RVA: 0x00006538 File Offset: 0x00004738
		private static object Member(object obj, string fieldName, string propName)
		{
			Type type = obj.GetType();
			FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (!(field != null))
			{
				if (propName != null)
				{
					PropertyInfo property = type.GetProperty(propName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					if (property != null)
					{
						return property.GetValue(obj, null);
					}
				}
				throw new MissingMemberException(type.FullName + "." + fieldName);
			}
			return field.GetValue(obj);
		}

		// Token: 0x06000081 RID: 129 RVA: 0x000065DC File Offset: 0x000047DC
		private static void AttachForm(object form)
		{
			Type type = form.GetType();
			EngineBoot._fCheck = type.GetField("_check", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			EngineBoot._fSlider = type.GetField("_slider", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			EngineBoot._fList = type.GetField("_list", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (EngineBoot._fCheck == null || EngineBoot._fSlider == null || EngineBoot._fList == null)
			{
				Log.Write("form fields not found, state polling disabled");
			}
			else
			{
				EventInfo @event = type.GetEvent("FormClosing", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (@event != null)
				{
					@event.AddEventHandler(form, new FormClosingEventHandler(delegate(object s, FormClosingEventArgs e)
					{
						EngineBoot.Stop();
					}));
				}
				EngineBoot._timer = new Timer();
				EngineBoot._timer.Interval = 200;
				EngineBoot._timer.Tick += delegate(object param0, EventArgs param1)
				{
					EngineBoot.Poll(form);
				};
				EngineBoot._timer.Start();
				EngineBoot.Poll(form);
				Log.Write("form state polling attached");
			}
		}

		// Token: 0x06000082 RID: 130 RVA: 0x00006718 File Offset: 0x00004918
		private static void Poll(object form)
		{
			if (!EngineBoot._pollBroken && EngineBoot._engine != null)
			{
				EngineBoot._pollCount++;
				if (EngineBoot._pollCount % 50 == 0 && EngineBoot._engine.AudioStarted)
				{
					double num;
					double num2;
					long num3;
					float num4;
					float num5;
					if (EngineBoot._engine.GetStats(out num, out num2, out num3, out num4, out num5))
					{
						float in2Peak = EngineBoot._engine.In2Peak;
						Log.Write("audio alive: total={0} inPeak={1:0.000} in2Peak={2:0.000} outPeak={3:0.000}", new object[]
						{
							num3,
							num4,
							in2Peak,
							num5
						});
					}
				}
				try
				{
					object value = EngineBoot._fCheck.GetValue(form);
					object value2 = EngineBoot._fSlider.GetValue(form);
					object value3 = EngineBoot._fList.GetValue(form);
					if (value != null && value2 != null && value3 != null)
					{
						bool enabled = (bool)EngineBoot.Member(value, "Checked", null);
						int amount = (int)EngineBoot.Member(value2, "_value", "Value");
						int num6 = (int)EngineBoot.Member(value3, "_selected", "SelectedIndex");
						string[] array = (string[])EngineBoot.Member(value3, "Items", null);
						if (array != null && num6 >= 0 && num6 < array.Length)
						{
							EngineBoot._engine.SetState(enabled, array[num6], amount);
						}
					}
				}
				catch (Exception ex)
				{
					EngineBoot._pollBroken = true;
					Log.Write("form polling disabled: " + ex.Message);
				}
			}
		}

		private static FieldInfo FindField(Type t, string name)
		{
			while (t != null)
			{
				FieldInfo f = t.GetField(name, EngineBoot.BF | BindingFlags.DeclaredOnly);
				if (f != null)
				{
					return f;
				}
				t = t.BaseType;
			}
			return null;
		}

		// يربط زر Equalizer في نافذة EAX بنافذة الـ EQ (بدل رسالة "not part of...")
		private static bool HookEqualizerButton(object form, Engine eng)
		{
			FieldInfo fw = form.GetType().GetField("_widgets", EngineBoot.BF);
			if (fw == null)
			{
				return false;
			}
			System.Collections.IEnumerable list = fw.GetValue(form) as System.Collections.IEnumerable;
			if (list == null)
			{
				return false;
			}
			bool hooked = false;
			foreach (object w in list)
			{
				if (w == null)
				{
					continue;
				}
				try
				{
					string tn = w.GetType().Name;
					if (tn == "IconButton" || tn == "SpriteButton")
					{
						object tip = EngineBoot.Member(w, "Tip", "Tip");
						if (tip != null && string.Equals(tip.ToString(), "Equalizer", StringComparison.OrdinalIgnoreCase))
						{
							FieldInfo fc = EngineBoot.FindField(w.GetType(), "Click");
							if (fc != null)
							{
								fc.SetValue(w, new EventHandler(delegate(object s2, EventArgs a2)
								{
									EqPanel.ShowFor(eng);
								}));
								hooked = true;
							}
						}
					}
					else if (tn == "ActionZoneWidget")
					{
						object bo = EngineBoot.Member(w, "Bounds", "Bounds");
						if (bo is System.Drawing.Rectangle)
						{
							System.Drawing.Rectangle r = (System.Drawing.Rectangle)bo;
							if (r.X == 119 && r.Y == 304 && r.Width == 25 && r.Height == 25)
							{
								FieldInfo fa = EngineBoot.FindField(w.GetType(), "_action");
								if (fa != null)
								{
									fa.SetValue(w, new Action(delegate()
									{
										EqPanel.ShowFor(eng);
									}));
									hooked = true;
								}
							}
						}
					}
				}
				catch (Exception ex)
				{
					Log.Write("eq hook: " + ex.Message);
				}
			}
			return hooked;
		}

		private static void ShowPanel(object form)
		{
			try
			{
				Engine eng = EngineBoot._engine;
				if (eng == null)
				{
					return;
				}
				Action act = delegate()
				{
					try { BlueCursor.Apply(form as Control); BlueCursor.Pin(form as Control); } catch (Exception) { }
					bool hooked = false;
					try
					{
						hooked = EqHook.TryHook(form, eng);
					}
					catch (Exception ex2)
					{
						Log.Write("hook: " + ex2.Message);
					}
					if (!hooked)
					{
						EqPanel.ShowFor(eng);
					}
				};
				Control c = form as Control;
				if (c != null && c.InvokeRequired)
				{
					c.BeginInvoke(act);
				}
				else
				{
					act();
				}
			}
			catch (Exception ex)
			{
				Log.Write("panel: " + ex.Message);
			}
		}

		// Token: 0x06000083 RID: 131 RVA: 0x000068E8 File Offset: 0x00004AE8
		public static void Stop()
		{
			lock (EngineBoot.Sync)
			{
				if (!EngineBoot._started || EngineBoot._stopped)
				{
					return;
				}
				EngineBoot._stopped = true;
			}
			try
			{
				if (EngineBoot._timer != null)
				{
					EngineBoot._timer.Stop();
					EngineBoot._timer.Dispose();
					EngineBoot._timer = null;
				}
			}
			catch (Exception ex)
			{
				Log.Write("timer stop: " + ex.Message);
			}
			try
			{
				if (EngineBoot._engine != null)
				{
					EngineBoot._engine.Stop();
					EngineBoot._engine = null;
				}
			}
			catch (Exception ex)
			{
				Log.Write("engine stop: " + ex.Message);
			}
			try { EqPanel.Shutdown(); } catch (Exception) { }
			Log.Write("engine stopped");
		}

		// Token: 0x04000097 RID: 151
		private const BindingFlags BF = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

		// Token: 0x04000098 RID: 152
		private static readonly object Sync = new object();

		// Token: 0x04000099 RID: 153
		private static Engine _engine;

		// Token: 0x0400009A RID: 154
		private static EngineConfig _cfg;

		// Token: 0x0400009B RID: 155
		private static Timer _timer;

		// Token: 0x0400009C RID: 156
		private static FieldInfo _fCheck;

		// Token: 0x0400009D RID: 157
		private static FieldInfo _fSlider;

		// Token: 0x0400009E RID: 158
		private static FieldInfo _fList;

		// Token: 0x0400009F RID: 159
		private static bool _pollBroken;

		// Token: 0x040000A0 RID: 160
		private static bool _started;

		// Token: 0x040000A1 RID: 161
		private static bool _stopped;

		// Token: 0x040000A2 RID: 162
		private static int _pollCount;
	}
}
