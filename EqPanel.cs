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
        private static bool _closing;

        public static bool Closing { get { return _closing; } }

        public static void ShowFor(Engine engine)
        {
            Open(engine.Fx, "eq");
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
        }
    }
}
