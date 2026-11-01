using System;

namespace EaxReverbEngine
{
    public static class EngineDiagnostics
    {
        public static string[] CaptureDevices() { return Wave.ListIn(); }
        public static string[] RenderDevices() { return Wave.ListOut(); }
    }
}
