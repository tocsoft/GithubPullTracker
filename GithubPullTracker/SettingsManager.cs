using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using static GithubPullTracker.SettingsManager;

namespace GithubPullTracker
{
    public class SettingsManager
    {
        public static SettingsManager Settings { get; } = new SettingsManager();

        List<SettingsManagerProvider> providers = new List<SettingsManagerProvider>();

        public void RegisterProvider(SettingsManagerProvider provider)
        {
            providers.Add(provider);
        }

        public string GetSetting(string path)
        {
            foreach(var p in providers)
            {
                var value = p.GetValue(path);
                if (value != null)
                    return value;
            }
            return null;
        }

        public abstract class SettingsManagerProvider
        {
            public abstract string GetValue(string path);
        }

      

    }

    public static class SettingsManagerExtensions
    {
        public static SettingsManager AddFromConfig(this SettingsManager settings)
        {
            settings.RegisterProvider(new AppSettingsSettingsManagerProvider());
            return settings;
        }

        public class AppSettingsSettingsManagerProvider : SettingsManagerProvider
        {
            public override string GetValue(string path)
            {
                //app settings are seperated by colon first and fallback to seperated by dots
                return ConfigurationManager.AppSettings[path.Replace('.', ':')] ?? ConfigurationManager.AppSettings[path];
            }
        }

        public static SettingsManager AddEnvironmentVariables(this SettingsManager settings)
        {
            settings.RegisterProvider(new EnvironmentVariablesSettingsManagerProvider());
            return settings;
        }

        public class EnvironmentVariablesSettingsManagerProvider : SettingsManagerProvider
        {
            public override string GetValue(string path)
            {
                var envName = path.Replace('.', '_').ToUpper();
                return Environment.GetEnvironmentVariable(envName);
            }
        }


        public static SettingsManager AddJsonSettings(this SettingsManager settings, string filename)
        {
            var path = Path.Combine(HttpRuntime.AppDomainAppPath, filename);
            settings.RegisterProvider(new JsonSettingsManagerProvider(path));
            return settings;

        }

        public static SettingsManager AddDevlopmentSettings(this SettingsManager settings, string name)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);                
            var path = Path.Combine(appData, "Development", "Settings", name + ".json");
            settings.RegisterProvider(new JsonSettingsManagerProvider(path));
            return settings;
        }

        public class JsonSettingsManagerProvider : SettingsManagerProvider
        {
            private readonly string filePath;
            private Dictionary<string, string> settings = null;
            private bool settingsLoaded = false;
            public JsonSettingsManagerProvider(string path)
            {
                this.filePath = Path.GetFullPath(path);

                var dir = Path.GetDirectoryName(path);
                var fsw = new FileSystemWatcher(dir, "*.json");
                fsw.Changed += Fsw_CallBack;
                fsw.Created+= Fsw_CallBack;
                fsw.Deleted += Fsw_CallBack;
                fsw.Renamed += Fsw_CallBack;
                fsw.EnableRaisingEvents = true;
                
            }

            private void Fsw_CallBack(object sender, FileSystemEventArgs e)
            {
                if(e.FullPath == filePath)
                {
                    //if file changed in any way we drop the settings object
                    settings = null;
                    settingsLoaded = false;
                }
            }
            object _locker = new object();
            public override string GetValue(string path)
            {
                if (!settingsLoaded)
                {
                    lock (_locker)
                    {
                        //if null then try to load it from file
                        try
                        {
                            var localsettings = JObject.Parse(File.ReadAllText(filePath));
                            settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                            var all = localsettings.DescendantsAndSelf();
                            foreach (var s in all)
                            {
                                var v = s as JValue;
                                if (v != null)
                                {
                                    settings.Add(v.Path, v.Value.ToString());
                                }
                            }
                        }
                        catch
                        {
                            settings = null;
                            //failed to load then act as empty
                        }
                        settingsLoaded = true;
                    }
                }
                if (settings == null)
                {
                    return null;
                }

                if (settings.ContainsKey(path))
                {
                    return settings[path];
                }

                return null;
            }
        }
    }
}