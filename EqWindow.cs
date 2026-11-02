using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EaxReverbEngine
{
    // Graphic Equalizer - نفس تخطيط الاصلي (قياسات من اللقطة)
    internal sealed class EqWindow : PanelBase
    {
        private static readonly string[] BuiltNames = new string[] { "Flat", "Rock", "Pop", "Jazz", "Classical", "Bass", "Vocal" };
        private static readonly int[][] BuiltVals = new int[][]
        {
            new int[] { 0, 0, 0, 0, 0, 0, 0 },
            new int[] { 5, 3, -3, -4, -1, 3, 5 },
            new int[] { -1, 2, 4, 5, 3, -1, -2 },
            new int[] { 4, 2, -2, -2, 0, 3, 5 },
            new int[] { 5, 3, 0, 0, 0, -3, -4 },
            new int[] { 8, 6, 3, 0, 0, 0, 0 },
            new int[] { -2, -1, 2, 4, 4, 2, -1 }
        };

        private readonly CSlider[] _band = new CSlider[FxChain.Bands];
        private CSlider _lvl;
        private CheckBox _chk;
        private ComboBox _pre;

        public EqWindow(FxChain fx) : base(fx, "eq", "Graphic Equalizer", 500, 240)
        {
            Panel strip = new Panel();
            strip.SetBounds(0, 0, 500, 25);
            strip.BackColor = Color.FromArgb(221, 221, 221);
            strip.Paint += delegate(object s, PaintEventArgs e)
            {
                TextRenderer.DrawText(e.Graphics, "Equalizer", Font, new Rectangle(0, 0, 500, 25), Color.FromArgb(40, 40, 50),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            Body.Controls.Add(strip);

            Panel row = new Panel();
            row.SetBounds(0, 25, 500, 26);
            row.BackColor = Skin.EqCol;
            Body.Controls.Add(row);

            _chk = new CheckBox();
            _chk.Text = "Enable Equalizer";
            _chk.AutoSize = true;
            _chk.BackColor = Color.Transparent;
            _chk.Location = new Point(8, 4);
            _chk.CheckedChanged += delegate { if (!Loading) Fx.EqOn = _chk.Checked; };
            row.Controls.Add(_chk);

            Label pl = new Label();
            pl.Text = "Presets";
            pl.AutoSize = true;
            pl.BackColor = Color.Transparent;
            pl.Location = new Point(222, 6);
            row.Controls.Add(pl);

            _pre = new ComboBox();
            _pre.DropDownStyle = ComboBoxStyle.DropDownList;
            _pre.SetBounds(266, 2, 156, 21);
            _pre.SelectedIndexChanged += delegate
            {
                if (Loading) return;
                ApplyPreset(_pre.SelectedItem as string);
            };
            row.Controls.Add(_pre);

            Panel save = new Panel();
            save.SetBounds(432, 4, 18, 18);
            save.Cursor = BlueCursor.Arrow;
            save.Paint += delegate(object s, PaintEventArgs e)
            {
                using (Brush b = new SolidBrush(Skin.Blue)) { e.Graphics.FillRectangle(b, 1, 1, 15, 15); }
                using (Brush w = new SolidBrush(Color.White)) { e.Graphics.FillRectangle(w, 4, 2, 9, 5); e.Graphics.FillRectangle(w, 4, 10, 9, 5); }
            };
            save.Click += delegate { SavePreset(); };
            new ToolTip().SetToolTip(save, "Save preset");
            row.Controls.Add(save);

            Panel del = new Panel();
            del.SetBounds(468, 4, 18, 18);
            del.Cursor = BlueCursor.Arrow;
            del.Paint += delegate(object s, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (Pen p = new Pen(Skin.Blue, 3f)) { e.Graphics.DrawLine(p, 3, 3, 14, 14); e.Graphics.DrawLine(p, 14, 3, 3, 14); }
            };
            del.Click += delegate { DeletePreset(); };
            new ToolTip().SetToolTip(del, "Delete preset");
            row.Controls.Add(del);

            // مقياس dB يسار (مواقع من اللقطة الاصلية)
            Cap(Body, "12dB", 14, 76, 44, ContentAlignment.MiddleRight);
            Cap(Body, "0dB", 14, 131, 44, ContentAlignment.MiddleRight);
            Cap(Body, "-12dB", 14, 186, 44, ContentAlignment.MiddleRight);

            string[] names = new string[] { "125", "250", "500", "1k", "2k", "4k", "8k" };
            for (int i = 0; i < FxChain.Bands; i++)
            {
                int idx = i;
                CSlider tb = new CSlider();
                tb.Kind = SliderKind.Eq;
                tb.Column = true;
                tb.Orientation = Orientation.Vertical;
                tb.Minimum = -12; tb.Maximum = 12;
                tb.PadTop = 24; tb.PadBottom = 36;
                tb.Text = names[i];
                tb.SetBounds(64 + i * 47, 60, 34, 170);
                tb.ValueChanged += delegate
                {
                    if (Loading) return;
                    Fx.SetBand(idx, -tb.Value);
                    Loading = true; _pre.SelectedIndex = 0; Loading = false;
                };
                Body.Controls.Add(tb);
                _band[i] = tb;
            }

            Cap(Body, "0dB", 396, 82, 42, ContentAlignment.MiddleRight);
            Cap(Body, "-24dB", 396, 179, 42, ContentAlignment.MiddleRight);
            _lvl = new CSlider();
            _lvl.Kind = SliderKind.Eq;
            _lvl.Orientation = Orientation.Vertical;
            _lvl.Minimum = 0; _lvl.Maximum = 24;
            _lvl.PadTop = 30; _lvl.PadBottom = 43;
            _lvl.SetBounds(446, 60, 34, 170);
            _lvl.ValueChanged += delegate { if (!Loading) Fx.LevelDb = -_lvl.Value; };
            Body.Controls.Add(_lvl);
            Cap(Body, "Level", 435, 213, 56, ContentAlignment.MiddleCenter);

            RebuildPresets("*");
            Reload();
        }

        private void RebuildPresets(string select)
        {
            bool was = Loading;
            Loading = true;
            try
            {
                _pre.Items.Clear();
                _pre.Items.Add("*");
                foreach (string n in BuiltNames) _pre.Items.Add(n);
                foreach (UserPreset u in UserPresets.All()) _pre.Items.Add(u.Name);
                int i = _pre.Items.IndexOf(select);
                _pre.SelectedIndex = i < 0 ? 0 : i;
            }
            finally { Loading = was; }
        }

        private void ApplyPreset(string name)
        {
            if (string.IsNullOrEmpty(name) || name == "*") return;
            int[] v = null;
            int lv = 0;
            for (int i = 0; i < BuiltNames.Length; i++) if (BuiltNames[i] == name) v = BuiltVals[i];
            if (v == null)
            {
                UserPreset u = UserPresets.Find(name);
                if (u != null) { v = u.Bands; lv = u.Level; }
            }
            if (v == null) return;
            for (int b = 0; b < FxChain.Bands; b++) Fx.SetBand(b, v[b]);
            Fx.LevelDb = lv;
            Fx.EqOn = true;
            Reload();
        }

        private void SavePreset()
        {
            string name = NameDialog.Ask(this, "Save preset");
            if (string.IsNullOrEmpty(name)) return;
            int[] b = new int[FxChain.Bands];
            for (int i = 0; i < b.Length; i++) b[i] = (int)Math.Round(Fx.GetBand(i));
            UserPresets.Put(name, b, (int)Math.Round(Fx.LevelDb));
            RebuildPresets(name);
        }

        private void DeletePreset()
        {
            string name = _pre.SelectedItem as string;
            if (UserPresets.Find(name) != null)
            {
                if (MessageBox.Show(this, "Delete preset \"" + name + "\"?", "Presets", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                UserPresets.Remove(name);
                RebuildPresets("*");
            }
            else
            {
                Fx.ResetEq();
                Reload();
            }
        }

        protected override void DoDefault()
        {
            Fx.ResetEq();
            Fx.Bass = 0f;
            Fx.Surround = 0f;
            Fx.StereoOn = false;
        }

        // يطابق الوضع الحالي مع اسم وزنية (او "*")
        private string MatchName()
        {
            int lv = (int)Math.Round(Fx.LevelDb);
            for (int p = 0; p < BuiltNames.Length; p++)
            {
                bool same = lv == 0;
                for (int b = 0; same && b < FxChain.Bands; b++)
                    if ((int)Math.Round(Fx.GetBand(b)) != BuiltVals[p][b]) same = false;
                if (same) return BuiltNames[p];
            }
            foreach (UserPreset u in UserPresets.All())
            {
                bool same = lv == u.Level;
                for (int b = 0; same && b < FxChain.Bands; b++)
                    if ((int)Math.Round(Fx.GetBand(b)) != u.Bands[b]) same = false;
                if (same) return u.Name;
            }
            return "*";
        }

        public override void Reload()
        {
            bool was = Loading;
            Loading = true;
            try
            {
                _chk.Checked = Fx.EqOn;
                for (int i = 0; i < FxChain.Bands; i++) _band[i].Value = -(int)Math.Round(Fx.GetBand(i));
                _lvl.Value = -(int)Math.Round(Fx.LevelDb);
                int idx = _pre.Items.IndexOf(MatchName());
                _pre.SelectedIndex = idx < 0 ? 0 : idx;
            }
            finally { Loading = was; }
        }
    }
}
