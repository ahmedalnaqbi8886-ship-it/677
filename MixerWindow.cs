using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EaxReverbEngine
{
    // Surround Mixer: Basic / Advanced (مقاسات من اللقطة الاصلية)
    internal sealed class MixerWindow : PanelBase
    {
        private CSlider _vol, _fl, _fr, _bass, _trb, _cry, _sur, _sub, _fl2, _fr2, _sub2;
        private Panel _mute;
        private MeterBar _mL, _mR;
        private readonly Timer _tm = new Timer();
        private float _sl, _sr;

        public MixerWindow(FxChain fx) : base(fx, "mixer", "Surround Mixer", 500, 240)
        {
            Tabs tabs = new Tabs(new string[] { "Basic", "Advanced" });
            tabs.SetBounds(0, 0, 500, 29);
            Body.Controls.Add(tabs);
            Panel basic = new Panel(); basic.SetBounds(0, 29, 500, 211); basic.BackColor = Skin.Back;
            Panel adv = new Panel(); adv.SetBounds(0, 29, 500, 211); adv.BackColor = Skin.Back; adv.Visible = false;
            Body.Controls.Add(basic); Body.Controls.Add(adv);
            tabs.Changed += delegate { basic.Visible = tabs.Selected == 0; adv.Visible = tabs.Selected == 1; };

            // ---- Basic ----
            Panel g1 = Group(basic, "Master Control", 6, 6, 156, 204);
            _vol = Col(g1, 33, "Volume", true, 41, 143);
            _fl = Col(g1, 83, "", false, 41, 143);
            _fr = Col(g1, 124, "", false, 41, 143);
            _mute = new Panel();
            _mute.SetBounds(23, 184, 20, 20);
            _mute.Cursor = BlueCursor.Arrow;
            _mute.Paint += delegate(object s, PaintEventArgs e)
            {
                Bitmap b = Assets.Get(Fx.Mute ? "spk_off" : "spk_on");
                if (b != null) e.Graphics.DrawImage(b, 0, 0, b.Width, b.Height);
            };
            _mute.Click += delegate { Fx.Mute = !Fx.Mute; Reload(); };
            new ToolTip().SetToolTip(_mute, "Mute");
            g1.Controls.Add(_mute);
            Cap(g1, "L", 70, 186, 20, ContentAlignment.MiddleCenter);
            Cap(g1, "R", 114, 186, 20, ContentAlignment.MiddleCenter);

            Panel g2 = Group(basic, "Effects", 166, 6, 268, 204);
            _bass = Col(g2, 36, "Bass", true, 41, 143);
            _trb = Col(g2, 84, "Treble", false, 41, 143);
            _cry = Col(g2, 132, "Crystal", false, 41, 143);
            _sur = Col(g2, 180, "Surround", false, 41, 143);
            _sub = Col(g2, 228, "Subwoofer", false, 41, 143);

            Panel g3 = Group(basic, "", 438, 6, 56, 204);
            Label rec = Cap(g3, "● Output", 4, 8, 52, ContentAlignment.MiddleCenter);
            rec.ForeColor = Color.FromArgb(220, 0, 0);
            rec.Font = UiFont.Get(7f, FontStyle.Bold);
            _mL = new MeterBar(); _mL.Size = new Size(12, 140); _mL.Location = new Point(12, 36); g3.Controls.Add(_mL);
            _mR = new MeterBar(); _mR.Size = new Size(12, 140); _mR.Location = new Point(32, 36); g3.Controls.Add(_mR);

            // ---- Advanced ----
            Cap(adv, "Speaker Volume Controls:", 8, 2, 260, ContentAlignment.MiddleLeft);
            Panel g4 = Group(adv, "", 6, 20, 306, 190);
            _fl2 = Col(g4, 127, "FrontLeft", true, 14, 157);
            _fr2 = Col(g4, 189, "FrontRight", true, 14, 157);
            Panel g5 = Group(adv, "", 318, 20, 176, 190);
            _sub2 = Col(g5, 97, "Subwoofer", false, 14, 157);

            // ---- احداث ----
            _vol.ValueChanged += delegate { if (!Loading) { Fx.Master = 100 - _vol.Value; Reload(); } };
            _fl.ValueChanged += delegate { if (!Loading) { Fx.VolL = 100 - _fl.Value; Reload(); } };
            _fr.ValueChanged += delegate { if (!Loading) { Fx.VolR = 100 - _fr.Value; Reload(); } };
            _fl2.ValueChanged += delegate { if (!Loading) { Fx.VolL = 100 - _fl2.Value; Reload(); } };
            _fr2.ValueChanged += delegate { if (!Loading) { Fx.VolR = 100 - _fr2.Value; Reload(); } };
            _bass.ValueChanged += delegate { if (!Loading) { Fx.Bass = 100 - _bass.Value; if (Fx.Bass > 0.5f) Fx.BassOn = true; Reload(); } };
            _trb.ValueChanged += delegate { if (!Loading) { Fx.Treble = 100 - _trb.Value; Reload(); } };
            _cry.ValueChanged += delegate { if (!Loading) { Fx.Crystal = 100 - _cry.Value; Reload(); } };
            _sur.ValueChanged += delegate { if (!Loading) { Fx.Surround = 100 - _sur.Value; if (Fx.Surround > 0.5f) Fx.StereoOn = false; Reload(); } };
            _sub.ValueChanged += delegate { if (!Loading) { Fx.Sub = 100 - _sub.Value; Reload(); } };
            _sub2.ValueChanged += delegate { if (!Loading) { Fx.Sub = 100 - _sub2.Value; Reload(); } };

            _tm.Interval = 70;
            _tm.Tick += delegate
            {
                float l, r;
                Fx.ReadPeaks(out l, out r);
                _sl = Math.Max(_sl * 0.85f, Level(l));
                _sr = Math.Max(_sr * 0.85f, Level(r));
                _mL.Value = _sl; _mR.Value = _sr;
            };
            VisibleChanged += delegate { _tm.Enabled = Visible; };

            Reload();
        }

        // عمود: منزلق + اسم عمودي على يساره
        private CSlider Col(Control parent, int cx, string caption, bool dark, int y, int h)
        {
            CSlider s = VBar(parent, cx - 15, y, h);
            s.DarkGroove = dark;
            if (!string.IsNullOrEmpty(caption))
            {
                VLabel v = new VLabel();
                v.Text = caption;
                v.Font = UiFont.Get(8.25f);
                v.SetBounds(cx - 28, y + 10, 14, h - 20);
                parent.Controls.Add(v);
            }
            return s;
        }

        private static float Level(float peak)
        {
            if (peak <= 0.00001f) return 0f;
            float db = 20f * (float)Math.Log10(peak);
            float v = (db + 60f) / 60f;
            return v < 0f ? 0f : (v > 1f ? 1f : v);
        }

        protected override void DoDefault() { Fx.ResetMixer(); Fx.Bass = 0f; }

        public override void Reload()
        {
            bool was = Loading;
            Loading = true;
            try
            {
                _vol.Value = 100 - (int)Math.Round(Fx.Master);
                _fl.Value = 100 - (int)Math.Round(Fx.VolL); _fr.Value = 100 - (int)Math.Round(Fx.VolR);
                _fl2.Value = _fl.Value; _fr2.Value = _fr.Value;
                _bass.Value = 100 - (int)Math.Round(Fx.Bass);
                _trb.Value = 100 - (int)Math.Round(Fx.Treble);
                _cry.Value = 100 - (int)Math.Round(Fx.Crystal);
                _sur.Value = 100 - (int)Math.Round(Fx.Surround);
                _sub.Value = 100 - (int)Math.Round(Fx.Sub);
                _sub2.Value = _sub.Value;
                _mute.Invalidate();
            }
            finally { Loading = was; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _tm.Dispose();
            base.Dispose(disposing);
        }
    }
}
