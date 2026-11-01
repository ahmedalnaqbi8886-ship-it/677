using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;

namespace EaxReverbEngine
{
    internal enum LicState { Ok, NoKey, BadKey, Revoked, Expired, NoServer }

    // نظام اشتراك مبني على موقع/سيرفر - مو مربوط بجهاز (HWID).
    // المفتاح نفسه رقم عشوائي من السيرفر، والسيرفر هو اللي يقرر صالح/ملغى/منتهي.
    // كذا تقدر تلغي اشتراك اي عميل في اي وقت من لوحة تحكم السيرفر (LicenseServer/admin.html)
    // بدون ما تحتاج تصدر نسخة جديدة من البرنامج.
    internal static class License
    {
        // رابط سيرفر التراخيص - لازم تبدله برابط موقعك الحقيقي بعد ما ترفعه (شوف LicenseServer/README.txt)
        private const string ServerUrl = "https://YOUR-LICENSE-SERVER.example.com";

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

        // يسأل السيرفر عن حالة المفتاح. السيرفر يرجع سطر نصي: STATUS|PLAN|EXPIRES
        // STATUS = ACTIVE | REVOKED | EXPIRED | NOTFOUND
        private static string AskServer(string key)
        {
            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                string url = ServerUrl.TrimEnd('/') + "/validate?key=" + Uri.EscapeDataString(key);
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "GET";
                req.Timeout = 8000;
                req.ReadWriteTimeout = 8000;
                req.UserAgent = "EaxReverbEngine-LicenseClient";
                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                using (Stream s = resp.GetResponseStream())
                using (StreamReader rd = new StreamReader(s, Encoding.UTF8))
                {
                    return rd.ReadToEnd().Trim();
                }
            }
            catch (Exception) { return null; } // نت مقطوع او السيرفر واقف - نرجع null ونعتمد على الكاش
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

        // يتحقق من مفتاح: يسأل السيرفر اولا، ولو ما وصل يستخدم اخر نتيجة محفوظة ضمن فترة سماح محدودة
        public static LicState Validate(string key)
        {
            PlanName = ""; Expiry = null;
            key = (key ?? "").Trim();
            if (string.IsNullOrEmpty(key)) return LicState.NoKey;
            if (key.Length < 8) return LicState.BadKey;

            string resp = AskServer(key);
            if (resp != null)
            {
                string[] parts = resp.Split('|');
                string status = parts.Length > 0 ? parts[0] : "";
                string plan = parts.Length > 1 ? parts[1] : "";
                string exp = parts.Length > 2 ? parts[2] : "";
                WriteCache(status, plan, exp);
                return FromStatus(status, plan, exp);
            }

            // ما قدرنا نوصل للسيرفر - نسمح بفترة سماح قصيرة بناء على اخر تحقق ناجح فقط
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
