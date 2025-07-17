using Microsoft.Win32;
using OblivionRemasterDownpatcher.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OblivionRemasterDownpatcher.Pages
{
    /// <summary>
    /// Interaction logic for DashboardPage.xaml
    /// </summary>
    public partial class DashboardPage
    {
        public string? GamePath { get; set; }
        
        public string? Username { get; set; }

        public OblivionVersion? CurrentVersion { 
            get { 
                return GamePath != null ? OblivionVersionManager.FromInstall(GamePath) : null; 
            } 
        }

        public List<OblivionVersion> Versions { get; } = OblivionVersionManager.AllVersions();
        public OblivionVersion SelectedVersion { get; set; } = OblivionVersionManager.AllVersions().First();

        private Process? currentDepotDownloader = null;
        public Action<Process>? downloaderWatcher = null;

        public DiskUsageMonitor? UsageMonitor { get; set; }


        public DashboardPage()
        {
            InitializeComponent();
            
            downloaderWatcher = (proc) =>
            {
                currentDepotDownloader = proc;
                UsageMonitor = new DiskUsageMonitor(currentDepotDownloader);
                NetworkMonitor.DiskUsageMonitor = UsageMonitor;
                
                RefreshDataContext();
            };
        }

        public void RefreshDataContext()
        {
            DataContext = null;
            DataContext = this;
        }

        private void ManualSelect_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog openFolderDialog = new() { 
                Multiselect = false, 
                InitialDirectory = GamePath ?? GameHunter.FindEXE(OblivionVersionManager.APP_ID),
            };

            var success = openFolderDialog.ShowDialog();
            if (success != true) return;
            if (openFolderDialog.FolderNames.Length <= 0) return;

            GamePath = openFolderDialog.FolderNames[0];
            RefreshDataContext();
        }

        private void AutoSelect_Click(object sender, RoutedEventArgs e)
        {
            GamePath = GameHunter.FindEXE(OblivionVersionManager.APP_ID);
            RefreshDataContext();
        }

        private void PasswordInfo_Click(object sender, RoutedEventArgs e)
        {
            var uiMessageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = "Info",
                Content = "This project uses SteamRE/DepotDownloader to download Oblivion updates; If you have used this software before, you do not need to include your password.",
            };

            uiMessageBox.ShowDialogAsync();
        }

        private void VerifyGame_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckRequiredFields()) return;

            var password = PasswordBox.Password!;

            var wrapper = new DepotDownloaderWrapper(DepotDownloaderWrapper.GetDefaultExecutable());
            wrapper.VerifyVersion(GamePath!, Username!, password, downloaderWatcher);
        }

        private void DownpatchGame_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckRequiredFields()) return;

            string? password = PasswordBox.Password;
            if (password.Length <= 0) password = null;

            var wrapper = new DepotDownloaderWrapper(DepotDownloaderWrapper.GetDefaultExecutable());
            wrapper.ChangeVersion(GamePath!, Username!, password, SelectedVersion, downloaderWatcher);
        }

        private bool CheckRequiredFields()
        {
            if (GamePath == null)
            {
                var uiMessageBox = new Wpf.Ui.Controls.MessageBox { Title = "Info", Content = "Game path is required" };
                uiMessageBox.ShowDialogAsync();
                return false;
            }

            var password = PasswordBox.Password;

            if (Username == null)
            {
                var uiMessageBox = new Wpf.Ui.Controls.MessageBox { Title = "Info", Content = "Steam username/password is required" };
                uiMessageBox.ShowDialogAsync();
                return false;
            }

            if (GamePath == null || Username == null) return false;

            return true;
        }
    }
}
