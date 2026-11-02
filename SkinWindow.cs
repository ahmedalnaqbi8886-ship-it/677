using System;
using System.Drawing;
using System.Windows.Forms;

namespace EaxReverbEngine
{
    // نافذة تخصيص الشكل: تختار صورة من جهازك (Browse) لكل عنصر بدل اللي مرسوم بالكود.
    // متاحة لأي مستخدم (انت او العميل) من زر "Skin..." بالشريط السفلي لأي نافذة.
    internal sealed class SkinWindow : Form
    {
        private sealed class Row
        {
            public string Name;
            public PictureBox Pic;
            public Label Status;
        }

        private readonly System.Collections.Generic.List<Row> _rows = new System.Collections.Generic.List<Row>();

        public SkinWindow()
        {
            Text = "Customize Look";
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(460, 300);
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(560, 520);
            Font = new Font("Tahoma", 8.25f);
            BackColor = Color.White;
            Cursor = BlueCursor.Arrow;

            Label title = new Label();
            title.Text = "Pick your own image for any part of the skin";
            title.Font = new Font("Tahoma", 10.5f, FontStyle.Bold);
            title.SetBounds(14, 12, 500, 22);
            title.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(title);

            Label sub = new Label();
            sub.Text = "Browse = use your own PNG/JPG picture here.   Reset = back to the default look.";
            sub.ForeColor = Color.FromArgb(100, 100, 100);
            sub.SetBounds(14, 36, 520, 16);
            sub.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(sub);

            Panel list = new Panel();
            list.SetBounds(0, 60, ClientSize.Width, ClientSize.Height - 100);
            list.AutoScroll = true;
            list.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            list.BackColor = Color.White;
            Controls.Add(list);
            Cursor = BlueCursor.Arrow;

            int y = 4;
            for (int i = 0; i < Assets.OverridableNames.Length; i++)
            {
                string name = Assets.OverridableNames[i];
                string label = i < Assets.OverridableLabels.Length ? Assets.OverridableLabels[i] : name;
                AddRow(list, name, label, y);
                y += 54;
            }
            list.AutoScrollMinSize = new Size(0, y + 10);

            Label note = new Label();
            note.Text = "Changes apply right away to any open window - no need to restart.";
            note.ForeColor = Color.FromArgb(100, 100, 100);
            note.SetBounds(14, ClientSize.Height - 58, 400, 16);
            note.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            Controls.Add(note);

            SkinButton resetAll = new SkinButton();
            resetAll.Text = "Reset all";
            resetAll.SetBounds(ClientSize.Width - 192, ClientSize.Height - 42, 86, 26);
            resetAll.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            resetAll.Click += delegate { ResetAll(); };
            Controls.Add(resetAll);

            SkinButton close = new SkinButton();
            close.Text = "Close";
            close.SetBounds(ClientSize.Width - 100, ClientSize.Height - 42, 86, 26);
            close.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            close.Click += delegate { Close(); };
            Controls.Add(close);

            BlueCursor.Apply(this);
            BlueCursor.Pin(this);
        }

        private void AddRow(Control parent, string name, string label, int y)
        {
            Panel row = new Panel();
            row.SetBounds(10, y, parent.Width - 40, 48);
            row.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            row.BackColor = Color.White;

            PictureBox pic = new PictureBox();
            pic.SetBounds(0, 2, 70, 44);
            pic.BorderStyle = BorderStyle.FixedSingle;
            pic.SizeMode = PictureBoxSizeMode.Zoom;
            pic.BackColor = Color.FromArgb(245, 245, 248);
            row.Controls.Add(pic);

            Label l = new Label();
            l.Text = label;
            l.SetBounds(80, 4, 180, 16);
            row.Controls.Add(l);

            Label status = new Label();
            status.ForeColor = Color.FromArgb(0, 120, 0);
            status.SetBounds(80, 22, 180, 16);
            row.Controls.Add(status);

            SkinButton browse = new SkinButton();
            browse.Text = "Browse...";
            browse.SetBounds(268, 10, 90, 26);
            row.Controls.Add(browse);

            SkinButton reset = new SkinButton();
            reset.Text = "Reset";
            reset.SetBounds(364, 10, 70, 26);
            row.Controls.Add(reset);

            Row rec = new Row { Name = name, Pic = pic, Status = status };
            _rows.Add(rec);
            RefreshRow(rec);

            browse.Click += delegate { PickFor(rec); };
            reset.Click += delegate { Assets.ResetOverride(name); RefreshRow(rec); RepaintEverything(); };

            parent.Controls.Add(row);
        }

        private void RefreshRow(Row r)
        {
            try
            {
                Bitmap b = Assets.Get(r.Name);
                r.Pic.Image = b;
            }
            catch (Exception) { }
            r.Status.Text = Assets.HasOverride(r.Name) ? "custom image" : "default";
            r.Status.ForeColor = Assets.HasOverride(r.Name) ? Color.FromArgb(0, 120, 0) : Color.FromArgb(120, 120, 120);
        }

        private void PickFor(Row r)
        {
            using (OpenFileDialog d = new OpenFileDialog())
            {
                d.Title = "Choose an image";
                d.Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp|All files|*.*";
                if (d.ShowDialog(this) != DialogResult.OK) return;
                if (!Assets.SetOverride(r.Name, d.FileName))
                {
                    MessageBox.Show(this, "Couldn't load that image file.", "Customize Look");
                    return;
                }
                RefreshRow(r);
                RepaintEverything();
            }
        }

        private void ResetAll()
        {
            if (MessageBox.Show(this, "Reset every custom image back to default?", "Customize Look",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            foreach (Row r in _rows) { Assets.ResetOverride(r.Name); RefreshRow(r); }
            RepaintEverything();
        }

        // يحدّث رسم كل النوافذ المفتوحة حالا حتى تبين الصور الجديدة بدون ما يسكرون ويرجعون يفتحون
        private static void RepaintEverything()
        {
            foreach (Form f in Application.OpenForms)
            {
                try { InvalidateAll(f); } catch (Exception) { }
            }
        }

        private static void InvalidateAll(Control c)
        {
            c.Invalidate(true);
            foreach (Control ch in c.Controls) InvalidateAll(ch);
        }
    }
}
