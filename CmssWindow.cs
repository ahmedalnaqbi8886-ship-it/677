using System;
using System.Drawing;
using System.Windows.Forms;

namespace EaxReverbEngine
{
    // CMSS 3D - نافذة مستقلة براسها، بطلب صريح من المستخدم ("نفس ما سويت الـEQ، سوي الـCMSS") -
    // بنفس اسلوب EqWindow.cs بالضبط (نافذة قائمة تتفتح من ايقونتها بالشريط السفلي، مو تبويب
    // جوا EAX Settings). المحتوى نفسه اللي كان بتبويب CMSS 3D سابقًا (تفعيل + Stereo Widening).
    internal sealed class CmssWindow : PanelBase
    {
        private CheckBox _on;
        private CSlider _widen;

        public CmssWindow(FxChain fx) : base(fx, "cmss", "CMSS 3D", 500, 150)
        {
            Panel g = Group(Body, "", 6, 10, 488, 120);

            _on = new CheckBox();
            _on.Text = "Enable CMSS 3D Surround";
            _on.AutoSize = true;
            _on.BackColor = Color.Transparent;
            _on.Location = new Point(10, 14);
            g.Controls.Add(_on);

            Cap(g, "Stereo Widening", 12, 54, 112, ContentAlignment.MiddleLeft);
            Cap(g, "Low", 130, 54, 30, ContentAlignment.MiddleCenter);
            _widen = HBar(g, 164, 50, 254, 0, 100);
            Cap(g, "High", 424, 54, 34, ContentAlignment.MiddleCenter);

            _on.CheckedChanged += delegate { if (!Loading) Fx.CmssOn = _on.Checked; };
            _widen.ValueChanged += delegate { if (!Loading) Fx.CmssWidening = _widen.Value; };

            Reload();
        }

        protected override void DoDefault()
        {
            Fx.CmssOn = false;
            Fx.CmssWidening = 50;
        }

        public override void Reload()
        {
            bool was = Loading;
            Loading = true;
            try
            {
                _on.Checked = Fx.CmssOn;
                _widen.Value = Fx.CmssWidening;
            }
            finally { Loading = was; }
        }
    }
}
