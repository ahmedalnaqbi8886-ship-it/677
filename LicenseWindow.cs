using System;
using System.Drawing;
using System.Windows.Forms;

namespace EaxReverbEngine
{
    // بوابة الاشتراك: تظهر نافذة التفعيل اذا ما فيه مفتاح صالح
    internal static class LicenseGate
    {
        private static System.Threading.Timer _timer;

        public static bool Ensure()
        {
            LicState s = License.Check();
            Log.Write("license: " + License.Describe(s) + " hwid=" + License.Hwid());
            if (s == LicState.Ok) return true;
            using (LicenseWindow w = new LicenseWindow(s))
            {
                w.ShowDialog();
                return w.Activated;
            }
        }

        // يفحص كل دقيقة؛ اذا انتهى الاشتراك يوقف المحرك
        public static void Watch(Action onExpired)
        {
            _timer = new System.Threading.Timer(delegate(object o)
            {
                try
                {
                    LicState s = License.Check();
                    if (s != LicState.Ok)
                    {
                        Log.Write("license: " + License.Describe(s) + " - stopping engine");
                        _timer.Dispose();
                        onExpired();
                    }
                }
                catch (Exception) { }
            }, null, 60000, 60000);
        }
    }

    internal sealed class LicenseWindow : Form
    {
        public bool Activated;
        private readonly TextBox _key = new TextBox();
        private readonly TextBox _hw = new TextBox();
        private readonly Label _status = new Label();
        private readonly SkinButton _act = new SkinButton();
        private readonly SkinButton _close = new SkinButton();

        public LicenseWindow(LicState initial)
        {
            Text = "Activation";
            // نافذة قابلة للتمديد حتى يقدر العميل يكبّرها ويشوف/يسحب الكي بسهولة
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true; MinimizeBox = true;
            MinimumSize = new Size(440, 300);
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(520, 360);
            Font = new Font("Tahoma", 8.25f);
            BackColor = Color.White;
            Cursor = BlueCursor.Arrow;

            Label title = new Label();
            title.Text = "Activate your subscription";
            title.Font = new Font("Tahoma", 11f, FontStyle.Bold);
            title.SetBounds(16, 14, 480, 22);
            title.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(title);

            Label plans = new Label();
            plans.Text = "Plans: Weekly  |  Monthly  |  Lifetime  (bound to this device)";
            plans.ForeColor = Color.FromArgb(90, 90, 90);
            plans.SetBounds(16, 40, 480, 16);
            plans.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(plans);

            Label h = new Label();
            h.Text = "Your device ID (HWID) - send it to get a key:";
            h.SetBounds(16, 68, 480, 16);
            h.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(h);

            _hw.ReadOnly = true;
            _hw.Text = License.HwidPretty();
            _hw.Font = new Font("Consolas", 12f, FontStyle.Bold);
            _hw.SetBounds(16, 88, 384, 28);
            _hw.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            // اضغطة بسيطة تسهّل التحديد: تحديد كامل تلقائي عند الدخول، وتحديد بالنقر
            _hw.Enter += delegate { _hw.SelectAll(); };
            _hw.Click += delegate { _hw.SelectAll(); };
            Controls.Add(_hw);

            SkinButton copy = new SkinButton();
            copy.Text = "Copy";
            copy.SetBounds(406, 88, 98, 28);
            copy.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            copy.Click += delegate
            {
                try { Clipboard.SetText(_hw.Text); Flash("HWID copied.", false); } catch (Exception) { }
            };
            Controls.Add(copy);

            Label k = new Label();
            k.Text = "License key (paste the one you received):";
            k.SetBounds(16, 128, 400, 16);
            k.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(k);

            _key.Multiline = true;
            _key.SetBounds(16, 148, 488, 110);
            _key.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            _key.Font = new Font("Consolas", 10f);
            _key.ScrollBars = ScrollBars.Vertical;
            _key.WordWrap = true;
            _key.AcceptsReturn = true;
            _key.AcceptsTab = false;
            Controls.Add(_key);

            SkinButton selAll = new SkinButton();
            selAll.Text = "Select All";
            selAll.SetBounds(16, 264, 98, 24);
            selAll.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            selAll.Click += delegate { _key.Focus(); _key.SelectAll(); };
            Controls.Add(selAll);

            SkinButton paste = new SkinButton();
            paste.Text = "Paste";
            paste.SetBounds(120, 264, 98, 24);
            paste.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            paste.Click += delegate
            {
                try { if (Clipboard.ContainsText()) { _key.Text = Clipboard.GetText(); _key.Focus(); _key.SelectAll(); } } catch (Exception) { }
            };
            Controls.Add(paste);

            _status.SetBounds(16, 294, 488, 18);
            _status.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _status.ForeColor = Color.FromArgb(190, 0, 0);
            _status.Text = initial == LicState.NoKey ? "" : License.Describe(initial);
            Controls.Add(_status);

            _act.Text = "Activate";
            _act.SetBounds(324, 322, 90, 28);
            _act.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _act.Click += delegate { Activate(); };
            Controls.Add(_act);

            _close.Text = "Close";
            _close.SetBounds(420, 322, 84, 28);
            _close.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _close.Click += delegate { Close(); };
            Controls.Add(_close);

            AcceptButton = null;
            string old = License.Load();
            if (!string.IsNullOrEmpty(old)) _key.Text = old;
        }

        private void Flash(string msg, bool error)
        {
            _status.ForeColor = error ? Color.FromArgb(190, 0, 0) : Color.FromArgb(0, 120, 0);
            _status.Text = msg;
        }

        private void Activate()
        {
            string key = _key.Text;
            LicState s = License.Validate(key);
            if (s == LicState.Ok)
            {
                License.Store(key);
                Activated = true;
                Flash(License.Describe(s), false);
                Close();
            }
            else
            {
                Flash(License.Describe(s), true);
            }
        }
    }
}
