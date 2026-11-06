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
        // الشريط نفسه يتحكم بارتفاعه (اختفاء/ظهور تلقائي فوق الشاشة) - هنا بس نحدد له موقعه الافقي
        public static void ShowBarStandalone(Engine engine, Control host)
        {
            _fx = engine.Fx;
            _closing = false;
            try
            {
                int bx = 60;
                if (host != null)
                {
                    try { bx = host.PointToScreen(Point.Empty).X; }
                    catch (Exception) { bx = 60; }
                }
                ShowBarAt(engine.Fx, bx);
            }
            catch (Exception ex) { Log.Write("mediasource bar: " + ex); }
        }

        private static void ShowBar(FxChain fx, PanelBase near)
        {
            int bx = 60;
            if (near != null) bx = near.Location.X;
            ShowBarAt(fx, bx);
        }

        private static void ShowBarAt(FxChain fx, int bx)
        {
            if (_bar == null || _bar.IsDisposed) _bar = new LauncherBar(fx);
            _bar.PlaceAt(bx);
            _bar.Show();
            _bar.WindowState = FormWindowState.Normal;
            _bar.TopMost = true;
            _bar.BringToFront();
            _bar.Activate();
            Log.Write("mediasource bar: shown at x=" + bx);
        }

        internal static void Open(string kind)
        {
            if (_fx != null) Open(_fx, kind);
        }

        internal static PanelBase OpenFrom(string kind, Form current)
        {
            Point loc = current.Location;
            PanelBase w = Open(_fx, kind);
            if (w != null && !ReferenceEquals(w, current))
            {
                w.StartPosition = FormStartPosition.Manual;
                w.Location = loc;
                current.Hide();
            }
            return w;
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
                else if (kind == "cmss") w = new CmssWindow(fx);
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
