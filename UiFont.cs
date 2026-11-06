using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;

namespace EaxReverbEngine
{
    // اختيار خط آمن: نتأكد ان "Tahoma" فعلا مثبت بجهاز المستخدم قبل لا نستخدمه. بعض الاجهزة
    // (وندوز مخفف/مخصص للصوتيات مثلا) ما يجي معها Tahoma افتراضيًا، ووقتها GDI+ يستبدلها
    // بخط ثاني موجود بالجهاز من غير ما يرمي أي خطأ - فيطلع شكل خط غريب (مزخرف/خطي) بدل
    // الخط الاصلي النظيف. هذا الكلاس يدور على افضل خط متوفر فعليًا بدل ما يفترض ان Tahoma موجود.
    internal static class UiFont
    {
        private static readonly string[] Prefer = { "Tahoma", "Microsoft Sans Serif", "Segoe UI", "Arial" };
        private static string _family;

        private static string Family()
        {
            if (_family != null) return _family;
            try
            {
                using (InstalledFontCollection col = new InstalledFontCollection())
                {
                    HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (FontFamily f in col.Families) names.Add(f.Name);
                    foreach (string p in Prefer)
                    {
                        if (names.Contains(p)) { _family = p; return _family; }
                    }
                }
            }
            catch (Exception ex) { Log.Write("uifont: " + ex.Message); }
            // ما لقينا ولا وحد من قايمتنا - آخر حل نستخدم خط النظام الافتراضي (مضمون موجود)
            try { _family = SystemFonts.DefaultFont.FontFamily.Name; }
            catch (Exception) { _family = "Arial"; }
            return _family;
        }

        public static Font Get(float size, FontStyle style)
        {
            try { return new Font(Family(), size, style); }
            catch (Exception ex)
            {
                Log.Write("uifont get: " + ex.Message);
                return new Font(FontFamily.GenericSansSerif, size, style);
            }
        }

        public static Font Get(float size) { return Get(size, FontStyle.Regular); }
    }
}
