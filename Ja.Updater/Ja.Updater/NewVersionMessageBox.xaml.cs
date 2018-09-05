using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Timers;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NLog;

namespace Ja.Updater
{
    /// <summary>
    /// Interaction logic for NewVersionMessageBox.xaml
    /// </summary>
    internal partial class NewVersionMessageBox : Window, INotifyPropertyChanged
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        #region Fields

        private readonly string _url;
        private readonly string _fileName;
        private int _progressValue;
        private readonly WebClient _wc = new WebClient();

        #endregion Fields

        #region Properties

        public int ProgressValue
        {
            get => _progressValue;
            set
            {
                if (value == _progressValue) return;
                _progressValue = value;
                NotifyPropertyChanged("ProgressValue");
            }
        }

        public string NewVersionHeader { get; set; }
        public string NewVersionText { get; set; }
        public string OkString { get; set; }
        public string CancelString { get; set; }
        public bool ForceUpdate { get; set; }

        #endregion Properties

        #region Constructors

        internal NewVersionMessageBox()
        {
            InitializeComponent();

            NewVersionHeader = string.Format(Languages.Resource.NewVersionHeader, "bla");
            NewVersionText = string.Format(Languages.Resource.NewVersionText, "bla", "x.x.x");
            OkString = Languages.Resource.Ok;
            CancelString = Languages.Resource.Cancel;

            DataContext = this;
        }

        internal NewVersionMessageBox(string productName, string url, string fileName, Version webVersion, string background, bool forceUpdate, string user, string passwd, string domain)
        {
            InitializeComponent();

            if (background != "")
            {
                try
                {
                    var image = Image.FromFile(background); // Or wherever it comes from.
                    var bitmap = new Bitmap(image);
                    var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(bitmap.GetHbitmap(), IntPtr.Zero,
                                                                             Int32Rect.Empty,
                                                                             BitmapSizeOptions.FromEmptyOptions());
                    bitmap.Dispose();

                    MainGrid.Background = new ImageBrush(bitmapSource)
                    {
                        AlignmentX = AlignmentX.Left,
                        AlignmentY = AlignmentY.Top,
                        Stretch = Stretch.None
                    };
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }

            ForceUpdate = forceUpdate;
            _url = url;
            _fileName = fileName + "-" + webVersion + ".msi";

            OkString = Ja.Updater.Languages.Resource.Ok;
            CancelString = Languages.Resource.Cancel;
            ForceUpdateLabel.Content = Languages.Resource.ForceUpdateTooltip;
            NewVersionHeader = string.Format(Languages.Resource.NewVersionHeader, productName);
            NewVersionText = string.Format(Languages.Resource.NewVersionText, productName, webVersion);
            ProgressValue = 0;

            _wc.DownloadFileCompleted += (sender, e) => DownloadFileCompleted(user, passwd, domain);
            _wc.DownloadProgressChanged += (a, b) => ProgressValue = b.ProgressPercentage;

            DataContext = this;
            MyWebBrowser.Navigate(url + "changelog.htm");
        }

        #endregion Constructors

        #region Private Helpers

        private void DownloadFileCompleted(string user, string passwd, string domain)
        {
            InstallMsiWithCredentials(@"c:\temp\" + _fileName, user, passwd, domain);

            Application.Current.Shutdown();

            // In case it didn't work.
            var t = new Timer(1000);
            t.Elapsed += (a, b) => Environment.Exit(-1);
            t.Start();
        }

        private static void InstallMsiWithCredentials(string msiFile, string user, string passwd, string domain)
        {
            var ssPwd = new System.Security.SecureString();
            foreach (var c in passwd)
            {
                ssPwd.AppendChar(c);
            }

            try
            {
                Logger.Info($"runas msiexec with username={user}, domain={domain}, password=***");
                var p = new Process
                {
                    StartInfo =
                    {
                        FileName = "msiexec",
                        Arguments = "/i " + msiFile,
                        UseShellExecute = false,
                        Domain = domain,
                        UserName = user,
                        Password = ssPwd
                    }
                };
                p.Start();
            }
            catch (Win32Exception e)
            {
                Logger.Info(e, $"didn't work... run msiexec normally");
                var p = new Process
                {
                    StartInfo =
                    {
                        FileName = "msiexec",
                        Arguments = "/i " + msiFile,
                        UseShellExecute = false
                    }
                };
                p.Start();
            }
        }

        #endregion Private Helpers

        #region Ui Events

        private void OkClick(object sender, RoutedEventArgs e)
        {
            Directory.CreateDirectory(@"c:\temp\");
            MyProgressBar.Visibility = Visibility.Visible;

            _wc.DownloadFileAsync(new Uri(_url + _fileName), @"c:\temp\" + _fileName);
        }

        private void CancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            MyProgressBar.Visibility = Visibility.Hidden;
            _wc.CancelAsync();
            ProgressValue = 0;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (ForceUpdate)
                Application.Current.Shutdown();
        }

        #endregion Ui Events

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string info) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));

        #endregion
    }
}
