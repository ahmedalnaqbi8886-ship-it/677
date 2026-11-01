using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EaxReverbEngine
{
    // زر بشكل سماعة (Bass / Stereo / Surround) - مرسوم بالكود، بدون صور او شعارات
    internal sealed class SpeakerButton : Control
    {
        public enum Kind { Bass, Stereo, Surround }

        private readonly Kind _kind;
        private bool _on;
        private bool _hover;
        public string Caption;

        public SpeakerButton(Kind kind, string caption)
        {
            _kind = kind;
            Caption = caption;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Size = new Size(72, 62);
            Cursor = BlueCursor.Arrow;
        }

        public bool On
        {
            get { return _on; }
            set { if (_on != value) { _on = value; Invalidate(); } }
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

        private static GraphicsPath Round(Rectangle r, int rad)
        {
            GraphicsPath p = new GraphicsPath();
            int d = rad * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        private static void Speaker(Graphics g, float cx, float cy, float s, Color c, int wavesRight, int wavesLeft)
        {
            using (Brush b = new SolidBrush(c))
            using (Pen pen = new Pen(c, 1.5f * s))
            {
                g.FillRectangle(b, cx - 9f * s, cy - 4f * s, 5f * s, 8f * s);
                PointF[] cone = new PointF[]
                {
                    new PointF(cx - 4f * s, cy - 4f * s), new PointF(cx + 2f * s, cy - 9f * s),
                    new PointF(cx + 2f * s, cy + 9f * s), new PointF(cx - 4f * s, cy + 4f * s)
                };
                g.FillPolygon(b, cone);
                for (int i = 1; i <= wavesRight; i++)
                {
                    float r = (3f + 3.2f * i) * s;
                    g.DrawArc(pen, cx + 2f * s - r, cy - r, 2f * r, 2f * r, -50f, 100f);
                }
                for (int i = 1; i <= wavesLeft; i++)
                {
                    float r = (3f + 3.2f * i) * s;
                    g.DrawArc(pen, cx - 9f * s - r, cy - r, 2f * r, 2f * r, 130f, 100f);
                }
            }
        }

        // بطاقة بحافة واضحة ثلاثية الابعاد (نفس اسلوب ازرار الدرايفر الاصلي SkinButton):
        // قبل كانت الحافة فاتحة جدا على خلفية بيضاء فصارت شبه مختفية - صلحتها بحدود واضحة + تدرج + لمعة علوية
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            Color edge = _on ? Color.FromArgb(40, 110, 225) : (_hover ? Color.FromArgb(90, 130, 190) : Color.FromArgb(140, 140, 146));
            Color top = _on ? Color.FromArgb(205, 225, 252) : (_hover ? Color.FromArgb(240, 243, 248) : Color.White);
            Color bot = _on ? Color.FromArgb(150, 185, 235) : (_hover ? Color.FromArgb(222, 226, 233) : Color.FromArgb(222, 222, 226));
            Color fg = Color.FromArgb(35, 38, 46);
            float edgeW = _on ? 2.2f : 1.4f;
            using (GraphicsPath p = Round(r, 9))
            {
                Rectangle sh = r; sh.Offset(0, 2);
                using (GraphicsPath spth = Round(sh, 9))
                using (Brush shb = new SolidBrush(Color.FromArgb(40, 0, 0, 0))) { g.FillPath(shb, spth); }
                using (LinearGradientBrush b = new LinearGradientBrush(r, top, bot, 90f))
                {
                    Blend bl = new Blend();
                    bl.Positions = new float[] { 0f, 0.5f, 0.5f, 1f };
                    bl.Factors = new float[] { 0f, 0.55f, 0.75f, 1f };
                    b.Blend = bl;
                    g.FillPath(b, p);
                }
                g.SetClip(p);
                using (Pen hi = new Pen(Color.FromArgb(200, 255, 255, 255), 1.3f)) { g.DrawLine(hi, r.X + 1, r.Y + 1, r.Right - 1, r.Y + 1); }
                using (Pen sl = new Pen(Color.FromArgb(40, 0, 0, 0), 1.3f)) { g.DrawLine(sl, r.X + 1, r.Bottom - 1, r.Right - 1, r.Bottom - 1); }
                g.ResetClip();
                using (Pen pen = new Pen(edge, edgeW)) { g.DrawPath(pen, p); }
            }
            float cx = Width / 2f, cy = Height * 0.4f;
            if (_kind == Kind.Bass) Speaker(g, cx - 3f, cy, 1.3f, fg, 3, 0);
            else if (_kind == Kind.Stereo)
            {
                Speaker(g, cx - 14f, cy, 0.85f, fg, 1, 0);
                Speaker(g, cx + 10f, cy, 0.85f, fg, 1, 0);
            }
            else Speaker(g, cx, cy, 1.0f, fg, 2, 2);

            using (StringFormat sf = new StringFormat())
            using (Brush tb = new SolidBrush(fg))
            using (Font f = new Font("Tahoma", 7.75f, FontStyle.Bold))
            {
                sf.Alignment = StringAlignment.Center;
                g.DrawString(Caption, f, tb, new RectangleF(0, Height - 18, Width, 16), sf);
            }
        }
    }
}
