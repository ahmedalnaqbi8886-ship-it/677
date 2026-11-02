using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using Microsoft.Win32;

namespace EaxReverbEngine
{
    internal enum LicState { Ok, NoKey, BadKey, WrongHwid, Expired, Revoked, Offline, ClockTampered }

    // نظام الاشتراك: مفتاح مربوط بـ HWID الجهاز، يتحقق منه اونلاين من سيرفر التراخيص
    // (LicenseServer) كل ما يفتح البرنامج - يقدر صاحب البرنامج يلغي اي اشتراك او يمدده
    // من لوحة التحكم /admin بدون ما يطلع نسخة جديدة. لو ما فيه نت، يشتغل بفترة سماح
    // (GraceDays) على آخر حالة "فعّال" وصلت من السيرفر، بعدها لازم يتصل بالنت يجدد.
    internal static class License
    {
        // رابط سيرفر التراخيص (Render) - بدّله برابطك الحقيقي بعد ما تنشره (بدون / بالاخر).
        // او بدل ما تعدل الكود: حط ملف اسمه license_url.txt فيه الرابط بمجلد
        // %AppData%\CreativeEAXSettings على جهاز العميل، وياخذ الاولوية على هذا الثابت.
        private const string DefaultServerUrl = "https://YOUR-LICENSE-SERVER.onrender.com";
        private const int GraceDays = 3;
        private const int TimeoutMs = 8000;

        public static string PlanName = "";
        public static DateTime Expiry = DateTime.MaxValue;

        private static string Dir()
        {
            string d = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CreativeEAXSettings");
            Directory.CreateDirectory(d);
            return d;
        }

        private static string ServerUrl()
        {
            try
            {
                string f = Path.Combine(Dir(), "license_url.txt");
                if (File.Exists(f))
                {
                    string u = File.ReadAllText(f).Trim();
                    if (u.Length > 0) return u.TrimEnd('/');
                }
            }
            catch (Exception) { }
            return DefaultServerUrl.TrimEnd('/');
        }

        private static string RegVal(RegistryHive hive, string path, string name)
        {
            try
            {
                using (RegistryKey bk = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64))
                using (RegistryKey k = bk.OpenSubKey(path))
                {
                    if (k == null) return "";
                    object v = k.GetValue(name);
                    return v == null ? "" : v.ToString();
                }
            }
            catch (Exception) { return ""; }
        }

        // بصمة الجهاز: 20 خانة hex
        public static string Hwid()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(RegVal(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Cryptography", "MachineGuid"));
            sb.Append("|").Append(RegVal(RegistryHive.LocalMachine, @"HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString"));
            sb.Append("|").Append(RegVal(RegistryHive.LocalMachine, @"HARDWARE\DESCRIPTION\System\CentralProcessor\0", "Identifier"));
            sb.Append("|").Append(RegVal(RegistryHive.LocalMachine, @"HARDWARE\DESCRIPTION\System\BIOS", "BaseBoardProduct"));
            sb.Append("|").Append(RegVal(RegistryHive.LocalMachine, @"HARDWARE\DESCRIPTION\System\BIOS", "SystemProductName"));
            using (System.Security.Cryptography.SHA256 h = System.Security.Cryptography.SHA256.Create())
            {
                byte[] d = h.ComputeHash(Encoding.UTF8.GetBytes("EAXLIC1|" + sb));
                StringBuilder o = new StringBuilder();
                for (int i = 0; i < 10; i++) o.Append(d[i].ToString("X2"));
                return o.ToString();
            }
        }

        public static string HwidPretty()
        {
            string h = Hwid();
            return h.Substring(0, 4) + "-" + h.Substring(4, 4) + "-" + h.Substring(8, 4) + "-" + h.Substring(12, 4) + "-" + h.Substring(16, 4);
        }

        private static string CacheFile() { return Path.Combine(Dir(), "lic.cache"); }
        private static string StampFile() { return Path.Combine(Dir(), "lic.stamp"); }

        // كشف ارجاع الوقت للخلف (عشان ما يلعبون بفترة السماح بتغيير ساعة الجهاز)
        private static bool ClockBack()
        {
            try
            {
                long last = 0;
                string f = StampFile();
                if (File.Exists(f)) long.TryParse(File.ReadAllText(f).Trim(), out last);
                try
                {
                    object v = Registry.GetValue(@"HKEY_CURRENT_USER\Software\CreativeEAXSettings", "ls", null);
                    long rv;
                    if (v != null && long.TryParse(v.ToString(), out rv) && rv > last) last = rv;
                }
                catch (Exception) { }
                long now = DateTime.UtcNow.Ticks;
                if (last > 0 && now < last - TimeSpan.TicksPerHour * 36) return true;
                long keep = Math.Max(now, last);
                File.WriteAllText(f, keep.ToString());
                try { Registry.SetValue(@"HKEY_CURRENT_USER\Software\CreativeEAXSettings", "ls", keep.ToString()); } catch (Exception) { }
                return false;
            }
            catch (Exception) { return false; }
        }

        // يحفظ اخر رد صريح وصل من السيرفر لهذا المفتاح (عشان فترة السماح بدون نت)
        private static void SaveCache(string key, string word, string plan, string expires)
        {
            try
            {
                string[] L = new string[] { key, word, plan ?? "", expires ?? "", DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture) };
                File.WriteAllLines(CacheFile(), L);
            }
            catch (Exception) { }
        }

        private static bool LoadCache(string key, out string word, out string plan, out string expires, out DateTime savedUtc)
        {
            word = ""; plan = ""; expires = ""; savedUtc = DateTime.MinValue;
            try
            {
                string f = CacheFile();
                if (!File.Exists(f)) return false;
                string[] L = File.ReadAllLines(f);
                if (L.Length < 5) return false;
                if (!string.Equals(L[0], key, StringComparison.Ordinal)) return false; // كاش مفتاح ثاني - تجاهله
                word = L[1]; plan = L[2]; expires = L[3];
                long ticks;
                if (!long.TryParse(L[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out ticks)) return false;
                savedUtc = new DateTime(ticks, DateTimeKind.Utc);
                return true;
            }
            catch (Exception) { return false; }
        }

        private static string HttpGet(string url)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Timeout = TimeoutMs;
            req.ReadWriteTimeout = TimeoutMs;
            req.Method = "GET";
            req.UserAgent = "EaxLicenseClient/1";
            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
            using (Stream s = resp.GetResponseStream())
            using (StreamReader r = new StreamReader(s, Encoding.UTF8))
            {
                return r.ReadToEnd();
            }
        }

        private static LicState WordToState(string w)
        {
            switch ((w ?? "").Trim().ToUpperInvariant())
            {
                case "ACTIVE": return LicState.Ok;
                case "REVOKED": return LicState.Revoked;
                case "EXPIRED": return LicState.Expired;
                case "WRONGHWID": return LicState.WrongHwid;
                default: return LicState.BadKey; // NOTFOUND او اي رد غريب
            }
        }

        private static DateTime ParseExpiry(string s)
        {
            if (string.IsNullOrEmpty(s) || string.Equals(s, "NEVER", StringComparison.OrdinalIgnoreCase)) return DateTime.MaxValue;
            DateTime d;
            if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out d)) return d;
            return DateTime.MaxValue;
        }

        // يتحقق من نص المفتاح: يسأل سيرفر التراخيص اول شي، ولو ما وصل النت يرجع
        // لآخر حالة محفوظة لنفس المفتاح (فترة سماح GraceDays ايام بس)
        public static LicState Validate(string key)
        {
            PlanName = ""; Expiry = DateTime.MaxValue;
            if (string.IsNullOrEmpty(key)) return LicState.NoKey;
            key = key.Trim();
            string hwid = Hwid();

            string body = null;
            try
            {
                string url = ServerUrl() + "/validate?key=" + Uri.EscapeDataString(key) + "&hwid=" + Uri.EscapeDataString(hwid);
                body = HttpGet(url).Trim();
            }
            catch (Exception) { body = null; }

            if (!string.IsNullOrEmpty(body))
            {
                string[] parts = body.Split('|');
                string word = parts.Length > 0 ? parts[0].Trim() : "";
                string plan = parts.Length > 1 ? parts[1].Trim() : "";
                string expires = parts.Length > 2 ? parts[2].Trim() : "";
                LicState state = WordToState(word);
                SaveCache(key, word, plan, expires);
                if (state == LicState.Ok)
                {
                    PlanName = plan;
                    Expiry = ParseExpiry(expires);
                }
                return state;
            }

            // ما قدرنا نوصل للسيرفر (بدون نت) - نرجع لآخر حالة معروفة لنفس المفتاح
            string cw, cp, ce; DateTime saved;
            if (!LoadCache(key, out cw, out cp, out ce, out saved)) return LicState.Offline;
            if (!string.Equals(cw, "ACTIVE", StringComparison.OrdinalIgnoreCase)) return WordToState(cw);
            DateTime exp = ParseExpiry(ce);
            if (DateTime.UtcNow >= exp) return LicState.Expired;
            if (ClockBack()) return LicState.ClockTampered;
            if ((DateTime.UtcNow - saved).TotalDays > GraceDays) return LicState.Offline;
            PlanName = cp; Expiry = exp;
            return LicState.Ok;
        }

        public static string Load()
        {
            try
            {
                string f = Path.Combine(Dir(), "license.key");
                return File.Exists(f) ? File.ReadAllText(f).Trim() : "";
            }
            catch (Exception) { return ""; }
        }

        public static void Store(string key)
        {
            try { File.WriteAllText(Path.Combine(Dir(), "license.key"), key.Trim()); } catch (Exception) { }
        }

        public static LicState Check() { return Validate(Load()); }

        public static string Describe(LicState s)
        {
            switch (s)
            {
                case LicState.Ok:
                    return (Expiry == DateTime.MaxValue)
                        ? "Active - " + (string.IsNullOrEmpty(PlanName) ? "Lifetime" : PlanName)
                        : "Active - " + PlanName + " - expires " + Expiry.ToString("yyyy-MM-dd");
                case LicState.NoKey: return "No license key.";
                case LicState.BadKey: return "Invalid license key.";
                case LicState.WrongHwid: return "This key belongs to another device.";
                case LicState.Expired: return "Subscription expired.";
                case LicState.Revoked: return "This subscription was revoked.";
                case LicState.Offline: return "Can't reach the license server - check your internet connection.";
                default: return "System clock was changed.";
            }
        }
    }
}
