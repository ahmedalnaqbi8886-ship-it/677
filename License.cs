using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;

namespace EaxReverbEngine
{
    internal enum LicState { Ok, NoKey, BadKey, Revoked, Expired, NoServer }

    // نظام اشتراك بسيط بدون سيرفر حقيقي - مو مربوط بجهاز (HWID).
    // البرنامج يقرأ ملف نصي عام (licenses.txt) من GitHub Pages، سطر لكل عميل:
    //   KEY|STATUS|PLAN|EXPIRES
    // تلغي اشتراك اي عميل بأي وقت بس بفتح الملف بـ GitHub وتبدل ACTIVE الى REVOKED وتعمل Commit.
    // شوف LicenseData/README.txt لخطوات الاعداد (GitHub Pages، بدون استضافة/سيرفر منفصل).
    internal static class License
    {
        // رابط ملف الملخصات النصي (raw) بعد ما تفعّل GitHub Pages - بدله برابطك الحقيقي
        private const string ServerUrl = "https://YOUR-USERNAME.github.io/license-data/licenses.txt";

        public static string PlanName = "";
        public static DateTime? Expiry = null; // null = Lifetime او بدون تاريخ معروف

        // كم يوم نسمح للبرنامج يشتغل بدون ما يوصل للسيرفر (مثلا انقطاع نت مؤقت عند العميل)
        private const int GraceDays = 3;

        private static string Dir()
        {
            string d = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CreativeEAXSettings");
            Directory.CreateDirectory(d);
            return d;
        }

        private static string KeyFile() { return Path.Combine(Dir(), "license.key"); }
        private static string CacheFile() { return Path.Combine(Dir(), "license.cache"); }

        public static string Load()
        {
            try { return File.Exists(KeyFile()) ? File.ReadAllText(KeyFile()).Trim() : ""; }
            catch (Exception) { return ""; }
        }

        public static void Store(string key)
        {
            try { File.WriteAllText(KeyFile(), key.Trim()); } catch (Exception) { }
        }

        // يحمل ملف النصوص كامل (كل مرة - بدون كاش HTTP حتى ما ياخذ نسخة قديمة محفوظة بالمتصفح/الشبكة)
        private static string FetchFile()
        {
            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                string url = ServerUrl + (ServerUrl.IndexOf('?') >= 0 ? "&" : "?") + "t=" + DateTime.UtcNow.Ticks;
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "GET";
                req.Timeout = 8000;
                req.ReadWriteTimeout = 8000;
                req.UserAgent = "EaxReverbEngine-LicenseClient";
                req.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                using (Stream s = resp.GetResponseStream())
                using (StreamReader rd = new StreamReader(s, Encoding.UTF8))
                {
                    return rd.ReadToEnd();
                }
            }
            catch (Exception) { return null; } // نت مقطوع او الملف مو موصول - نرجع null ونعتمد على الكاش
        }

        // يدور سطر المفتاح داخل الملف: KEY|STATUS|PLAN|EXPIRES  (اسطر تبدأ بـ # تتجاهل)
        private static string FindLine(string text, string key)
        {
            if (text == null) return null;
            string[] lines = text.Replace("\r", "").Split('\n');
            foreach (string raw in lines)
            {
                string line = raw.Trim();
                int hash = line.IndexOf('#');
                if (hash >= 0) line = line.Substring(0, hash).Trim(); // يشيل تعليق # بآخر السطر لو موجود
                if (line.Length == 0) continue;
                int i = line.IndexOf('|');
                if (i <= 0) continue;
                string k = line.Substring(0, i).Trim();
                if (string.Equals(k, key, StringComparison.OrdinalIgnoreCase)) return line;
            }
            return null;
        }

        private static void WriteCache(string status, string plan, string expiresIso)
        {
            try
            {
                string line = DateTime.UtcNow.Ticks + "|" + status + "|" + plan + "|" + expiresIso;
                File.WriteAllText(CacheFile(), line);
            }
            catch (Exception) { }
        }

        private static bool ReadCache(out string status, out string plan, out string expiresIso, out DateTime lastOk)
        {
            status = ""; plan = ""; expiresIso = ""; lastOk = DateTime.MinValue;
            try
            {
                if (!File.Exists(CacheFile())) return false;
                string[] p = File.ReadAllText(CacheFile()).Trim().Split('|');
                if (p.Length < 4) return false;
                long ticks;
                if (!long.TryParse(p[0], out ticks)) return false;
                lastOk = new DateTime(ticks, DateTimeKind.Utc);
                status = p[1]; plan = p[2]; expiresIso = p[3];
                return true;
            }
            catch (Exception) { return false; }
        }

        private static LicState FromStatus(string status, string plan, string expiresIso)
        {
            PlanName = plan;
            Expiry = null;
            DateTime exp;
            if (DateTime.TryParse(expiresIso, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out exp))
            {
                Expiry = exp;
            }

            switch (status)
            {
                case "ACTIVE": return LicState.Ok;
                case "REVOKED": return LicState.Revoked;
                case "EXPIRED": return LicState.Expired;
                case "NOTFOUND": return LicState.BadKey;
                default: return LicState.BadKey;
            }
        }

        // يتحقق من مفتاح: يحمل ملف النصوص ويدور السطر، ولو ما وصل يستخدم اخر نتيجة محفوظة ضمن فترة سماح
        public static LicState Validate(string key)
        {
            PlanName = ""; Expiry = null;
            key = (key ?? "").Trim();
            if (string.IsNullOrEmpty(key)) return LicState.NoKey;
            if (key.Length < 8) return LicState.BadKey;

            string text = FetchFile();
            if (text != null)
            {
                string line = FindLine(text, key);
                if (line == null)
                {
                    WriteCache("NOTFOUND", "", "");
                    return LicState.BadKey;
                }
                string[] f = line.Split('|');
                string status = (f.Length > 1 ? f[1] : "").Trim().ToUpperInvariant();
                string plan = (f.Length > 2 ? f[2] : "").Trim();
                string expText = (f.Length > 3 ? f[3] : "").Trim();

                // لو التاريخ المكتوب بالملف فات، نعتبرها منتهية حتى لو حد نسى يبدلها يدوي
                DateTime expDate;
                bool hasExp = DateTime.TryParse(expText, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out expDate);
                if (status == "ACTIVE" && hasExp && DateTime.UtcNow >= expDate) status = "EXPIRED";

                string expIso = hasExp ? expDate.ToString("o") : "";
                WriteCache(status, plan, expIso);
                return FromStatus(status, plan, expIso);
            }

            // ما قدرنا نوصل للملف - نسمح بفترة سماح قصيرة بناء على اخر تحقق ناجح فقط
            string cStatus, cPlan, cExp; DateTime lastOk;
            if (ReadCache(out cStatus, out cPlan, out cExp, out lastOk))
            {
                bool clockOk = DateTime.UtcNow >= lastOk; // لو الساعة رجعت للخلف نعتبرها غير موثوقة
                if (cStatus == "ACTIVE" && clockOk && (DateTime.UtcNow - lastOk).TotalDays <= GraceDays)
                {
                    return FromStatus(cStatus, cPlan, cExp);
                }
            }
            return LicState.NoServer;
        }

        public static LicState Check() { return Validate(Load()); }

        public static string Describe(LicState s)
        {
            switch (s)
            {
                case LicState.Ok:
                    return Expiry.HasValue
                        ? "Active - " + PlanName + " - expires " + Expiry.Value.ToString("yyyy-MM-dd")
                        : "Active - " + PlanName;
                case LicState.NoKey: return "No license key.";
                case LicState.BadKey: return "Invalid license key.";
                case LicState.Revoked: return "This subscription was cancelled.";
                case LicState.Expired: return "Subscription expired.";
                case LicState.NoServer: return "Can't reach the license server. Check your internet connection.";
                default: return "Unknown license state.";
            }
        }
    }
}
