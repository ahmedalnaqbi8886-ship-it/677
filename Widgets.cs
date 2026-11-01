using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EaxReverbEngine
{
    // زر تنقل صغير (Equalizer / Speaker / Mixer) مرسوم بالكود
    internal sealed class NavButton : Control
    {
        private readonly string _kind;
        private readonly bool _cur;
        private bool _hover;

        public NavButton(string kind, bool current)
        {
            _kind = kind; _cur = current;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Size = new Size(30, 24);
            Cursor = BlueCursor.Arrow;
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Color bg = _cur ? Color.FromArgb(52, 120, 224) : (_hover ? Color.FromArgb(240, 242, 248) : Color.FromArgb(222, 224, 230));
            Color fg = _cur ? Color.White : Color.FromArgb(50, 55, 70);
            using (Brush b = new SolidBrush(bg)) { g.FillRectangle(b, 0, 0, Width, Height); }
            using (Pen p = new Pen(Color.FromArgb(70, 70, 76))) { g.DrawRectangle(p, 0, 0, Width - 1, Height - 1); }
            using (Brush f = new SolidBrush(fg))
            using (Pen pen = new Pen(fg, 1.5f))
            {
                if (_kind == "eq")
                {
                    g.FillRectangle(f, 7, 8, 3, 10); g.FillRectangle(f, 13, 5, 3, 13); g.FillRectangle(f, 19, 10, 3, 8);
                }
                else if (_kind == "speaker")
                {
                    g.FillRectangle(f, 7, 9, 4, 6);
                    g.FillPolygon(f, new PointF[] { new PointF(11, 9), new PointF(16, 5), new PointF(16, 19), new PointF(11, 15) });
                    g.DrawArc(pen, 12, 7, 8, 10, -50, 100);
                    g.DrawArc(pen, 11, 4, 13, 16, -50, 100);
                }
                else
                {
                    g.DrawLine(pen, 8, 5, 8, 19); g.DrawLine(pen, 15, 5, 15, 19); g.DrawLine(pen, 22, 5, 22, 19);
                    g.FillRectangle(f, 5, 8, 7, 3); g.FillRectangle(f, 12, 13, 7, 3); g.FillRectangle(f, 19, 7, 7, 3);
                }
            }
        }
    }

    // مؤشر مستوى عمودي
    internal sealed class MeterBar : Control
    {
        private float _v;
        public MeterBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Size = new Size(14, 170);
        }
        public float Value
        {
            get { return _v; }
            set { float n = value < 0f ? 0f : (value > 1f ? 1f : value); if (Math.Abs(n - _v) > 0.003f) { _v = n; Invalidate(); } }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (Brush b = new SolidBrush(Color.FromArgb(60, 60, 64))) { g.FillRectangle(b, 0, 0, Width, Height); }
            int h = (int)(Height * _v);
            if (h > 0)
            {
                Color c = _v > 0.92f ? Color.FromArgb(230, 60, 50) : (_v > 0.75f ? Color.FromArgb(240, 200, 40) : Color.FromArgb(60, 200, 90));
                using (Brush b = new SolidBrush(c)) { g.FillRectangle(b, 1, Height - h, Width - 2, h); }
            }
        }
    }

    // رسمة بسيطة لاختبار السماعات (سماعة راس او سماعتين)
    internal sealed class TestPic : Control
    {
        private bool _headphone = true;
        public TestPic()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Size = new Size(196, 120);
        }
        public bool Headphone { get { return _headphone; } set { _headphone = value; Invalidate(); } }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (LinearGradientBrush bg = new LinearGradientBrush(ClientRectangle, Color.FromArgb(245, 225, 190), Color.FromArgb(225, 190, 150), 90f))
            { g.FillRectangle(bg, ClientRectangle); }
            using (Brush d = new SolidBrush(Color.FromArgb(40, 40, 46)))
            using (Pen p = new Pen(Color.FromArgb(40, 40, 46), 6f))
            {
                if (_headphone)
                {
                    g.DrawArc(p, 56, 18, 84, 84, 180, 180);
                    g.FillRectangle(d, 50, 56, 18, 36);
                    g.FillRectangle(d, 128, 56, 18, 36);
                }
                else
                {
                    g.FillRectangle(d, 34, 30, 40, 70);
                    g.FillRectangle(d, 122, 30, 40, 70);
                    using (Brush c = new SolidBrush(Color.FromArgb(120, 120, 128)))
                    {
                        g.FillEllipse(c, 42, 56, 24, 24); g.FillEllipse(c, 130, 56, 24, 24);
                    }
                }
            }
        }
    }

    // نص عمودي (مثل ادوات Creative)
    internal sealed class VLabel : Control
    {
        public VLabel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Size = new Size(16, 80);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            g.TranslateTransform(0, Height);
            g.RotateTransform(-90f);
            using (Brush b = new SolidBrush(Color.Black))
            { g.DrawString(Text, Font, b, 0f, 0f); }
        }
    }
}
