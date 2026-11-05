using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace EaxReverbEngine
{
    internal static class Skin
    {
        public static readonly Color Back = Color.FromArgb(255, 255, 255);
        public static readonly Color Panel = Color.FromArgb(222, 219, 222);
        public static readonly Color PanelEdge = Color.FromArgb(206, 202, 206);
        public static readonly Color EqCol = Color.FromArgb(219, 223, 219);
        public static readonly Color ColEdge = Color.FromArgb(173, 170, 173);
        public static readonly Color Dark = Color.FromArgb(60, 60, 64);
        public static readonly Color Blue = Color.FromArgb(0, 95, 210);

        public static GraphicsPath Round(Rectangle r, int rad)
        {
            GraphicsPath p = new GraphicsPath();
            int d = rad * 2;
            if (d > r.Height) d = r.Height;
            if (d > r.Width) d = r.Width;
            if (d < 1) d = 1;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }

    internal enum SliderKind { Eq, Mixer, Horiz }

    // منزلق مرسوم بالكود بنفس شكل الاصلي
    internal sealed class CSlider : Control
    {
        private int _min, _max = 100, _val;
        private bool _vert, _drag, _hover;
        public SliderKind Kind = SliderKind.Mixer;
        public bool Column;          // رسم عمود EQ (خلفية + حدود + ارقام + تدريج)
        public bool DarkGroove;      // مجرى غامق (المنزلقات النشطة في Mixer)
        public int PadTop = 10, PadBottom = 10;
        public int TickFrequency = 10;
        public event EventHandler ValueChanged;

        public CSlider()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Size = new Size(120, 26);
            Cursor = BlueCursor.Arrow;
        }

        public Orientation Orientation
        {
            get { return _vert ? Orientation.Vertical : Orientation.Horizontal; }
            set { _vert = value == Orientation.Vertical; Invalidate(); }
        }
        public int Minimum { get { return _min; } set { _min = value; if (_val < _min) _val = _min; Invalidate(); } }
        public int Maximum { get { return _max; } set { _max = value; if (_val > _max) _val = _max; Invalidate(); } }
        public int Value
        {
            get { return _val; }
            set
            {
                int v = value < _min ? _min : (value > _max ? _max : value);
                if (v == _val) return;
                _val = v;
                Invalidate();
                if (ValueChanged != null) ValueChanged(this, EventArgs.Empty);
            }
        }

        private float Frac() { return _max == _min ? 0f : (float)(_val - _min) / (float)(_max - _min); }

        private int Lo { get { return _vert ? PadTop : PadTop; } }
        private int Span { get { return _vert ? Math.Max(1, Height - PadTop - PadBottom) : Math.Max(1, Width - PadTop - PadBottom); } }

        private void FromMouse(Point p)
        {
            float f = ((_vert ? p.Y : p.X) - PadTop) / (float)Span;
            if (f < 0f) f = 0f;
            if (f > 1f) f = 1f;
            Value = _min + (int)Math.Round(f * (_max - _min));
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) { _drag = true; Capture = true; FromMouse(e.Location); }
            base.OnMouseDown(e);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_drag) FromMouse(e.Location);
            base.OnMouseMove(e);
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            _drag = false; Capture = false;
            base.OnMouseUp(e);
        }
        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            int step = Math.Max(1, (_max - _min) / 20);
            Value = _val + (e.Delta > 0 ? -step : step) * (_vert ? 1 : -1);
            base.OnMouseWheel(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float f = Frac();
            if (_vert)
            {
                int cx = Width / 2;
                int top = PadTop;
                int bot = Height - PadBottom;
                int ty = top + (int)Math.Round((bot - top) * f);
                if (Kind == SliderKind.Eq)
                {
                    if (Column)
                    {
                        using (Brush b = new SolidBrush(Skin.EqCol)) { g.FillRectangle(b, 0, 0, Width - 1, Height - 1); }
                        // غائر خفيف حول العمود
                        using (Pen inTop = new Pen(Color.FromArgb(50, 0, 0, 0))) { g.DrawLine(inTop, 1, 1, Width - 2, 1); }
                        using (Pen inLft = new Pen(Color.FromArgb(50, 0, 0, 0))) { g.DrawLine(inLft, 1, 1, 1, Height - 2); }
                        using (Pen inBot = new Pen(Color.FromArgb(160, 255, 255, 255))) { g.DrawLine(inBot, 1, Height - 2, Width - 2, Height - 2); }
                        using (Pen p = new Pen(Skin.ColEdge)) { g.DrawRectangle(p, 0, 0, Width - 1, Height - 1); }
                        DrawTicks(g, top, bot, cx);
                    }
                    Rectangle gv = new Rectangle(cx - 2, top - 12, 4, bot - top + 24);
                    using (LinearGradientBrush b = new LinearGradientBrush(gv, Color.FromArgb(91, 90, 91), Color.FromArgb(150, 150, 150), 0f))
                    { g.FillRectangle(b, gv); }
                    Bitmap tb = Assets.Get("thumb_v");
                    if (tb != null) { g.SmoothingMode = SmoothingMode.None; g.DrawImage(tb, cx - tb.Width / 2, ty - tb.Height / 2, tb.Width, tb.Height); }
                    else DrawThumb(g, new Rectangle(cx - 8, ty - 12, 16, 24), true);
                    if (Column && !string.IsNullOrEmpty(Text))
                    {
                        using (Font ft = new Font("Tahoma", 9f, FontStyle.Bold))
                        { TextRenderer.DrawText(g, Text, ft, new Rectangle(0, Height - 24, Width, 20), Color.FromArgb(60, 60, 60), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }
                    }
                }
                else
                {
                    // سلايدر الـ Mixer العمودي - طابقته على لقطة الاصلي الحقيقية بالبكسل:
                    // قضيب رفيع بأطراف مدورة (غامق يسار لفاتح يمين) + مقبض ابيض مستطيل
                    // زواياه مدورة اعرض... اطول من عرضه يعلو فوق القضيب، بدون اي نقطة زرقاء
                    // ولا علامة حمراء (هذاك شكل قديم غلط) - صغّرته اكثر لأنه كان لسا اكبر من الاصلي
                    Rectangle gv = new Rectangle(cx - 2, top - 2, 5, bot - top + 4);
                    using (GraphicsPath gp = Skin.Round(gv, 2))
                    {
                        if (DarkGroove)
                        {
                            using (LinearGradientBrush b = new LinearGradientBrush(gv, Color.FromArgb(41, 40, 41), Color.FromArgb(163, 162, 163), 0f)) { g.FillPath(b, gp); }
                        }
                        else
                        {
                            using (LinearGradientBrush b = new LinearGradientBrush(gv, Color.FromArgb(62, 62, 64), Color.FromArgb(196, 196, 199), 0f))
                            {
                                Blend bl = new Blend();
                                bl.Positions = new float[] { 0f, 0.18f, 1f };
                                bl.Factors = new float[] { 0f, 0.38f, 1f };
                                b.Blend = bl;
                                g.FillPath(b, gp);
                            }
                        }
                        using (Pen p = new Pen(Color.FromArgb(44, 44, 44), 1f)) { g.DrawPath(p, gp); }
                    }
                    DrawHandleThumbV(g, new Rectangle(cx - 5, ty - 8, 10, 16));
                }
            }
            else
            {
                // سلايدر افقي Bass Boost / Bass Management - طابقته على لقطة الاصلي الحقيقية بالبكسل:
                // قضيب رمادي غامق رفيع باطراف مدورة (تدرج غامق فوق لفاتح تحت) + مقبض ابيض
                // مستطيل زواياه مدورة اعرض من طوله يعلو فوق القضيب (مب كبسولة ومب خط رفيع)
                // صغّرته اكثر عن القياس السابق لأنه كان لسا اكبر من حجمه بالاصلي
                int cy = Height / 2;
                int left = PadTop;
                int right = Width - PadBottom;
                int tx = left + (int)Math.Round((right - left) * f);

                int gh = 5;
                Rectangle gv = new Rectangle(left, cy - gh / 2, Math.Max(gh, right - left), gh);
                using (GraphicsPath gp = Skin.Round(gv, gh / 2))
                {
                    using (LinearGradientBrush b = new LinearGradientBrush(gv, Color.FromArgb(62, 62, 64), Color.FromArgb(196, 196, 199), 90f))
                    {
                        Blend bl = new Blend();
                        bl.Positions = new float[] { 0f, 0.18f, 1f };
                        bl.Factors = new float[] { 0f, 0.38f, 1f };
                        b.Blend = bl;
                        g.FillPath(b, gp);
                    }
                    using (Pen edge = new Pen(Color.FromArgb(44, 44, 44), 1f)) { g.DrawPath(edge, gp); }
                }

                Rectangle th = new Rectangle(tx - 8, cy - 5, 16, 10);
                DrawHandleThumb(g, th);
            }
        }

        private void DrawTicks(Graphics g, int top, int bot, int cx)
        {
            g.SmoothingMode = SmoothingMode.None;
            Color gray = Color.FromArgb(155, 159, 155);
            using (Pen tp = new Pen(gray))
            using (Pen rp = new Pen(Color.FromArgb(237, 0, 0)))
            {
                for (int i = 0; i <= 24; i++)
                {
                    int y = top + (int)Math.Round((bot - top) * i / 24.0);
                    int len = (i % 6 == 0) ? 11 : ((i % 3 == 0) ? 8 : 5);
                    Pen p = (i == 0 || i == 24) ? rp : tp;
                    if (i == 12) continue;
                    g.DrawLine(p, 3, y, 3 + len, y);
                    g.DrawLine(p, Width - 4 - len, y, Width - 4, y);
                }
                int zy = top + (bot - top) / 2;
                using (Brush gb = new SolidBrush(gray)) { g.FillRectangle(gb, 3, zy - 1, 11, 3); }
                using (Brush bb = new SolidBrush(Color.Black))
                { g.FillPolygon(bb, new Point[] { new Point(Width - 3, zy - 4), new Point(Width - 3, zy + 4), new Point(Width - 10, zy) }); }
            }
            g.SmoothingMode = SmoothingMode.AntiAlias;
        }

        // مقبض ثلاثي الابعاد: ظل تحته + لمعة علوية + انعكاس زجاجي (نفس شكل Creative)
        private static void DrawThumb(Graphics g, Rectangle th, bool vert)
        {
            Rectangle sh = th; sh.Offset(0, 2);
            using (GraphicsPath sgp = Skin.Round(sh, 3))
            using (Brush sb = new SolidBrush(Color.FromArgb(55, 0, 0, 0))) { g.FillPath(sb, sgp); }

            using (GraphicsPath gp = Skin.Round(th, 3))
            {
                using (LinearGradientBrush b = new LinearGradientBrush(th, Color.FromArgb(255, 255, 255), Color.FromArgb(188, 188, 194), 90f))
                {
                    b.Blend = Gloss();
                    g.FillPath(b, gp);
                }
                using (Pen edge = new Pen(Color.FromArgb(110, 110, 116))) { g.DrawPath(edge, gp); }
            }
            // خط لمعان رفيع اعلى المقبض
            Rectangle hi = new Rectangle(th.X + 2, th.Y + 1, th.Width - 4, Math.Max(2, th.Height / 3));
            using (GraphicsPath hgp = Skin.Round(hi, 2))
            using (Brush hb = new SolidBrush(Color.FromArgb(150, 255, 255, 255))) { g.FillPath(hb, hgp); }
        }

        // مقبض Bass Boost / Bass Management: مستطيل ابيض زواياه مدورة (مب كبسولة كاملة) بتدرج
        // خفيف فاتح فوق لأغمق تحت وحدود رمادية غامقة - مطابق للقطة الاصلية بالبكسل
        private static void DrawHandleThumb(Graphics g, Rectangle th)
        {
            using (GraphicsPath gp = Skin.Round(th, 2))
            {
                using (LinearGradientBrush b = new LinearGradientBrush(th, Color.White, Color.FromArgb(185, 183, 186), 90f))
                {
                    Blend bl = new Blend();
                    bl.Positions = new float[] { 0f, 0.5f, 1f };
                    bl.Factors = new float[] { 0f, 0.3f, 1f };
                    b.Blend = bl;
                    g.FillPath(b, gp);
                }
                using (Pen edge = new Pen(Color.FromArgb(100, 100, 100), 1.1f)) { g.DrawPath(edge, gp); }
            }
        }

        // نفس المقبض بس نسخة عمودية لمنزلقات الـ Mixer (Volume/FrontLeft/FrontRight/Subwoofer...):
        // تدرج افقي (فاتح بالنص لغامق باليمين واليسار) بدل العمودي
        private static void DrawHandleThumbV(Graphics g, Rectangle th)
        {
            using (GraphicsPath gp = Skin.Round(th, 2))
            {
                using (LinearGradientBrush b = new LinearGradientBrush(th, Color.FromArgb(226, 226, 228), Color.White, 0f))
                {
                    Blend bl = new Blend();
                    bl.Positions = new float[] { 0f, 0.5f, 1f };
                    bl.Factors = new float[] { 0f, 1f, 0f };
                    b.Blend = bl;
                    g.FillPath(b, gp);
                }
                using (Pen edge = new Pen(Color.FromArgb(100, 100, 100), 1.1f)) { g.DrawPath(edge, gp); }
            }
        }

        private static Blend _gloss;
        private static Blend Gloss()
        {
            if (_gloss == null)
            {
                _gloss = new Blend();
                _gloss.Positions = new float[] { 0f, 0.42f, 0.5f, 1f };
                _gloss.Factors = new float[] { 0f, 0.55f, 0.8f, 1f };
            }
            return _gloss;
        }
    }

    // زر مدور (Default / Channel / Noise)
    internal sealed class SkinButton : Control
    {
        private bool _hover, _down;
        public bool Checked;
        public SkinButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            Size = new Size(86, 24);
            Cursor = BlueCursor.Arrow;
            Font = new Font("Tahoma", 9f);
            ForeColor = Color.Black;
        }
        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; _down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { _down = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { _down = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            Color c1 = Checked ? Color.FromArgb(205, 225, 252) : Color.FromArgb(255, 255, 255);
            Color c2 = Checked ? Color.FromArgb(150, 185, 235) : (_hover ? Color.FromArgb(232, 236, 244) : Color.FromArgb(222, 222, 226));
            if (_down) { Color t = c1; c1 = c2; c2 = t; }
            using (GraphicsPath gp = Skin.Round(r, 6))
            {
                using (LinearGradientBrush b = new LinearGradientBrush(r, c1, c2, 90f))
                {
                    Blend bl = new Blend();
                    bl.Positions = new float[] { 0f, 0.5f, 0.5f, 1f };
                    bl.Factors = new float[] { 0f, 0.55f, 0.75f, 1f };
                    b.Blend = bl;
                    g.FillPath(b, gp);
                }
                // زر ثلاثي الابعاد بارز: لمعة علوية رفيعة + ظل سفلي خفيف
                g.SetClip(gp);
                using (Pen hi = new Pen(Color.FromArgb(_down ? 40 : 200, 255, 255, 255), 1.3f))
                { g.DrawLine(hi, r.X + 1, r.Y + 1, r.Right - 1, r.Y + 1); }
                using (Pen sh = new Pen(Color.FromArgb(_down ? 0 : 45, 0, 0, 0), 1.3f))
                { g.DrawLine(sh, r.X + 1, r.Bottom - 1, r.Right - 1, r.Bottom - 1); }
                g.ResetClip();
                using (Pen p = new Pen(Color.FromArgb(70, 70, 76), 1.4f)) { g.DrawPath(p, gp); }
            }
            if (_down) g.TranslateTransform(0f, 1f);
            TextRenderer.DrawText(g, Text, Font, new Rectangle(0, 0, Width, Height), ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    // زر "؟" مدمج مع سهم القائمة بقطعة واحدة (نفس شكل الاصلي: مقسوم بخط رفيع بالنص)
    internal sealed class HelpArrowButton : Control
    {
        public event EventHandler HelpClick;
        private bool _hoverL, _hoverR, _downL, _downR;

        public HelpArrowButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            Size = new Size(58, 22);
            Cursor = BlueCursor.Arrow;
            Font = new Font("Tahoma", 10f, FontStyle.Bold);
        }

        private bool IsLeftSide(Point p) { return p.X < Width / 2; }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            bool l = IsLeftSide(e.Location);
            if (_hoverL != l || _hoverR == l) { _hoverL = l; _hoverR = !l; Invalidate(); }
            base.OnMouseMove(e);
        }
        protected override void OnMouseLeave(EventArgs e) { _hoverL = _hoverR = _downL = _downR = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (IsLeftSide(e.Location)) _downL = true; else _downR = true;
            Invalidate();
            base.OnMouseDown(e);
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            bool wasL = _downL;
            _downL = _downR = false;
            Invalidate();
            if (wasL && IsLeftSide(e.Location) && HelpClick != null) HelpClick(this, EventArgs.Empty);
            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            int mid = Width / 2;
            using (GraphicsPath gp = Skin.Round(r, 6))
            {
                using (LinearGradientBrush b = new LinearGradientBrush(r, Color.White, Color.FromArgb(220, 220, 224), 90f)) { g.FillPath(b, gp); }
                if (_hoverL || _downL)
                {
                    g.SetClip(gp);
                    using (Brush hb = new SolidBrush(Color.FromArgb(_downL ? 70 : 35, 40, 120, 225))) { g.FillRectangle(hb, 0, 0, mid, Height); }
                    g.ResetClip();
                }
                if (_hoverR || _downR)
                {
                    g.SetClip(gp);
                    using (Brush hb = new SolidBrush(Color.FromArgb(_downR ? 70 : 35, 40, 120, 225))) { g.FillRectangle(hb, mid, 0, Width - mid, Height); }
                    g.ResetClip();
                }
                using (Pen p = new Pen(Color.FromArgb(70, 70, 76), 1.4f)) { g.DrawPath(p, gp); }
            }
            using (Pen sep = new Pen(Color.FromArgb(170, 170, 176))) { g.DrawLine(sep, mid, 3, mid, Height - 4); }
            TextRenderer.DrawText(g, "?", Font, new Rectangle(0, _downL ? 1 : 0, mid, Height), Skin.Blue, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            using (Brush ab = new SolidBrush(Skin.Blue))
            { g.FillPolygon(ab, new Point[] { new Point(mid + 9, 9 + (_downR ? 1 : 0)), new Point(mid + 18, 9 + (_downR ? 1 : 0)), new Point(mid + 13, 15 + (_downR ? 1 : 0)) }); }
        }
    }

    // ايقونات الشريط السفلي (صور الاصلية + رسم لـ Stereo)
    internal sealed class BarIcon : Control
    {
        private readonly string _kind;
        private bool _on, _hover;
        public BarIcon(string kind)
        {
            _kind = kind;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Size = new Size(27, 27);
            Cursor = BlueCursor.Arrow;
        }
        public bool On { get { return _on; } set { if (_on != value) { _on = value; Invalidate(); } } }
        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

        private string AssetName()
        {
            switch (_kind)
            {
                case "eq": return "ic_grid";
                case "speaker": return "ic_woof";
                case "mixer": return "ic_caps";
                case "badge": return "ic_fx";
                case "eax": return "ic_eax";
                default: return null;
            }
        }

        private static void Cone(Graphics g, Brush b, float x, float cy, float s)
        {
            g.FillRectangle(b, x, cy - 3f * s, 3f * s, 6f * s);
            g.FillPolygon(b, new PointF[] {
                new PointF(x + 3f * s, cy - 3f * s), new PointF(x + 8f * s, cy - 7f * s),
                new PointF(x + 8f * s, cy + 7f * s), new PointF(x + 3f * s, cy + 3f * s) });
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Bitmap img = null;
            string an = AssetName();
            if (an != null) img = Assets.Get(an);
            if (img != null)
            {
                g.SmoothingMode = SmoothingMode.None;
                g.DrawImage(img, 1, 1, img.Width, img.Height);
                g.SmoothingMode = SmoothingMode.AntiAlias;
            }
            else
            {
                Rectangle r = new Rectangle(1, 1, 24, 24);
                using (GraphicsPath gp = Skin.Round(r, 3))
                {
                    using (LinearGradientBrush b = new LinearGradientBrush(r, Color.FromArgb(250, 250, 250), Color.FromArgb(200, 200, 204), 90f)) { g.FillPath(b, gp); }
                    using (Pen p = new Pen(Color.FromArgb(90, 90, 94))) { g.DrawPath(p, gp); }
                }
                Color c = Color.FromArgb(40, 42, 50);
                using (Brush b = new SolidBrush(c))
                {
                    Cone(g, b, 3, 13, 0.9f);
                    Cone(g, b, 14, 13, 0.9f);
                }
            }
            if (_on)
            {
                using (Brush b = new SolidBrush(Color.FromArgb(70, 40, 120, 255))) { g.FillRectangle(b, 1, 1, 25, 25); }
                using (Pen p = new Pen(Color.FromArgb(0, 95, 210), 2f)) { g.DrawRectangle(p, 1, 1, 24, 24); }
            }
            else if (_hover)
            {
                using (Pen p = new Pen(Color.FromArgb(0, 95, 210), 1f)) { g.DrawRectangle(p, 1, 1, 24, 24); }
            }
        }
    }

    // زر كبير بعلامة سماعة لتفعيل Bass Boost / Stereo / Surround (بدون اي شعار)
    internal sealed class FxToggle : Control
    {
        public enum Kind { Bass, Stereo, Surround }
        private readonly Kind _kind;
        private bool _on, _hover;
        public string Caption;

        public FxToggle(Kind kind, string caption)
        {
            _kind = kind;
            Caption = caption;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Size = new Size(64, 50);
            Cursor = BlueCursor.Arrow;
        }

        public bool On { get { return _on; } set { if (_on != value) { _on = value; Invalidate(); } } }
        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

        private static void Woofer(Graphics g, Brush face, Pen edge, float cx, float cy, float r)
        {
            g.FillEllipse(face, cx - r, cy - r, r * 2f, r * 2f);
            g.DrawEllipse(edge, cx - r, cy - r, r * 2f, r * 2f);
            g.FillEllipse(face, cx - r * 0.55f, cy - r * 0.55f, r * 1.1f, r * 1.1f);
            g.DrawEllipse(edge, cx - r * 0.55f, cy - r * 0.55f, r * 1.1f, r * 1.1f);
            using (Brush dot = new SolidBrush(Color.FromArgb(40, 42, 50)))
            { g.FillEllipse(dot, cx - r * 0.18f, cy - r * 0.18f, r * 0.36f, r * 0.36f); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            bool lit = _on;
            Color ring = lit ? Color.FromArgb(0, 95, 210) : (_hover ? Color.FromArgb(150, 150, 155) : Color.FromArgb(190, 190, 195));
            Color fill = lit ? Color.FromArgb(220, 232, 250) : Color.White;
            Rectangle box = new Rectangle(1, 1, Width - 3, 36);
            using (GraphicsPath gp = Skin.Round(box, 7))
            {
                using (Brush b = new SolidBrush(fill)) { g.FillPath(b, gp); }
                using (Pen p = new Pen(ring, lit ? 2f : 1.2f)) { g.DrawPath(p, gp); }
            }
            using (Brush face = new SolidBrush(Color.FromArgb(70, 70, 76)))
            using (Pen edge = new Pen(Color.FromArgb(40, 40, 46), 1.3f))
            using (Pen wave = new Pen(lit ? Color.FromArgb(0, 95, 210) : Color.FromArgb(110, 110, 115), 2f))
            {
                float cy = 19f;
                if (_kind == Kind.Bass)
                {
                    Woofer(g, face, edge, Width / 2f, cy, 12f);
                }
                else if (_kind == Kind.Stereo)
                {
                    Woofer(g, face, edge, Width / 2f - 12f, cy, 8f);
                    Woofer(g, face, edge, Width / 2f + 12f, cy, 8f);
                }
                else
                {
                    Woofer(g, face, edge, Width / 2f, cy, 9f);
                    g.DrawArc(wave, Width / 2f - 20f, cy - 20f, 40f, 40f, 200, 140);
                    g.DrawArc(wave, Width / 2f - 26f, cy - 26f, 52f, 52f, 200, 140);
                }
            }
            using (Font f = new Font("Tahoma", 7.5f))
            {
                TextRenderer.DrawText(g, Caption, f, new Rectangle(0, 37, Width, 13), Color.FromArgb(40, 40, 46),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.Top);
            }
        }
    }

    // مربع اختبار السماعة: بطاقة مسطحة بسيطة + ايقونة سماعة/سماعتين (بدون غرفة ثلاثية الابعاد)
    internal sealed class RoomPic : Control
    {
        private bool _head = true;
        public RoomPic()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Size = new Size(220, 140);
            Cursor = BlueCursor.Arrow;
        }
        public bool Headphone { get { return _head; } set { if (_head != value) { _head = value; Invalidate(); } } }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath gp = Skin.Round(r, 8))
            {
                using (LinearGradientBrush b = new LinearGradientBrush(r, Color.White, Color.FromArgb(226, 226, 230), 90f))
                { g.FillPath(b, gp); }
                using (Pen p = new Pen(Color.FromArgb(180, 180, 186))) { g.DrawPath(p, gp); }
            }

            Color fg = Color.FromArgb(35, 38, 46);
            if (_head) DrawHeadphone(g, Width / 2f, Height / 2f + 4f, fg);
            else
            {
                DrawSpeaker(g, Width / 2f - 46f, Height / 2f + 4f, fg);
                DrawSpeaker(g, Width / 2f + 46f, Height / 2f + 4f, fg);
            }
        }

        private static void DrawHeadphone(Graphics g, float cx, float cy, Color fg)
        {
            using (Pen band = new Pen(fg, 4.5f)) { g.DrawArc(band, cx - 34f, cy - 44f, 68f, 60f, 180, 180); }
            Ear(g, cx - 34f, cy + 10f, fg);
            Ear(g, cx + 34f, cy + 10f, fg);
        }

        private static void Ear(Graphics g, float cx, float cy, Color fg)
        {
            Rectangle r = new Rectangle((int)cx - 9, (int)cy - 14, 18, 28);
            using (GraphicsPath gp = Skin.Round(r, 8))
            using (Brush b = new SolidBrush(fg)) { g.FillPath(b, gp); }
            using (Brush hi = new SolidBrush(Color.FromArgb(70, 255, 255, 255)))
            { g.FillEllipse(hi, cx - 5f, cy - 9f, 5f, 8f); }
        }

        private static void DrawSpeaker(Graphics g, float cx, float cy, Color fg)
        {
            Rectangle r = new Rectangle((int)cx - 16, (int)cy - 32, 32, 64);
            using (GraphicsPath gp = Skin.Round(r, 5))
            using (Brush b = new SolidBrush(fg)) { g.FillPath(b, gp); }
            using (Pen p = new Pen(Color.White, 1.3f))
            {
                g.DrawEllipse(p, cx - 10f, cy - 10f, 20f, 20f);
                g.DrawEllipse(p, cx - 5f, cy - 24f, 10f, 10f);
            }
        }
    }

    // مؤشر ماوس ازرق: يحمّل ملف الـ .cur الحقيقي عن طريق وندوز نفسه (اضمن من تركيب ايقونة يدوي)
    internal static class BlueCursor
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadCursorFromFile(string path);

        private static Cursor _arrow;
        private static string _curPath;

        public static Cursor Arrow
        {
            get
            {
                if (_arrow == null) _arrow = Make();
                return _arrow;
            }
        }

        private static Cursor Make()
        {
            try
            {
                System.Threading.Thread t = System.Threading.Thread.CurrentThread;
                Log.Write("cursor: Make() running on thread id=" + t.ManagedThreadId + " apartment=" + t.GetApartmentState());
            }
            catch (Exception) { }
            byte[] raw = null;
            try { raw = Convert.FromBase64String(CursorData.HandBlue); }
            catch (Exception ex) { Log.Write("cursor: base64 decode failed: " + ex.Message); }
            if (raw == null) { Log.Write("cursor: no raw bytes - using system Hand cursor"); return Cursors.Hand; }
            Log.Write("cursor: raw bytes OK, len=" + raw.Length);

            // السبب الحقيقي للموشر الاسود: System.Windows.Forms.Cursor(Stream) فيها خلل معروف
            // بتحميل مؤشرات الوان 32-bit ARGB - ما ترمي استثناء، بس ترسمها سوداء/مكسورة.
            // لازم نحمّل بأول محاولة عن طريق LoadCursorFromFile (API وندوز الحقيقي، يرسم الالوان صح
            // دايما)، ونخلي تحميل الذاكرة احتياط اخير بس.
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CreativeEAXSettings");
                Directory.CreateDirectory(dir);
                _curPath = Path.Combine(dir, "pointer.cur");
                bool write = true;
                if (File.Exists(_curPath))
                {
                    try { write = new FileInfo(_curPath).Length != raw.Length; } catch (Exception) { }
                }
                if (write) File.WriteAllBytes(_curPath, raw);
                IntPtr h = LoadCursorFromFile(_curPath);
                if (h != IntPtr.Zero)
                {
                    Log.Write("cursor: loaded OK from file " + _curPath);
                    return new Cursor(h);
                }
                Log.Write("cursor: LoadCursorFromFile returned NULL, path=" + _curPath + " win32err=" + Marshal.GetLastWin32Error());
            }
            catch (Exception ex) { Log.Write("cursor: file-based load failed: " + ex); }

            // احتياط اخير فقط لو ملف وندوز فشل كليا (نادر) - نقبل خطر اللون الاسود بدل ما نرجع للسهم العادي
            try
            {
                Cursor c = new Cursor(new MemoryStream(raw));
                Log.Write("cursor: loaded OK from memory stream (fallback)");
                return c;
            }
            catch (Exception ex) { Log.Write("cursor: memory-stream load failed: " + ex); }

            Log.Write("cursor: ALL load attempts failed - falling back to system Hand cursor");
            return Cursors.Hand;   // لا نستخدم اي رسم يدوي بديل، تفاديا لمؤشر اسود
        }

        // يطبق المؤشر على نافذة وكل ما فيها (مرة وحدة عند الانشاء)
        public static void Apply(Control root)
        {
            if (root == null) return;
            try
            {
                Cursor c = Arrow;
                root.Cursor = c;
                foreach (Control ch in root.Controls) Apply(ch);
            }
            catch (Exception) { }
        }

        public static void Pin(Control form)
        {
            if (form == null) return;
            try
            {
                Cursor c = Arrow;
                form.MouseMove += delegate { Cursor.Current = c; };
            }
            catch (Exception) { }
        }
    }
}
