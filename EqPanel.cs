using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace EaxReverbEngine
{
    // واجهة عامة تفتحها بقية الكود: ShowFor / Shutdown
    public static class EqPanel
    {
        private static FxChain _fx;
        private static readonly Dictionary<string, PanelBase> Wins = new Dictionary<string, PanelBase>();
        private static LauncherBar _bar;
        private static bool _closing;

        public static bool Closing { get { return _closing; } }

        public static void ShowFor(Engine engine)
        {
            PanelBase eq = Open(engine.Fx, "eq");
            try { ShowBar(engine.Fx, eq); }
            catch (Exception ex) { Log.Write("mediasource bar: " + ex); }
        }

        // يعرض شريط MediaSource لحاله فقط - بدون فتح نافذة Equalizer - يستخدم وقت تشغيل البرنامج
        // عشان الشريط يطلع كتطبيق منفصل مربوط بالمحرك، فوق نافذة الهوست تمامًا (مو عايم بمكان عشوائي)
        public static void ShowBarStandalone(Engine engine, Control host)
        {
            _fx = engine.Fx;
            _closing = false;
            try
            {
                if (_bar == null || _bar.IsDisposed) _bar = new LauncherBar(engine.Fx);
                Point topLeft = new Point(60, 60);
                if (host != null)
                {
                    try
                    {
                        Point hostScreen = host.PointToScreen(Point.Empty);
                        topLeft = new Point(hostScreen.X, hostScreen.Y - _bar.Height - 2);
                        if (topLeft.Y < 0) topLeft = new Point(hostScreen.X, hostScreen.Y);
                    }
                    catch (Exception) { topLeft = new Point(60, 60); }
                }
                _bar.StartPosition = FormStartPosition.Manual;
                _bar.Location = topLeft;
                _bar.Show();
                _bar.WindowState = FormWindowState.Normal;
                _bar.TopMost = true;
                _bar.BringToFront();
                _bar.Activate();
                _bar.TopMost = false;
                Log.Write("mediasource bar: standalone shown at " + topLeft.X + "," + topLeft.Y);
            }
            catch (Exception ex) { Log.Write("mediasource bar: " + ex); }
        }

        private static void ShowBar(FxChain fx, PanelBase near)
        {
            if (_bar == null || _bar.IsDisposed) _bar = new LauncherBar(fx);
            int bx = 60, by = 60;
            if (near != null)
            {
                bx = near.Location.X;
                by = near.Location.Y - _bar.Height - 2;
                if (by < 0) by = near.Location.Y + near.Height + 2;
                if (by < 0) by = 60;
            }
            _bar.StartPosition = FormStartPosition.Manual;
            _bar.Location = new Point(bx, by);
            _bar.Show();
            _bar.WindowState = FormWindowState.Normal;
            _bar.TopMost = true;
            _bar.BringToFront();
            _bar.Activate();
            _bar.TopMost = false;
            Log.Write("mediasource bar: shown at " + bx + "," + by + " visible=" + _bar.Visible);
        }

        internal static void Open(string kind)
        {
            if (_fx != null) Open(_fx, kind);
        }

        internal static void OpenFrom(string kind, Form current)
        {
            Point loc = current.Location;
            PanelBase w = Open(_fx, kind);
            if (w != null && !ReferenceEquals(w, current))
            {
                w.StartPosition = FormStartPosition.Manual;
                w.Location = loc;
                current.Hide();
            }
        }

        internal static PanelBase Open(FxChain fx, string kind)
        {
            _fx = fx;
            _closing = false;
            PanelBase w;
            if (!Wins.TryGetValue(kind, out w) || w.IsDisposed)
            {
                if (kind == "speaker") w = new SpeakerWindow(fx);
                else if (kind == "mixer") w = new MixerWindow(fx);
                else if (kind == "eax") w = new EaxWindow(fx);
                else w = new EqWindow(fx);
                Wins[kind] = w;
            }
            if (w.WindowState == FormWindowState.Minimized) w.WindowState = FormWindowState.Normal;
            w.Show();
            w.BringToFront();
            w.Activate();
            return w;
        }

        // يطابق اسم الجهاز الجديد بكل النوافذ المفتوحة حاليًا (غير اللي المستخدم كاتب فيها)
        internal static void BroadcastDeviceName(string name)
        {
            foreach (PanelBase w in Wins.Values)
            {
                try { if (!w.IsDisposed) w.SetDeviceNameText(name); } catch (Exception) { }
            }
        }

        public static void Shutdown()
        {
            _closing = true;
            foreach (PanelBase w in new List<PanelBase>(Wins.Values))
            {
                try { if (!w.IsDisposed) w.Close(); } catch (Exception) { }
            }
            Wins.Clear();
            try { if (_bar != null && !_bar.IsDisposed) _bar.Close(); } catch (Exception) { }
            _bar = null;
        }
    }
}
