using System;
using System.Drawing;
using System.IO;

namespace SnapPin
{
    internal static class SelfTest
    {
        public static int Run()
        {
            string logPath = Path.Combine(Environment.CurrentDirectory, "selftest.log");
            try
            {
                using (Bitmap bitmap = ScreenCaptureService.CaptureVirtualScreen())
                {
                    string line = "SELFTEST OK " + bitmap.Width + "x" + bitmap.Height;
                    File.WriteAllText(logPath, line);
                    Console.WriteLine(line);
                    return 0;
                }
            }
            catch (Exception ex)
            {
                string line = "SELFTEST FAILED " + ex.GetType().Name + ": " + ex.Message;
                File.WriteAllText(logPath, line);
                Console.WriteLine(line);
                return 1;
            }
        }
    }
}
