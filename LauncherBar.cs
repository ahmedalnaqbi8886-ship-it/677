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

        private readonly FxChain _fx;
        private Panel _iconRow;
        private bool _collapsed;

        public LauncherBar(FxChain fx)
        {
            _fx = fx;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ClientSize = new Size(662, 54);
            BackColor = Color.White;
            Cursor = BlueCursor.Arrow;
            BlueCursor.Apply(this);
            BlueCursor.Pin(this);

            Panel top = new Panel();
            top.SetBounds(0, 0, ClientSize.Width, 48);
            top.Paint += delegate(object s, PaintEventArgs e)
            {
                Rectangle r = new Rectangle(0, 0, top.Width, top.Height);
                using (LinearGradientBrush b = new LinearGradientBrush(r, Color.FromArgb(252, 252, 253), Color.FromArgb(200, 201, 206), 90f))
                { e.Graphics.FillRectangle(b, r); }
            };
            top.MouseDown += delegate(object s, MouseEventArgs e)
            { if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, 0xA1, (IntPtr)2, IntPtr.Zero); } };
            Controls.Add(top);

            Panel bottom = new Panel();
            bottom.SetBounds(0, 48, ClientSize.Width, 6);
            bottom.BackColor = Color.FromArgb(40, 42, 48);
            Controls.Add(bottom);

            Img(top, "ms_green", 3, 1, "فتح Graphic Equalizer", delegate { Open("eq"); });
            Img(top, "ms_logo", 40, 0, null, null);
            ImgBtn(top, "ms_go", 132, 7, "فتح Graphic Equalizer", delegate { Open("eq"); });
            ImgBtn(top, "ms_arrow", 182, 7, "طي/عرض الأيقونات", delegate { ToggleCollapse(); });

            _iconRow = new Panel();
            _iconRow.SetBounds(216, 0, ClientSize.Width - 216, 48);
            _iconRow.BackColor = Color.Transparent;
            top.Controls.Add(_iconRow);

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

            ImgBtn(top, "ms_x", ClientSize.Width - 42, 3, "إغلاق الشريط", delegate { Close(); });
        }

        private void Open(string kind)
        {
            EqPanel.Open(_fx, kind);
        }

        private void ToggleCollapse()
        {
            _collapsed = !_collapsed;
            _iconRow.Visible = !_collapsed;
            ClientSize = new Size(_collapsed ? 220 : 662, ClientSize.Height);
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
            base.OnFormClosing(e);
        }
    }
}
