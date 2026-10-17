using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace SnapPin
{
    internal static class Program
    {
        private const string MutexName = "SnapPin_SingleInstance";

        [STAThread]
        private static void Main(string[] args)
        {
            if (args.Length > 0 &&
                string.Equals(args[0], "--selftest", StringComparison.OrdinalIgnoreCase))
            {
                Environment.ExitCode = SelfTest.Run();
                return;
            }

            bool createdNew;
            using (var mutex = new Mutex(true, MutexName, out createdNew))
            {
                if (!createdNew)
                {
                    NativeMethods.RequestCaptureBroadcast();
                    return;
                }

                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += (s, e) => HandleException(e.Exception);
                AppDomain.CurrentDomain.UnhandledException +=
                    (s, e) => HandleException(e.ExceptionObject as Exception);

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new SnapPinApp());
            }
        }

        private static void HandleException(Exception ex)
        {
            if (ex == null)
            {
                return;
            }

            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log");
                File.AppendAllText(
                    path,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + ex
                    + Environment.NewLine + Environment.NewLine);
            }
            catch
            {
            }

            try
            {
                MessageBox.Show(
                    "发生未处理的错误：" + ex.Message + "\n详情已写入 error.log。",
                    "SnapPin",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch
            {
            }
        }
    }
}
