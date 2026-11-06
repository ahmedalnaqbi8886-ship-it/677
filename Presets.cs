using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace EaxReverbEngine
{
    // وزنيات EQ المحفوظة بأسماء المستخدم (تبقى بعد اغلاق البرنامج)
    internal sealed class UserPreset
    {
        public string Name;
        public int[] Bands = new int[FxChain.Bands];
        public int Level;
    }

    internal static class UserPresets
    {
        private static readonly List<UserPreset> Items = new List<UserPreset>();
        private static bool _loaded;

        private static string FilePath()
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CreativeEAXSettings");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "eq_presets.txt");
        }

        public static List<UserPreset> All()
        {
            Load();
            return Items;
        }

        private static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            try
            {
                string p = FilePath();
                if (!File.Exists(p)) return;
                foreach (string line in File.ReadAllLines(p, Encoding.UTF8))
                {
                    string[] a = line.Split('|');
                    if (a.Length < 3 || a[0].Length == 0) continue;
                    UserPreset u = new UserPreset();
                    u.Name = a[0];
                    string[] b = a[1].Split(',');
                    for (int i = 0; i < FxChain.Bands && i < b.Length; i++)
                    {
                        int v;
                        if (int.TryParse(b[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out v)) u.Bands[i] = v;
                    }
                    int lv;
                    int.TryParse(a[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out lv);
                    u.Level = lv;
                    Items.Add(u);
                }
            }
            catch (Exception) { }
        }

        private static void Save()
        {
            try
            {
                List<string> L = new List<string>();
                foreach (UserPreset u in Items)
                {
                    string[] b = new string[u.Bands.Length];
                    for (int i = 0; i < b.Length; i++) b[i] = u.Bands[i].ToString(CultureInfo.InvariantCulture);
                    L.Add(u.Name.Replace("|", " ") + "|" + string.Join(",", b) + "|" + u.Level.ToString(CultureInfo.InvariantCulture));
                }
                File.WriteAllLines(FilePath(), L.ToArray(), Encoding.UTF8);
            }
            catch (Exception) { }
        }

        public static UserPreset Find(string name)
        {
            Load();
            foreach (UserPreset u in Items) if (string.Equals(u.Name, name, StringComparison.OrdinalIgnoreCase)) return u;
            return null;
        }

        public static void Put(string name, int[] bands, int level)
        {
            Load();
            name = name.Replace("|", " ").Trim();
            UserPreset u = Find(name);
            if (u == null) { u = new UserPreset(); u.Name = name; Items.Add(u); }
            for (int i = 0; i < FxChain.Bands; i++) u.Bands[i] = bands[i];
            u.Level = level;
            Save();
        }

        public static void Remove(string name)
        {
            Load();
            UserPreset u = Find(name);
            if (u != null) { Items.Remove(u); Save(); }
        }
    }

    // نافذة صغيرة لكتابة الاسم
    internal sealed class NameDialog : Form
    {
        private readonly TextBox _tb = new TextBox();

        private NameDialog(string title)
        {
            Text = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(300, 96);
            Font = UiFont.Get(8.25f);
            BackColor = Color.White;
            Cursor = BlueCursor.Arrow;
            Label l = new Label();
            l.Text = "Preset name:";
            l.SetBounds(12, 12, 276, 16);
            Controls.Add(l);
            _tb.SetBounds(12, 32, 276, 21);
            _tb.MaxLength = 32;
            Controls.Add(_tb);
            SkinButton ok = new SkinButton();
            ok.Text = "Save"; ok.SetBounds(120, 62, 80, 24);
            ok.Click += delegate { if (_tb.Text.Trim().Length > 0) { DialogResult = DialogResult.OK; } };
            Controls.Add(ok);
            SkinButton no = new SkinButton();
            no.Text = "Cancel"; no.SetBounds(208, 62, 80, 24);
            no.Click += delegate { DialogResult = DialogResult.Cancel; };
            Controls.Add(no);
            AcceptButton = null;
            _tb.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter && _tb.Text.Trim().Length > 0) { e.SuppressKeyPress = true; DialogResult = DialogResult.OK; }
            };
        }

        public static string Ask(IWin32Window owner, string title)
        {
            using (NameDialog d = new NameDialog(title))
            {
                if (d.ShowDialog(owner) == DialogResult.OK) return d._tb.Text.Trim();
                return null;
            }
        }
    }
}
