using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace SnapPin
{
    [DataContract]
    internal sealed class AppSettings
    {
        [DataMember(Name = "captureHotkey")]
        public string CaptureHotkey { get; set; }

        [DataMember(Name = "pinHotkey")]
        public string PinHotkey { get; set; }

        [DataMember(Name = "historyDirectory")]
        public string HistoryDirectory { get; set; }

        public static AppSettings CreateDefault()
        {
            return new AppSettings
            {
                CaptureHotkey = "Ctrl+Alt+A",
                PinHotkey = "Ctrl+Alt+P",
                HistoryDirectory = string.Empty
            };
        }

        public static AppSettings Load()
        {
            string path = SettingsPath();
            AppSettings defaults = CreateDefault();

            if (!File.Exists(path))
            {
                return defaults;
            }

            try
            {
                using (FileStream stream = File.OpenRead(path))
                {
                    var serializer = new DataContractJsonSerializer(typeof(AppSettings));
                    var loaded = (AppSettings)serializer.ReadObject(stream);
                    if (string.IsNullOrWhiteSpace(loaded.CaptureHotkey))
                    {
                        loaded.CaptureHotkey = defaults.CaptureHotkey;
                    }
                    if (string.IsNullOrWhiteSpace(loaded.PinHotkey))
                    {
                        loaded.PinHotkey = defaults.PinHotkey;
                    }
                    if (loaded.HistoryDirectory == null)
                    {
                        loaded.HistoryDirectory = defaults.HistoryDirectory;
                    }
                    return loaded;
                }
            }
            catch
            {
                return defaults;
            }
        }

        public void Save()
        {
            try
            {
                string path = SettingsPath();
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                using (FileStream stream = File.Create(path))
                {
                    var serializer = new DataContractJsonSerializer(typeof(AppSettings));
                    serializer.WriteObject(stream, this);
                }
            }
            catch
            {
                // 配置写入失败不应阻止程序启动。
            }
        }

        public static string SettingsPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SnapPin",
                "settings.json");
        }
    }
}
