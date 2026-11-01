using System;
using System.Collections;
using System.Drawing;
using System.Reflection;

namespace EaxReverbEngine
{
    // يربط زر "Equalizer" الموجود في واجهة برنامج الكرت بنافذة الـ EQ
    // (بدل رسالة "The equalizer is not part of this EAX Settings window").
    internal static class EqHook
    {
        private const BindingFlags BF = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static Engine _eng;

        public static bool TryHook(object form, Engine eng)
        {
            _eng = eng;
            if (form == null) return false;
            FieldInfo wf = form.GetType().GetField("_widgets", BF);
            if (wf == null) { Log.Write("hook: _widgets not found"); return false; }
            IEnumerable list = wf.GetValue(form) as IEnumerable;
            if (list == null) return false;

            int hooked = 0;
            foreach (object w in list)
            {
                if (w == null) continue;
                Type t = w.GetType();
                if (t.Name == "IconButton")
                {
                    string tip = GetMember(w, "Tip") as string;
                    string kind = tip == "Equalizer" ? "eq" : (tip == "Speaker" ? "speaker" : null);
                    if (kind != null)
                    {
                        string k = kind;
                        if (ReplaceEvent(w, "Click", new EventHandler(delegate(object s2, EventArgs e2) { OpenKind(k); }))) hooked++;
                    }
                }
                else if (t.Name == "ActionZoneWidget")
                {
                    object b = GetMember(w, "Bounds");
                    if (b is Rectangle)
                    {
                        Rectangle r = (Rectangle)b;
                        string kind = null;
                        if (r.Y == 304 && r.Width == 25 && r.Height == 25)
                        {
                            if (r.X == 119) kind = "eq";
                            else if (r.X == 151) kind = "speaker";
                            else if (r.X == 183) kind = "mixer";
                        }
                        if (kind != null)
                        {
                            FieldInfo af = t.GetField("_action", BF);
                            string k = kind;
                            if (af != null) { af.SetValue(w, new Action(delegate() { OpenKind(k); })); hooked++; }
                        }
                    }
                }
            }
            Log.Write("hook: button(s) hooked = " + hooked);
            return hooked > 0;
        }

        private static void OpenKind(string kind)
        {
            try { if (_eng != null) EqPanel.Open(_eng.Fx, kind); }
            catch (Exception ex) { Log.Write("open panel: " + ex.Message); }
        }

        private static object GetMember(object obj, string name)
        {
            for (Type t = obj.GetType(); t != null; t = t.BaseType)
            {
                FieldInfo f = t.GetField(name, BF | BindingFlags.DeclaredOnly);
                if (f != null) return f.GetValue(obj);
                PropertyInfo p = t.GetProperty(name, BF | BindingFlags.DeclaredOnly);
                if (p != null && p.GetIndexParameters().Length == 0) return p.GetValue(obj, null);
            }
            return null;
        }

        private static bool ReplaceEvent(object obj, string evName, Delegate handler)
        {
            try
            {
                for (Type t = obj.GetType(); t != null; t = t.BaseType)
                {
                    FieldInfo f = t.GetField(evName, BF | BindingFlags.DeclaredOnly);
                    if (f != null && typeof(Delegate).IsAssignableFrom(f.FieldType))
                    {
                        f.SetValue(obj, handler);
                        return true;
                    }
                }
            }
            catch (Exception ex) { Log.Write("hook event: " + ex.Message); }
            return false;
        }
    }
}
