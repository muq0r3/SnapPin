using System.Windows.Forms;
using Microsoft.Win32;

namespace SnapPin
{
    internal static class AutostartHelper
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "SnapPin";

        public static bool IsEnabled()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey))
            {
                return key != null && key.GetValue(ValueName) != null;
            }
        }

        public static void SetEnabled(bool enabled)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKey))
            {
                if (key == null)
                {
                    return;
                }

                if (enabled)
                {
                    key.SetValue(ValueName, Application.ExecutablePath);
                }
                else
                {
                    key.DeleteValue(ValueName, false);
                }
            }
        }
    }
}
