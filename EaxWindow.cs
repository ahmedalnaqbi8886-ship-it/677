using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EaxReverbEngine
{
    // EAX Settings: تبويبين Effects (المؤثرات + البيئة الصوتية) و CMSS 3D (توسيع الاستيريو)
    internal sealed class EaxWindow : PanelBase
    {
        // الاسماء المؤكدة من لقطة الاصلي (Active Preset list) - مرتبة ابجديًا زي الاصلي
        private static readonly string[] Presets = new string[]
        {
            "Auditorium", "Bathroom", "Church", "Concert Hall", "Garage", "Haunted Cavern", "Indoor Arena"
        };

        private const int TabH = 30;

        private Panel _tabs, _effTab, _cmssTab;
        private int _tabIdx;

        private CheckBox _on;
        private ListBox _list;
        private Label _presetVal, _amtVal;
        private CSlider _amt;

        private CheckBox _cmssOn;
        private CSlider _widen;

        public EaxWindow(FxChain fx) : base(fx, "eax", "EAX Settings", 500, 290)
        {
            BuildTabs(500);
            BuildEffectsTab(500, 260);
            BuildCmssTab(500, 260);

            AddToggle("badge", "EAX Advanced HD", 214);

            Reload();
        }

        // ---------------------------------------------------------------- tabs
        private void BuildTabs(int w)
        {
            _tabs = new Panel();
            _tabs.SetBounds(0, 0, w, TabH);
            _tabs.Cursor = BlueCursor.Arrow;
            _tabs.Paint += delegate(object s, PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Skin.Back);
                using (Pen pe = new Pen(Skin.PanelEdge)) { g.DrawLine(pe, 0, TabH - 1, _tabs.Width, TabH - 1); }
                DrawTab(g, 0, "Effects", Color.FromArgb(0, 150, 136));
                DrawTab(g, 1, "CMSS 3D", Color.FromArgb(40, 120, 220));
            };
            _tabs.MouseDown += delegate(object s, MouseEventArgs e)
            {
                for (int i = 0; i < 2; i++)
                { if (TabRect(i).Contains(e.Location)) { SelectTab(i); break; } }
            };
            Body.Controls.Add(_tabs);
        }

        private Rectangle TabRect(int i)
        {
            const int tw = 168, gap = 4, x0 = 10;
            return new Rectangle(x0 + i * (tw + gap), 0, tw, TabH);
        }

        private void DrawTab(Graphics g, int i, string text, Color iconColor)
        {
            Rectangle r = TabRect(i);
            bool sel = i == _tabIdx;
            if (sel)
            {
                using (Brush wb = new SolidBrush(Skin.Back)) { g.FillRectangle(wb, r.X, r.Y, r.Width, r.Height); }
                using (Pen bp = new Pen(Skin.Blue, 2.4f)) { g.DrawLine(bp, r.X + 2, r.Bottom - 1, r.Right - 2, r.Bottom - 1); }
            }
            Rectangle ic = new Rectangle(r.X + 14, r.Y + 7, 16, 16);
            using (Brush ib = new SolidBrush(iconColor)) { g.FillEllipse(ib, ic); }
            using (Pen ip = new Pen(Color.White, 1.4f)) { g.DrawEllipse(ip, ic); }
            using (Pen gp = new Pen(Color.White, 1.6f))
            {
                if (i == 0) { g.DrawLine(gp, ic.X + 4, ic.Y + 8, ic.X + 12, ic.Y + 8); g.DrawLine(gp, ic.X + 8, ic.Y + 4, ic.X + 8, ic.Y + 12); }
                else { g.DrawEllipse(gp, ic.X + 5, ic.Y + 5, 6, 6); }
            }
            Color tc = sel ? Color.Black : Color.FromArgb(120, 120, 126);
            using (Font tf = new Font("Tahoma", 8.5f, sel ? FontStyle.Bold : FontStyle.Regular))
            {
                TextRenderer.DrawText(g, text, tf, new Rectangle(r.X + 36, r.Y, r.Width - 40, r.Height),
                    tc, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            }
        }

        private void SelectTab(int i)
        {
            if (_tabIdx == i) return;
            _tabIdx = i;
            _effTab.Visible = i == 0;
            _cmssTab.Visible = i == 1;
            _tabs.Invalidate();
        }

        // ---------------------------------------------------------------- Effects tab
        private void BuildEffectsTab(int w, int h)
        {
            _effTab = new Panel();
            _effTab.SetBounds(0, TabH, w, h);
            _effTab.BackColor = Skin.Back;
            Body.Controls.Add(_effTab);

            Label desc = new Label();
            desc.Text = "Audio Effects are high quality music enhancement effects created by the powerful audio engine in your sound device.";
            desc.SetBounds(10, 6, 480, 34);
            desc.BackColor = Color.Transparent;
            _effTab.Controls.Add(desc);

            _on = new CheckBox();
            _on.Text = "Enable Audio Effects";
            _on.AutoSize = true;
            _on.BackColor = Color.Transparent;
            _on.Location = new Point(12, 46);
            _effTab.Controls.Add(_on);

            Panel g = Group(_effTab, "", 6, 72, 488, 158);

            Cap(g, "Environment", 10, 10, 120, ContentAlignment.MiddleLeft);

            Cap(g, "Active Preset", 140, 10, 160, ContentAlignment.MiddleLeft);
            _presetVal = Cap(g, Presets[0], 140, 26, 170, ContentAlignment.MiddleLeft);

            _list = new ListBox();
            _list.SetBounds(140, 46, 170, 100);
            _list.IntegralHeight = false;
            _list.Items.AddRange(Presets);
            g.Controls.Add(_list);

            _amtVal = Cap(g, "Effects Amount : 0", 330, 10, 150, ContentAlignment.MiddleLeft);
            Cap(g, "0", 330, 36, 18, ContentAlignment.MiddleLeft);
            _amt = HBar(g, 350, 30, 108, 0, 100);
            Cap(g, "100", 460, 36, 26, ContentAlignment.MiddleLeft);

            // ---- احداث ----
            _on.CheckedChanged += delegate { if (!Loading) { Fx.EaxOn = _on.Checked; Apply(); } };
            _list.SelectedIndexChanged += delegate
            {
                if (_list.SelectedIndex >= 0)
                {
                    string p = Presets[_list.SelectedIndex];
                    _presetVal.Text = p;
                    if (!Loading) { Fx.EaxPreset = p; Apply(); }
                }
            };
            _amt.ValueChanged += delegate
            {
                _amtVal.Text = "Effects Amount : " + _amt.Value;
                if (!Loading) { Fx.EaxAmount = _amt.Value; Apply(); }
            };
        }

        // ---------------------------------------------------------------- CMSS 3D tab
        private void BuildCmssTab(int w, int h)
        {
            _cmssTab = new Panel();
            _cmssTab.SetBounds(0, TabH, w, h);
            _cmssTab.BackColor = Skin.Back;
            _cmssTab.Visible = false;
            Body.Controls.Add(_cmssTab);

            Label desc = new Label();
            desc.Text = "CMSS 3D provides several different modes for 3D enhanced playback of stereo audio over headphones or multichannel speakers.";
            desc.SetBounds(10, 6, 480, 40);
            desc.BackColor = Color.Transparent;
            _cmssTab.Controls.Add(desc);

            _cmssOn = new CheckBox();
            _cmssOn.Text = "Enable CMSS 3D Surround";
            _cmssOn.AutoSize = true;
            _cmssOn.BackColor = Color.Transparent;
            _cmssOn.Location = new Point(12, 50);
            _cmssTab.Controls.Add(_cmssOn);

            Panel g2 = Group(_cmssTab, "", 6, 78, 488, 152);

            Label info = new Label();
            info.Text = "Classic 3D upmix effect. Ideal for audio CDs, WMAs, MP3s and other stereo audio sources.";
            info.SetBounds(12, 16, 262, 70);
            info.BackColor = Color.Transparent;
            g2.Controls.Add(info);

            Panel box = Group(g2, "", 300, 8, 178, 136);

            Cap(box, "High", 0, 6, 178, ContentAlignment.MiddleCenter);
            Cap(box, "Low", 0, 114, 178, ContentAlignment.MiddleCenter);

            _widen = VBar(box, 86, 24, 88);
            _widen.Value = 50;

            Label widLbl = new Label();
            widLbl.Text = "Stereo\r\nWidening";
            widLbl.TextAlign = ContentAlignment.MiddleCenter;
            widLbl.Font = new Font("Tahoma", 7.5f);
            widLbl.SetBounds(2, 52, 70, 32);
            widLbl.BackColor = Color.Transparent;
            box.Controls.Add(widLbl);

            // ---- احداث ----
            _cmssOn.CheckedChanged += delegate { if (!Loading) Fx.CmssOn = _cmssOn.Checked; };
            _widen.ValueChanged += delegate { if (!Loading) Fx.CmssWidening = _widen.Value; };
        }

        private void Apply()
        {
            EngineBoot.SetState(Fx.EaxOn, Fx.EaxPreset, Fx.EaxAmount);
        }

        protected override void DoDefault()
        {
            Fx.EaxOn = false;
            Fx.EaxAmount = 0;
            Fx.EaxPreset = Presets[0];
            Fx.CmssOn = false;
            Fx.CmssWidening = 50;
        }

        public override void Reload()
        {
            bool was = Loading;
            Loading = true;
            try
            {
                _on.Checked = Fx.EaxOn;
                int idx = Array.IndexOf(Presets, Fx.EaxPreset);
                if (idx < 0) idx = 0;
                _list.SelectedIndex = idx;
                _presetVal.Text = Presets[idx];
                _amt.Value = Fx.EaxAmount;
                _amtVal.Text = "Effects Amount : " + Fx.EaxAmount;

                _cmssOn.Checked = Fx.CmssOn;
                _widen.Value = Fx.CmssWidening;
            }
            finally { Loading = was; }
            Apply();
        }
    }
}
