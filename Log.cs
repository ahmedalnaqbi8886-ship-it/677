using System;
using System.IO;

namespace EaxReverbEngine
{
    public static class Log
    {
        private static readonly object Sync = new object();
        private static string _path;

        public static void Init(string dir)
        {
            try { _path = Path.Combine(dir, "engine.log"); }
            catch { _path = null; }
        }

        public static void Write(string msg)
        {
            try
            {
                lock (Sync)
                {
                    if (_path != null)
                        File.AppendAllText(_path, DateTime.Now.ToString("HH:mm:ss.fff") + " " + msg + Environment.NewLine);
                }
            }
            catch { }
        }

        public static void Write(string fmt, params object[] args)
        {
            try { Write(string.Format(fmt, args)); }
            catch { Write(fmt); }
        }
    }
}
