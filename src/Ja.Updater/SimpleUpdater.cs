using System;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Windows.Threading;
using Microsoft.Win32;
using NLog;

namespace Ja.Updater
{
    public class SimpleUpdater
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        #region Properties

        public string Url { get; set; }
        public string FileName { get; set; }
        public string ProductName { get; set; }
        public string RegistryKeyForInstalledVersion { get; set; }
        public string Background { get; set; }
        public bool ForceUpdate { get; private set; }

        #endregion Properties

        public SimpleUpdater()
        {
            Url = GetConfigOrDefault("BC.Updater.Url", "");
            FileName = GetConfigOrDefault("BC.Updater.FileName", "");
            ProductName = GetConfigOrDefault("BC.Updater.ProductName", "");
            RegistryKeyForInstalledVersion = GetConfigOrDefault("BC.Updater.VersionRegistryLocation", "");
            Background = GetConfigOrDefault("BC.Updater.Background", "");

            if (Url == "") throw new JaException("Please specify 'BC.Updater.Url'");
            if (FileName == "") throw new JaException("Please specify 'BC.Updater.FileName'");
            if (ProductName == "") throw new JaException("Please specify 'BC.Updater.ProductName'");
            if (RegistryKeyForInstalledVersion == "") throw new JaException("Please specify 'BC.Updater.VersionRegistryLocation'");
        }

        public Version GetCurrentlyInstalledVersion()
        {
            Version.TryParse((string)Registry.GetValue(RegistryKeyForInstalledVersion, null, "0.0.0"), out var installedVersion);
            return installedVersion;
        }

        /// <summary>
        /// blocks until updated or no update found/required and returns the current Version string e.g. 2.0.17
        /// </summary>
        public Version CheckSynchronized(Dispatcher dispatcher, string user, string passwd, string domain)
        {
            var installedVersion = GetCurrentlyInstalledVersion();

            Version webVersion;
            Version minVersion = null;
            string otherSwitch = null;

            #region parse things in version.txt from web folder
            try
            {
                var downloadedLines = new WebClient().DownloadString(Url + "version.txt").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                Version.TryParse(downloadedLines[0], out webVersion);

                foreach (var line in downloadedLines.Skip(1))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    if (line.Trim().StartsWith("#")) continue;
                    if (line.Trim().StartsWith(";")) continue;

                    var parts = line.Split(new[] { '=', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Count() != 2) continue;


                    switch (parts[0].Trim().ToLower())
                    {
                        case "minimumrequiredversion":
                        case "minimumversion":
                        case "minversion":
                            Version.TryParse(parts[1], out minVersion);
                            break;
                        case "otherswitch":
                            otherSwitch = parts[1];
                            break;
                    }
                }
            }
            catch (WebException)
            {
                return installedVersion;
            }
            #endregion

            if (webVersion == null) return installedVersion;

            if (installedVersion == null) ForceUpdate = true;
            else if (minVersion != null && minVersion > installedVersion) ForceUpdate = true;

            Logger.Info($"currently installed: {installedVersion} | web version: {webVersion} | min version: {minVersion}");
            if (ForceUpdate || webVersion > installedVersion)
            {
                // either exits app while installing or don't change anything - so installedVersion stays same
                dispatcher.Invoke(new Action(() => new NewVersionMessageBox(ProductName, Url, FileName, webVersion, Background, ForceUpdate, user, passwd, domain).ShowDialog()));
            }

            return installedVersion;
        }

        private static string GetConfigOrDefault(string key, string defaultValue) => !String.IsNullOrEmpty(ConfigurationManager.AppSettings[key]) ? ConfigurationManager.AppSettings[key] : defaultValue;

        private static bool GetConfigOrDefault(string key, bool defaultValue) => !String.IsNullOrEmpty(ConfigurationManager.AppSettings[key]) ? Convert.ToBoolean(ConfigurationManager.AppSettings[key], CultureInfo.InvariantCulture) : defaultValue;

        private static int GetConfigOrDefault(string key, int defaultValue) => !String.IsNullOrEmpty(ConfigurationManager.AppSettings[key]) ? Convert.ToInt32(ConfigurationManager.AppSettings[key], CultureInfo.InvariantCulture) : defaultValue;

        static double GetConfigOrDefault(string key, double defaultValue) => !String.IsNullOrEmpty(ConfigurationManager.AppSettings[key]) ? Convert.ToDouble(ConfigurationManager.AppSettings[key], CultureInfo.InvariantCulture) : defaultValue;
    }
}
