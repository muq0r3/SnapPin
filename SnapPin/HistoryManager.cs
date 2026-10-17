using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;

namespace SnapPin
{
    internal static class HistoryManager
    {
        private const int MaxEntries = 30;
        private static string _configuredDirectory;

        public static void Configure(string directory)
        {
            _configuredDirectory = directory;
        }

        public static string HistoryDirectory()
        {
            if (!string.IsNullOrWhiteSpace(_configuredDirectory))
            {
                try
                {
                    Directory.CreateDirectory(_configuredDirectory);
                    return _configuredDirectory;
                }
                catch
                {
                    // 配置的目录不可用，回退到默认。
                }
            }

            string portable = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "history");
            if (TryCreateDirectory(portable))
            {
                return portable;
            }

            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SnapPin",
                "history");
            Directory.CreateDirectory(dir);
            return dir;
        }

        private static bool TryCreateDirectory(string path)
        {
            try
            {
                Directory.CreateDirectory(path);
                string testFile = Path.Combine(path, ".write_test");
                File.WriteAllText(testFile, string.Empty);
                File.Delete(testFile);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void Add(Bitmap image)
        {
            Bitmap clone;
            try
            {
                clone = new Bitmap(image);
            }
            catch
            {
                return;
            }

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    string path = Path.Combine(
                        HistoryDirectory(),
                        DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".png");
                    clone.Save(path, ImageFormat.Png);
                }
                catch
                {
                    // 历史记录写入失败不应影响截图流程。
                }
                finally
                {
                    clone.Dispose();
                }
                Prune();
            });
        }

        public static List<string> ListEntries()
        {
            var list = new List<string>();
            try
            {
                foreach (string file in Directory.GetFiles(HistoryDirectory(), "*.png")
                    .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
                    .Take(MaxEntries))
                {
                    list.Add(file);
                }
            }
            catch
            {
            }
            return list;
        }

        public static Bitmap Load(string path)
        {
            try
            {
                return new Bitmap(path);
            }
            catch
            {
                return null;
            }
        }

        public static void Clear()
        {
            try
            {
                foreach (string file in Directory.GetFiles(HistoryDirectory(), "*.png"))
                {
                    File.Delete(file);
                }
            }
            catch
            {
            }
        }

        private static void Prune()
        {
            try
            {
                string[] files = Directory.GetFiles(HistoryDirectory(), "*.png");
                if (files.Length <= MaxEntries)
                {
                    return;
                }

                foreach (string file in files
                    .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
                    .Skip(MaxEntries))
                {
                    File.Delete(file);
                }
            }
            catch
            {
            }
        }
    }
}
