using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace EaxReverbEngine
{
    // اساس النوافذ: ترويسة بيضاء + جسم + شريط سفلي معدني (مثل الاصلي)
    internal abstract class PanelBase : Form
    {
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, IntPtr l);

        protected const int HeadH = 40;
        protected const int BarH = 30;

        protected readonly FxChain Fx;
        protected readonly Panel Body;
        protected bool Loading;
        private readonly string _kind;
        private readonly string _title;
        private Panel _head, _bar;

        protected PanelBase(FxChain fx, string kind, string title, int w, int h)
        {
            Fx = fx;
            _kind = kind;
            _title = title;
            Text = title;
            Font = new Font("Tahoma", 8.25f);
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Skin.Back;
            ClientSize = new Size(w, HeadH + h + BarH);

            Body = new Panel();
            Body.SetBounds(0, HeadH, w, h);
            Body.BackColor = Skin.Back;
            Controls.Add(Body);

            BuildHead(w);
            BuildBar(w, HeadH + h);
            Cursor = BlueCursor.Arrow;
            BlueCursor.Apply(this);
            BlueCursor.Pin(this);
        }

        protected abstract void DoDefault();
        public abstract void Reload();

        private void BuildHead(int w)
        {
            _head = new Panel();
            _head.SetBounds(0, 0, w, HeadH);
            _head.Paint += delegate(object s, PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.Clear(Color.White);
                int[] sh = new int[] { 133, 182, 201, 214 };
                for (int i = 0; i < 4; i++)
                {
                    using (Pen p = new Pen(Color.FromArgb(sh[i], sh[i], sh[i]))) { g.DrawLine(p, 0, 36 + i, _head.Width, 36 + i); }
                }
                string an = _kind == "speaker" ? "hdr_spk" : (_kind == "mixer" ? "hdr_mix" : "hdr_eq");
                Bitmap ic = Assets.Get(an);
                if (ic != null) g.DrawImage(ic, 13, 4, ic.Width, ic.Height);
                using (Font f = new Font("Tahoma", 10f, FontStyle.Bold))
                { TextRenderer.DrawText(g, _title, f, new Point(46, 9), Color.Black); }
            };
            _head.MouseDown += delegate(object s, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, 0xA1, (IntPtr)2, IntPtr.Zero); }
            };
            Controls.Add(_head);

            ComboBox dev = new ComboBox();
            dev.DropDownStyle = ComboBoxStyle.DropDownList;
            dev.Items.Add("Virtual Audio Output");
            dev.SelectedIndex = 0;
            dev.SetBounds(w - 295, 7, 170, 21);
            _head.Controls.Add(dev);

            HelpArrowButton help = new HelpArrowButton();
            help.SetBounds(w - 122, 7, 58, 22);
            help.HelpClick += delegate { MessageBox.Show(this, "Audio effects panel.\r\nEqualizer, Surround Mixer and Speaker Settings.", _title); };
            _head.Controls.Add(help);

            Label min = new Label();
            min.Text = "_"; min.Font = new Font("Tahoma", 10f, FontStyle.Bold);
            min.ForeColor = Color.FromArgb(110, 110, 110);
            min.BackColor = Color.Transparent; min.Cursor = BlueCursor.Arrow;
            min.SetBounds(w - 50, 8, 24, 22); min.TextAlign = ContentAlignment.MiddleCenter;
            min.Click += delegate { WindowState = FormWindowState.Minimized; };
            _head.Controls.Add(min);

            Label close = new Label();
            close.Text = "X"; close.Font = new Font("Tahoma", 9f);
            close.ForeColor = Color.FromArgb(110, 110, 110);
            close.BackColor = Color.Transparent; close.Cursor = BlueCursor.Arrow;
            close.SetBounds(w - 28, 8, 22, 22); close.TextAlign = ContentAlignment.MiddleCenter;
            close.Click += delegate { Close(); };
            _head.Controls.Add(close);
        }

        private void BuildBar(int w, int y)
        {
            _bar = new Panel();
            _bar.SetBounds(0, y, w, BarH);
            _bar.Paint += delegate(object s, PaintEventArgs e)
            {
                Bitmap t = Assets.Get("bar_tile");
                if (t == null)
                {
                    Rectangle rr = new Rectangle(0, 0, _bar.Width, _bar.Height);
                    using (LinearGradientBrush b = new LinearGradientBrush(rr, Color.FromArgb(246, 246, 248), Color.FromArgb(176, 176, 182), 90f))
                    { e.Graphics.FillRectangle(b, rr); }
                    return;
                }
                for (int x = 0; x < _bar.Width; x += t.Width)
                    e.Graphics.DrawImage(t, x, 0, t.Width, BarH);
            };
            Controls.Add(_bar);

            SkinButton def = new SkinButton();
            def.Text = "Default";
            def.SetBounds(20, 3, 88, 24);
            def.Click += delegate { DoDefault(); Reload(); };
            _bar.Controls.Add(def);

            string[] kinds = new string[] { "eq", "speaker", "mixer" };
            string[] tips = new string[] { "Equalizer", "Speaker Settings", "Surround Mixer" };
            ToolTip tt = new ToolTip();
            for (int i = 0; i < kinds.Length; i++)
            {
                string k = kinds[i];
                BarIcon nb = new BarIcon(k);
                nb.On = (k == _kind);
                nb.Location = new Point(118 + i * 32, 1);
                nb.Click += delegate { EqPanel.OpenFrom(k, this); };
                tt.SetToolTip(nb, tips[i]);
                _bar.Controls.Add(nb);
            }
        }

        protected BarIcon AddToggle(string kind, string tip, int x)
        {
            BarIcon b = new BarIcon(kind);
            b.Location = new Point(x, 1);
            new ToolTip().SetToolTip(b, tip);
            _bar.Controls.Add(b);
            return b;
        }

        // لوح رمادي مدور مثل الاصلي
        protected Panel Group(Control parent, string title, int x, int y, int w, int h)
        {
            Panel p = new Panel();
            p.SetBounds(x, y, w, h);
            p.Paint += delegate(object s, PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, p.Width - 1, p.Height - 1);
                using (GraphicsPath gp = Skin.Round(r, 7))
                {
                    using (Brush b = new SolidBrush(Skin.Panel)) { g.FillPath(b, gp); }
                    // شكل ثلاثي الابعاد غائر (Sunken): ظل داخلي فوق + لمعة داخلية تحت
                    g.SetClip(gp);
                    using (Pen inTop = new Pen(Color.FromArgb(60, 0, 0, 0), 2f)) { g.DrawLine(inTop, r.X, r.Y + 1, r.Right, r.Y + 1); }
                    using (Pen inBot = new Pen(Color.FromArgb(140, 255, 255, 255), 1.4f)) { g.DrawLine(inBot, r.X + 1, r.Bottom, r.Right - 1, r.Bottom); }
                    g.ResetClip();
                    using (Pen pe = new Pen(Color.FromArgb(165, 160, 165))) { g.DrawPath(pe, gp); }
                }
            };
            p.BackColor = Skin.Back;
            if (!string.IsNullOrEmpty(title))
            {
                Label l = new Label();
                l.Text = title;
                l.AutoSize = true;
                l.BackColor = Color.Transparent;
                l.Location = new Point(12, 8);
                p.Controls.Add(l);
            }
            parent.Controls.Add(p);
            return p;
        }

        protected Label Cap(Control parent, string text, int x, int y, int w, ContentAlignment al)
        {
            Label l = new Label();
            l.Text = text;
            l.TextAlign = al;
            l.SetBounds(x, y, w, 16);
            l.BackColor = Color.Transparent;
            parent.Controls.Add(l);
            return l;
        }

        protected CSlider VBar(Control parent, int x, int y, int h)
        {
            CSlider t = new CSlider();
            t.Kind = SliderKind.Mixer;
            t.Orientation = Orientation.Vertical;
            t.Minimum = 0; t.Maximum = 100;
            t.PadTop = 10; t.PadBottom = 10;
            t.SetBounds(x, y, 30, h);
            parent.Controls.Add(t);
            return t;
        }

        protected CSlider HBar(Control parent, int x, int y, int w, int min, int max)
        {
            CSlider t = new CSlider();
            t.Kind = SliderKind.Horiz;
            t.Minimum = min; t.Maximum = max;
            t.PadTop = 10; t.PadBottom = 10;
            t.SetBounds(x, y, w, 26);
            parent.Controls.Add(t);
            return t;
        }

        // صف تبويبات مثل الاصلي (المحدد رمادي بحواف مدورة فوق)
        protected sealed class Tabs : Control
        {
            private readonly string[] _names;
            private int _sel;
            public event EventHandler Changed;
            public Tabs(string[] names)
            {
                _names = names;
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
                Height = 29;
                Cursor = BlueCursor.Arrow;
            }
            public int Selected { get { return _sel; } set { if (value != _sel) { _sel = value; Invalidate(); if (Changed != null) Changed(this, EventArgs.Empty); } } }
            protected override void OnMouseDown(MouseEventArgs e)
            {
                int w = Width / _names.Length;
                int i = e.X / Math.Max(1, w);
                if (i >= _names.Length) i = _names.Length - 1;
                Selected = i;
                base.OnMouseDown(e);
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.White);
                int w = Width / _names.Length;
                for (int i = 0; i < _names.Length; i++)
                {
                    if (i == _sel)
                    {
                        Rectangle r = new Rectangle(i * w + 1, 0, w - 2, Height + 10);
                        using (GraphicsPath gp = Skin.Round(r, 7))
                        {
                            using (LinearGradientBrush b = new LinearGradientBrush(new Rectangle(r.X, r.Y, r.Width, Height), Color.FromArgb(246, 246, 246), Color.FromArgb(222, 219, 222), 90f))
                            { g.FillPath(b, gp); }
                            using (Pen p = new Pen(Color.FromArgb(150, 150, 154))) { g.DrawPath(p, gp); }
                        }
                    }
                    TextRenderer.DrawText(g, _names[i], Font, new Rectangle(i * w, 0, w, Height - 2), Color.Black,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen p = new Pen(Color.FromArgb(120, 120, 126))) { e.Graphics.DrawRectangle(p, 0, 0, Width - 1, Height - 1); }
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible) { Reload(); BlueCursor.Apply(this); }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!EqPanel.Closing && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                return;
            }
            base.OnFormClosing(e);
        }
    }
}
