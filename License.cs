using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace EaxReverbEngine
{
    internal enum LicState { Ok, NoKey, BadKey, WrongHwid, Expired, ClockTampered }

    // نظام الاشتراك: مفتاح موقّع (ECDSA P-256) مربوط بـ HWID الجهاز
    // اسبوع (W) / شهر (M) / مدى الحياة (L)
    internal static class License
    {
        // المفتاح العام (الخاص عندك فقط داخل KeyGen.html)
        private const string PubX = "de0f0fca199977d9aa42a5e16cf2e7ed9d19af0760dd61f30624c50a008d49b2";
        private const string PubY = "e10646500401a91d5528dd7186dc5d9c41f5f9d07050d324309d251118e8bbcf";

        public static string PlanName = "";
        public static DateTime Expiry = DateTime.MaxValue;

        private static string Dir()
        {
            string d = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CreativeEAXSettings");
            Directory.CreateDirectory(d);
            return d;
        }

        private static byte[] Hex(string s)
        {
            byte[] b = new byte[s.Length / 2];
            for (int i = 0; i < b.Length; i++) b[i] = Convert.ToByte(s.Substring(i * 2, 2), 16);
            return b;
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
            using (SHA256 h = SHA256.Create())
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

        private static bool VerifySig(byte[] data, byte[] sig)
        {
            try
            {
                byte[] blob = new byte[8 + 64];
                BitConverter.GetBytes(0x31534345).CopyTo(blob, 0);
                BitConverter.GetBytes(32).CopyTo(blob, 4);
                Hex(PubX).CopyTo(blob, 8);
                Hex(PubY).CopyTo(blob, 40);
                using (CngKey key = CngKey.Import(blob, CngKeyBlobFormat.EccPublicBlob))
                using (ECDsaCng ec = new ECDsaCng(key))
                {
                    ec.HashAlgorithm = CngAlgorithm.Sha256;
                    return ec.VerifyData(data, sig);
                }
            }
            catch (Exception) { return false; }
        }

        private static byte[] FromB64Url(string s)
        {
            s = s.Replace('-', '+').Replace('_', '/');
            while (s.Length % 4 != 0) s += "=";
            return Convert.FromBase64String(s);
        }

        // يتحقق من نص المفتاح (بدون كتابة اي شيء)
        public static LicState Validate(string key)
        {
            PlanName = ""; Expiry = DateTime.MaxValue;
            if (string.IsNullOrEmpty(key)) return LicState.NoKey;
            try
            {
                key = key.Trim().Replace("\r", "").Replace("\n", "").Replace(" ", "");
                byte[] all = FromB64Url(key);
                if (all.Length < 70) return LicState.BadKey;
                int len = all[0];
                if (len < 10 || 1 + len + 64 != all.Length) return LicState.BadKey;
                byte[] data = new byte[len];
                Array.Copy(all, 1, data, 0, len);
                byte[] sig = new byte[64];
                Array.Copy(all, 1 + len, sig, 0, 64);
                if (!VerifySig(data, sig)) return LicState.BadKey;
                string[] p = Encoding.ASCII.GetString(data).Split('|');
                if (p.Length != 3) return LicState.BadKey;
                if (!string.Equals(p[0], Hwid(), StringComparison.OrdinalIgnoreCase)) return LicState.WrongHwid;
                if (p[1] == "L") { PlanName = "Lifetime"; Expiry = DateTime.MaxValue; }
                else
                {
                    DateTime exp;
                    if (!DateTime.TryParseExact(p[2], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out exp)) return LicState.BadKey;
                    PlanName = p[1] == "W" ? "Weekly" : (p[1] == "M" ? "Monthly" : "Custom");
                    Expiry = exp.AddDays(1);
                    if (DateTime.UtcNow >= Expiry) return LicState.Expired;
                }
                if (ClockBack()) return LicState.ClockTampered;
                return LicState.Ok;
            }
            catch (Exception) { return LicState.BadKey; }
        }

        private static string StampFile() { return Path.Combine(Dir(), "lic.stamp"); }

        // كشف ارجاع الوقت للخلف
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
                    return PlanName == "Lifetime" ? "Active - Lifetime" : "Active - " + PlanName + " - expires " + Expiry.AddDays(-1).ToString("yyyy-MM-dd");
                case LicState.NoKey: return "No license key.";
                case LicState.BadKey: return "Invalid license key.";
                case LicState.WrongHwid: return "This key belongs to another device.";
                case LicState.Expired: return "Subscription expired.";
                default: return "System clock was changed.";
            }
        }
    }
}
