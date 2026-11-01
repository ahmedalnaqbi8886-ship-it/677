using System;
using System.Drawing;
using System.Windows.Forms;

namespace EaxReverbEngine
{
    // Speaker Settings: Speaker Selection / Bass Management / Bass Boost
    internal sealed class SpeakerWindow : PanelBase
    {
        private static readonly string[] Modes = new string[] { "Headphone", "2 Speakers", "2.1 Speakers" };
        private ComboBox _cmb1;
        private RoomPic _pic;
        private SkinButton _bCh, _bNo;
        private CSlider _bmPct, _bmCross, _bbLevel, _bbCut;
        private Label _vBmCross, _vBbLevel, _vBbCut;
        private CheckBox _bmOn, _bbOn;
        private readonly Timer _tm = new Timer();

        public SpeakerWindow(FxChain fx) : base(fx, "speaker", "Speaker Settings", 500, 270)
        {
            Tabs tabs = new Tabs(new string[] { "Speaker Selection", "Bass Management", "Bass Boost" });
            tabs.SetBounds(0, 0, 500, 29);
            Body.Controls.Add(tabs);
            Panel[] pg = new Panel[3];
            for (int i = 0; i < 3; i++)
            {
                pg[i] = new Panel();
                pg[i].SetBounds(0, 29, 500, 241);
                pg[i].BackColor = Skin.Back;
                pg[i].Visible = i == 0;
                Body.Controls.Add(pg[i]);
            }
            tabs.Changed += delegate { for (int i = 0; i < 3; i++) pg[i].Visible = tabs.Selected == i; };

            // ---- Speaker Selection ----
            Panel a = Group(pg[0], "Speaker/Headphone Selection", 6, 4, 238, 236);
            _cmb1 = Combo(a, 12, 32);
            Panel b = Group(pg[0], "Speaker Test", 252, 4, 242, 236);
            _pic = new RoomPic();
            _pic.Size = new Size(190, 140); _pic.Location = new Point(26, 30);
            b.Controls.Add(_pic);
            _bCh = new SkinButton(); _bCh.Text = "Channel"; _bCh.SetBounds(24, 190, 92, 24); b.Controls.Add(_bCh);
            _bNo = new SkinButton(); _bNo.Text = "Noise"; _bNo.SetBounds(122, 190, 92, 24); b.Controls.Add(_bNo);
            _bCh.Click += delegate { Toggle(1); };
            _bNo.Click += delegate { Toggle(2); };

            // ---- Bass Management ----
            Panel c = Group(pg[1], "", 6, 4, 488, 100);
            Cap(c, "Subwoofer Volume", 12, 10, 300, ContentAlignment.MiddleLeft);
            Cap(c, "0%", 12, 66, 40, ContentAlignment.MiddleLeft);
            _bmPct = HBar(c, 50, 58, 390, 0, 100);
            Cap(c, "100%", 446, 66, 40, ContentAlignment.MiddleLeft);
            Panel d = Group(pg[1], "", 6, 108, 488, 132);
            _bmOn = new CheckBox(); _bmOn.Text = "Bass Redirection"; _bmOn.AutoSize = true; _bmOn.BackColor = Color.Transparent; _bmOn.Location = new Point(18, 14); d.Controls.Add(_bmOn);
            _vBmCross = Cap(d, "", 34, 46, 300, ContentAlignment.MiddleLeft);
            Cap(d, "10 Hz", 18, 84, 50, ContentAlignment.MiddleLeft);
            _bmCross = HBar(d, 66, 76, 370, 10, 200);
            Cap(d, "200 Hz", 442, 84, 46, ContentAlignment.MiddleLeft);

            // ---- Bass Boost ----
            Panel e2 = Group(pg[2], "", 6, 4, 488, 236);
            _bbOn = new CheckBox(); _bbOn.Text = "Enable Bass Boost"; _bbOn.AutoSize = true; _bbOn.BackColor = Color.Transparent; _bbOn.Location = new Point(18, 18); e2.Controls.Add(_bbOn);
            _vBbLevel = Cap(e2, "", 18, 66, 300, ContentAlignment.MiddleLeft);
            Cap(e2, "0%", 18, 98, 40, ContentAlignment.MiddleLeft);
            _bbLevel = HBar(e2, 56, 90, 380, 0, 100);
            Cap(e2, "100%", 442, 98, 44, ContentAlignment.MiddleLeft);
            _vBbCut = Cap(e2, "", 18, 150, 300, ContentAlignment.MiddleLeft);
            Cap(e2, "10 Hz", 18, 182, 50, ContentAlignment.MiddleLeft);
            _bbCut = HBar(e2, 66, 174, 370, 10, 200);
            Cap(e2, "200 Hz", 442, 182, 46, ContentAlignment.MiddleLeft);

            // ---- احداث ----
            _cmb1.SelectedIndexChanged += delegate { if (!Loading) { Fx.SpeakerMode = _cmb1.SelectedIndex; Reload(); } };
            _bmPct.ValueChanged += delegate { if (!Loading) { Fx.BmPercent = _bmPct.Value; Reload(); } };
            _bmOn.CheckedChanged += delegate { if (!Loading) { Fx.BmOn = _bmOn.Checked; Reload(); } };
            _bmCross.ValueChanged += delegate { if (!Loading) { Fx.BmCrossover = _bmCross.Value; Reload(); } };
            _bbOn.CheckedChanged += delegate { if (!Loading) { Fx.BassOn = _bbOn.Checked; Reload(); } };
            _bbLevel.ValueChanged += delegate { if (!Loading) { Fx.Bass = _bbLevel.Value; if (Fx.Bass > 0.5f) Fx.BassOn = true; Reload(); } };
            _bbCut.ValueChanged += delegate { if (!Loading) { Fx.BassCutoff = _bbCut.Value; Reload(); } };

            _tm.Interval = 500;
            _tm.Tick += delegate { _bCh.Checked = Fx.TestMode == 1; _bNo.Checked = Fx.TestMode == 2; _bCh.Invalidate(); _bNo.Invalidate(); };
            VisibleChanged += delegate { _tm.Enabled = Visible; };

            Reload();
        }

        private static ComboBox Combo(Control parent, int x, int y)
        {
            ComboBox c = new ComboBox();
            c.DropDownStyle = ComboBoxStyle.DropDownList;
            c.Items.AddRange(Modes);
            c.SetBounds(x, y, 208, 21);
            parent.Controls.Add(c);
            return c;
        }

        private void Toggle(int mode)
        {
            if (Fx.TestMode == mode) Fx.StopTest();
            else Fx.StartTest(mode);
            _bCh.Checked = Fx.TestMode == 1; _bNo.Checked = Fx.TestMode == 2;
            _bCh.Invalidate(); _bNo.Invalidate();
        }

        protected override void DoDefault() { Fx.ResetSpeaker(); Fx.StopTest(); }

        public override void Reload()
        {
            bool was = Loading;
            Loading = true;
            try
            {
                _cmb1.SelectedIndex = Fx.SpeakerMode;
                _pic.Headphone = Fx.SpeakerMode == 0;
                _bmPct.Value = (int)Math.Round(Fx.BmPercent);
                _bmOn.Checked = Fx.BmOn;
                int cr = (int)Math.Round(Fx.BmCrossover);
                _bmCross.Value = cr;
                _vBmCross.Text = "Crossover Frequency: " + cr + " Hz";
                _bbOn.Checked = Fx.BassOn;
                int lv = (int)Math.Round(Fx.Bass);
                _bbLevel.Value = lv; _vBbLevel.Text = "Level: " + lv + " %";
                int cu = (int)Math.Round(Fx.BassCutoff);
                _bbCut.Value = cu; _vBbCut.Text = "Cutoff Frequency: " + cu + " Hz";
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
