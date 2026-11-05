using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace EaxReverbEngine
{
    // شريط Creative MediaSource - مشغل سريع يفتح نوافذ الكرت (زي الاصلي بالضبط)
    internal sealed class LauncherBar : Form
    {
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, IntPtr l);

        private const int BarH = 48;

        private readonly FxChain _fx;
        private Panel _top;
        private Panel _bottom;
        private Panel _iconRow;
        private bool _collapsed;
        private int _collapsedWidth;
        private int _expandedWidth;

        // اخفاء تلقائي زي شريط المهام: يختفي الا شوي من فوق، ولما الماوس يلمس اعلى الشاشة يطلع كامل
        private Timer _hoverTimer;
        private bool _revealed = true;
        private int _barX;
        private int _hiddenY;
        private const int PeekPx = 2;

        public LauncherBar(FxChain fx)
        {
            _fx = fx;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.White;
            Cursor = BlueCursor.Arrow;
            BlueCursor.Apply(this);
            BlueCursor.Pin(this);

            // عرض مبدئي مؤقت - يتعدل تلقائيًا بالاخير حسب حجم الصور الحقيقي (يمنع أي تداخل/قص)
            int tempW = 900;

            _top = new Panel();
            _top.SetBounds(0, 0, tempW, BarH);
            _top.Paint += delegate(object s, PaintEventArgs e)
            {
                Rectangle r = new Rectangle(0, 0, _top.Width, _top.Height);
                using (LinearGradientBrush b = new LinearGradientBrush(r, Color.FromArgb(252, 252, 253), Color.FromArgb(200, 201, 206), 90f))
                { e.Graphics.FillRectangle(b, r); }
            };
            _top.MouseDown += delegate(object s, MouseEventArgs e)
            { if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, 0xA1, (IntPtr)2, IntPtr.Zero); } };
            Controls.Add(_top);

            _bottom = new Panel();
            _bottom.SetBounds(0, BarH, tempW, 6);
            _bottom.BackColor = Color.FromArgb(40, 42, 48);
            Controls.Add(_bottom);

            int gx = Img(_top, "ms_green", 3, 3, "فتح Graphic Equalizer", delegate { Open("eq"); });
            int lx = Img(_top, "ms_logo", gx + 4, 6, null, null);
            int goX = Img(_top, "ms_go", lx + 12, 7, "فتح Graphic Equalizer", delegate { Open("eq"); });
            int arrX = Img(_top, "ms_arrow", goX + 8, 7, "طي/عرض الأيقونات", delegate { ToggleCollapse(); });

            int iconRowStart = arrX + 14;
            _iconRow = new Panel();
            _iconRow.SetBounds(iconRowStart, 0, 500, BarH);
            _iconRow.BackColor = Color.Transparent;
            _top.Controls.Add(_iconRow);

            int x = 0;
            x = Img(_iconRow, "ms_wand", x, 0, null, null) + 1;
            x = Img(_iconRow, "ms_bars", x, 0, "Surround Mixer", delegate { Open("mixer"); }) + 1;
            x = Img(_iconRow, "ms_oval", x, 0, "Speaker Settings", delegate { Open("speaker"); }) + 1;
            x = Img(_iconRow, "ms_pencil", x, 0, null, null) + 1;
            x = Img(_iconRow, "ms_play1", x, 0, null, null) + 1;
            x = Img(_iconRow, "ms_eqgrid", x, 0, "Graphic Equalizer", delegate { Open("eq"); }) + 1;
            x = Img(_iconRow, "ms_eax", x, 0, "EAX Settings", delegate { Open("eax"); }) + 1;
            x = Img(_iconRow, "ms_ovalcross", x, 0, null, null) + 1;
            x = Img(_iconRow, "ms_rec", x, 0, null, null) + 1;
            x = Img(_iconRow, "ms_swirlnote", x, 0, null, null) + 1;
            x = Img(_iconRow, "ms_play2", x, 0, null, null) + 1;
            _iconRow.Width = x;

            Bitmap xb = Assets.Get("ms_x");
            int closeW = xb != null ? xb.Width : 30;

            _collapsedWidth = iconRowStart + 16;
            _expandedWidth = iconRowStart + x + 16 + closeW + 12;

            ClientSize = new Size(_expandedWidth, BarH + 6);
            _top.Width = _expandedWidth;
            _bottom.Width = _expandedWidth;

            Img(_top, "ms_x", _expandedWidth - closeW - 10, 3, "إغلاق الشريط", delegate { Close(); });

            TopMost = true;
            _hiddenY = -(ClientSize.Height - PeekPx);
            _hoverTimer = new Timer();
            _hoverTimer.Interval = 120;
            _hoverTimer.Tick += delegate { PollAutoHide(); };
            _hoverTimer.Start();
        }

        // يحط الشريط بمكانه الافقي (X) مرة وحدة - الارتفاع (Y) يتحكم فيه الاخفاء التلقائي بعدين
        public void PlaceAt(int x)
        {
            _barX = x;
            Location = new Point(_barX, _revealed ? 0 : _hiddenY);
        }

        private void PollAutoHide()
        {
            try
            {
                Point p = Cursor.Position;
                bool nearTop = p.Y <= 4;
                bool overBar = Bounds.Contains(p);
                if (!_revealed && (nearTop || overBar))
                {
                    _revealed = true;
                    Location = new Point(_barX, 0);
                    TopMost = true;
                }
                else if (_revealed && !overBar && p.Y > (Bottom + 60))
                {
                    _revealed = false;
                    Location = new Point(_barX, _hiddenY);
                }
            }
            catch (Exception) { }
        }

        private void Open(string kind)
        {
            EqPanel.Open(_fx, kind);
        }

        private void ToggleCollapse()
        {
            _collapsed = !_collapsed;
            _iconRow.Visible = !_collapsed;
            int w = _collapsed ? _collapsedWidth : _expandedWidth;
            ClientSize = new Size(w, ClientSize.Height);
            _top.Width = w;
            _bottom.Width = w;
        }

        // يحط صورة مباشرة (بدون اطار زر) - يرجع نهاية x
        private int Img(Control parent, string asset, int x, int y, string tip, EventHandler click)
        {
            Bitmap b = Assets.Get(asset);
            PictureBox p = new PictureBox();
            p.Image = b;
            p.SizeMode = PictureBoxSizeMode.AutoSize;
            p.BackColor = Color.Transparent;
            p.Location = new Point(x, y);
            if (click != null) { p.Cursor = BlueCursor.Arrow; p.Click += click; }
            if (!string.IsNullOrEmpty(tip)) new ToolTip().SetToolTip(p, tip);
            parent.Controls.Add(p);
            return x + (b != null ? b.Width : 36);
        }

        // زر صورة (Go!, السهم, X) - نفس Img بس بدون فرق حاليا، للوضوح بالكود
        private void ImgBtn(Control parent, string asset, int x, int y, string tip, EventHandler click)
        {
            Img(parent, asset, x, y, tip, click);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!EqPanel.Closing && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                return;
            }
            try { if (_hoverTimer != null) { _hoverTimer.Stop(); _hoverTimer.Dispose(); _hoverTimer = null; } } catch (Exception) { }
            base.OnFormClosing(e);
        }
    }
}
