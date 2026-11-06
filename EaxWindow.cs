using System;
using System.Drawing;
using System.Windows.Forms;

namespace EaxReverbEngine
{
    // EAX Settings: تبويبين Effects | CMSS 3D جوا نفس النافذة (مؤكدة من صورة EAX CONSOLE
    // المرجعية) - رجعناها كذا بطلب صريح بعد ما جربنا نافذة CMSS مستقلة ولا زبطت للمستخدم.
    // CMSS صار تبويب بس، مو "kind" مستقل - ما يتفتح من EqPanel ولا له ايقونة بالشريط السفلي.
    internal sealed class EaxWindow : PanelBase
    {
        // الاسماء المؤكدة من لقطة الاصلي (Active Preset list) - مرتبة ابجديًا زي الاصلي
        private static readonly string[] Presets = new string[]
        {
            "Auditorium", "Bathroom", "Church", "Concert Hall", "Garage", "Haunted Cavern", "Indoor Arena"
        };

        private CheckBox _on;
        private ListBox _list;
        private Label _presetVal, _amtVal;
        private CSlider _amt;

        private CheckBox _cmssOn;
        private CSlider _widen;

        public EaxWindow(FxChain fx) : base(fx, "eax", "EAX Settings", 500, 270)
        {
            Tabs tabs = new Tabs(new string[] { "Effects", "CMSS 3D" }, new string[] { "fx", "cmss" });
            tabs.SetBounds(0, 0, 500, 29);
            Body.Controls.Add(tabs);

            Panel[] pg = new Panel[2];
            for (int i = 0; i < 2; i++)
            {
                pg[i] = new Panel();
                pg[i].SetBounds(0, 29, 500, 241);
                pg[i].BackColor = Skin.Back;
                pg[i].Visible = i == 0;
                Body.Controls.Add(pg[i]);
            }
            tabs.Changed += delegate { for (int i = 0; i < 2; i++) pg[i].Visible = tabs.Selected == i; };

            // ---- تبويب Effects ----
            Label desc = new Label();
            desc.Text = "Audio Effects are high quality music enhancement effects created by the powerful audio engine in your sound device.";
            desc.SetBounds(10, 6, 480, 34);
            desc.BackColor = Color.Transparent;
            pg[0].Controls.Add(desc);

            _on = new CheckBox();
            _on.Text = "Enable Audio Effects";
            _on.AutoSize = true;
            _on.BackColor = Color.Transparent;
            _on.Location = new Point(12, 46);
            pg[0].Controls.Add(_on);

            Panel g = Group(pg[0], "", 6, 72, 488, 158);

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

            // ---- تبويب CMSS 3D (وظيفة فعلية كاملة، نفس المحتوى اللي كان بالنافذة المستقلة) ----
            Panel g2 = Group(pg[1], "", 6, 10, 488, 120);

            _cmssOn = new CheckBox();
            _cmssOn.Text = "Enable CMSS 3D Surround";
            _cmssOn.AutoSize = true;
            _cmssOn.BackColor = Color.Transparent;
            _cmssOn.Location = new Point(10, 14);
            g2.Controls.Add(_cmssOn);

            Cap(g2, "Stereo Widening", 12, 54, 112, ContentAlignment.MiddleLeft);
            Cap(g2, "Low", 130, 54, 30, ContentAlignment.MiddleCenter);
            _widen = HBar(g2, 164, 50, 254, 0, 100);
            Cap(g2, "High", 424, 54, 34, ContentAlignment.MiddleCenter);

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
            _cmssOn.CheckedChanged += delegate { if (!Loading) Fx.CmssOn = _cmssOn.Checked; };
            _widen.ValueChanged += delegate { if (!Loading) Fx.CmssWidening = _widen.Value; };

            Reload();
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
