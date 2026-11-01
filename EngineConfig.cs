using System;
using System.IO;

namespace EaxReverbEngine
{
    public sealed class EngineConfig
    {
        public static string DirForApp()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CreativeEAXSettings");
        }

        private const string Template = "# EAX reverb engine\r\n# CaptureDevice  = input device substring; empty = auto (VoiceMeeter Out B1, else default mic)\r\n# CaptureDevice2 = second input; none = off; empty = auto\r\n# RenderDevice   = substring of output device name (empty = auto: CABLE/Sonar/VoiceMeeter)\r\n# PluginPath     = full path to ReverbPresets64.dll (empty = auto)\r\nBufferMs=20\r\nBufferCount=6\r\n";

        public static EngineConfig Load(string dir)
        {
            EngineConfig cfg = new EngineConfig();
            string path = Path.Combine(dir, "engine.ini");
            try
            {
                if (!File.Exists(path))
                {
                    File.WriteAllText(path, Template);
                }
                foreach (string line in File.ReadAllLines(path))
                {
                    string t = line.Trim();
                    if (t.Length == 0 || t.StartsWith("#")) continue;
                    int eq = t.IndexOf('=');
                    if (eq <= 0) continue;
                    string key = t.Substring(0, eq).Trim();
                    string val = t.Substring(eq + 1).Trim();
                    int n;
                    if (key.Equals("CaptureDevice", StringComparison.OrdinalIgnoreCase)) cfg.CaptureDevice = val;
                    else if (key.Equals("CaptureDevice2", StringComparison.OrdinalIgnoreCase)) cfg.CaptureDevice2 = val;
                    else if (key.Equals("RenderDevice", StringComparison.OrdinalIgnoreCase)) cfg.RenderDevice = val;
                    else if (key.Equals("PluginPath", StringComparison.OrdinalIgnoreCase)) cfg.PluginPath = val;
                    else if (key.Equals("BufferMs", StringComparison.OrdinalIgnoreCase))
                    {
                        if (int.TryParse(val, out n) && n >= 5 && n <= 200) cfg.BufferMs = n;
                    }
                    else if (key.Equals("BufferCount", StringComparison.OrdinalIgnoreCase))
                    {
                        if (int.TryParse(val, out n) && n >= 2 && n <= 32) cfg.BufferCount = n;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Write("config load: " + ex.Message);
            }
            return cfg;
        }

        public string CaptureDevice = "";
        public string CaptureDevice2 = "";
        public string RenderDevice = "";
        public string PluginPath = "";
        public int BufferMs = 20;
        public int BufferCount = 6;
    }
}
