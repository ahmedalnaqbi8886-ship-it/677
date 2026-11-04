using System;
using System.Drawing;
using System.Windows.Forms;

namespace EaxReverbEngine
{
    // EAX Settings: المؤثرات الصوتية + البيئة الصوتية (مطابقة للنافذة الاصلية)
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

        public EaxWindow(FxChain fx) : base(fx, "eax", "EAX Settings", 500, 260)
        {
            Label desc = new Label();
            desc.Text = "Audio Effects are high quality music enhancement effects created by the powerful audio engine in your sound device.";
            desc.SetBounds(10, 6, 480, 34);
            desc.BackColor = Color.Transparent;
            Body.Controls.Add(desc);

            _on = new CheckBox();
            _on.Text = "Enable Audio Effects";
            _on.AutoSize = true;
            _on.BackColor = Color.Transparent;
            _on.Location = new Point(12, 46);
            Body.Controls.Add(_on);

            Panel g = Group(Body, "", 6, 72, 488, 158);

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
            }
            finally { Loading = was; }
            Apply();
        }
    }
}
