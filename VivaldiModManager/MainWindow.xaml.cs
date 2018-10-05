using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Globalization;
using WPFLocalizeExtension.Engine;

namespace VivaldiModManager
{
    public partial class MainWindow : MetroWindow
    {
        private static string homeUri = "https://gitlab.com/Neur0toxine/vivaldimodmanager";
        private static string vivaldiHomeUri = "https://vivaldi.com";
        private static string snapshotsUri = "https://vivaldi.com/blog/snapshots/";
        private static string helpUri = "https://help.vivaldi.com";
        private static string communityUri = "https://forum.vivaldi.net";
        private static string downloadModsUri = "https://forum.vivaldi.net/category/52/modifications";
        ModManager modman;
        SettingsManager setman;

        public static string GetLocStr(string name)
        {
            return LocalizeDictionary.Instance.GetLocalizedObject(name, null, LocalizeDictionary.Instance.Culture).ToString();
        }

        public MainWindow()
        {
            InitializeComponent();
            LocalizeDictionary.Instance.SetCurrentThreadCulture = true;
            setman = new SettingsManager(
                this.Width, this.Height, this.Left, this.Top, WindowState.Normal,
                0, 0, 0, 0, WindowState.Normal, 0, 0, 0, 0, WindowState.Normal, 
                new List<string>(), CultureInfo.InstalledUICulture.TwoLetterISOLanguageName);
            if (!setman.CleanStart)
            {
                this.WindowStartupLocation = WindowStartupLocation.Manual;
                this.Width = setman.Settings.MainWindowWidth;
                this.Height = setman.Settings.MainWindowHeight;
                this.Left = setman.Settings.MainWindowLeft;
                this.Top = setman.Settings.MainWindowTop;
                if (setman.Settings.MainWindowState != WindowState.Minimized)
                    this.WindowState = setman.Settings.MainWindowState;
            }

            #region Language Setting
            if (setman.Settings.Culture == "ru")
            {
                this.RussianCheck_Click(RussianCheck, null);
            }
            else this.EnglishCheck_Click(EnglishCheck, null);
            #endregion

            if (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length > 1)
            {
                MessageBox.Show(
                    GetLocStr("AlreadyRunning"),
                    GetLocStr("Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                Application.Current.Shutdown();
            }
            this.modman = new ModManager(this.setman);
            vivaldiVersionsList.ItemsSource = this.modman.vivaldiInstallations;
            var app = this.modman.selectedVersion;
            if(app != null)
            {
                vivaldiScripts.ItemsSource = app.installedScripts;
                vivaldiStyles.ItemsSource = app.installedStyles;
            }
            else
            {
                this.ShowMessageAsync(
                    GetLocStr("CantFindVivaldi"),
                    GetLocStr("CantFindVivaldiMessage"));
            }
        }

        private void ChangeLanguage(string lang)
        {
            LocalizeDictionary.Instance.SetCurrentThreadCulture = true;
            LocalizeDictionary.Instance.Culture = new CultureInfo(lang);
        }

        private void mainWindow_Drop(object sender, DragEventArgs e)
        {
            var app = this.modman.selectedVersion;
            if (app == null || !app.Enabled) return;
            if (!app.isModsEnabled) return;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string file in files)
                {
                    if(file.EndsWith(".css") || file.EndsWith(".js"))
                    {
                        app.installMod(file);
                    }
                }
                app.searchMods();
            }
        }

        private void addFromFile_button_Click(object sender, RoutedEventArgs e)
        {
            var app = this.modman.selectedVersion;
            if (app == null || !app.Enabled) return;
            if (!app.isModsEnabled) return;
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Styles and Scripts (*.css; *.js)|*.css;*.js|Styles (*.css)|*.css|Scripts (*.js)|*.js";
            ofd.FilterIndex = 0;
            ofd.Multiselect = true;
            if (ofd.ShowDialog() ?? false)
            {
                foreach (string file in ofd.FileNames)
                {
                    if (file.EndsWith(".css") || file.EndsWith(".js"))
                    {
                        app.installMod(file);
                    }
                }
                app.searchMods();
            }
        }

        private void enableAll_button_Click(object sender, RoutedEventArgs e)
        {
            var app = this.modman.selectedVersion;
            if (app == null || !app.Enabled) return;
            if (!app.isModsEnabled) return;
            foreach (VivaldiModManager.ModManager.vivaldiMod mm in app.installedScripts)
            {
                if (!mm.isEnabled) mm.ToggleMod(true);
            }
            foreach (VivaldiModManager.ModManager.vivaldiMod mm in app.installedStyles)
            {
                if (!mm.isEnabled) mm.ToggleMod(true);
            }
            this.reconnectUI();
        }

        private void disableAll_button_Click(object sender, RoutedEventArgs e)
        {
            var app = this.modman.selectedVersion;
            if (app == null || !app.Enabled) return;
            if (!app.isModsEnabled) return;
            foreach (VivaldiModManager.ModManager.vivaldiMod mm in app.installedScripts)
            {
                if (mm.isEnabled) mm.ToggleMod(false);
            }
            foreach (VivaldiModManager.ModManager.vivaldiMod mm in app.installedStyles)
            {
                if (mm.isEnabled) mm.ToggleMod(false);
            }
            this.reconnectUI();
        }

        public void callMigrationTool(object sender, RoutedEventArgs e)
        {
            var app = this.modman.selectedVersion;
            if (app == null || !app.Enabled) return;
            ObservableCollection<MigrateVersions> fromVersions = new ObservableCollection<MigrateVersions>();
            ObservableCollection<MigrateVersions> toVersions = new ObservableCollection<MigrateVersions>();
            List<string> dirs = new List<string>();

            foreach(var item in this.modman.vivaldiInstallations)
            {
                string allVersionsPath = Directory.GetParent(item.modsPersistentDir).FullName;
                if(Directory.Exists(allVersionsPath))
                    dirs.AddRange(Directory.GetDirectories(allVersionsPath));
            }
            dirs = dirs.Distinct().ToList();

            Regex regm = new Regex(@"(\d\.[\d\.]+)$");
            foreach(var item in this.modman.vivaldiInstallations)
            {
                if(item.Enabled && Directory.Exists(Directory.GetParent(item.modsPersistentDir).FullName))
                {
                    toVersions.Add(new MigrateVersions()
                    {
                        version = item.version,
                        modsDir = item.modsDir,
                        modsPersistentDir = item.modsPersistentDir,
                        Selected = app.version == item.version ? true : false
                    });
                }
            }
            foreach (string dir in dirs)
            {
                string ver = regm.Match(dir).Value;
                var sameInSidebar = this.modman.vivaldiInstallations.Where(f => f.modsPersistentDir == dir).FirstOrDefault();
                if(Directory.Exists(Directory.GetParent(dir).FullName))
                    fromVersions.Add(new MigrateVersions()
                    {
                        version = ver,
                        modsDir = sameInSidebar == null ? null : sameInSidebar.modsDir,
                        modsPersistentDir = dir,
                        Selected = false
                    });
            }
            MigrationWizard mwiz = new MigrationWizard(fromVersions, toVersions);
            if (!this.setman.CleanStart)
            {
                if (this.setman.Settings.MigrationWindowWidth != 0) mwiz.Width = this.setman.Settings.MigrationWindowWidth;
                if (this.setman.Settings.MigrationWindowHeight != 0) mwiz.Height = this.setman.Settings.MigrationWindowHeight;
                if (this.setman.Settings.MigrationWindowLeft != 0) mwiz.Left = this.setman.Settings.MigrationWindowLeft;
                if (this.setman.Settings.MigrationWindowTop != 0) mwiz.Top = this.setman.Settings.MigrationWindowTop;
                if (this.setman.Settings.MigrationWindowState != WindowState.Minimized) mwiz.WindowState = this.setman.Settings.MigrationWindowState;
                mwiz.WindowStartupLocation = WindowStartupLocation.Manual;
            }
            mwiz.Owner = this;
            mwiz.ShowDialog();
            this.setman.Settings.MigrationWindowWidth = mwiz.Width;
            this.setman.Settings.MigrationWindowHeight = mwiz.Height;
            this.setman.Settings.MigrationWindowLeft = mwiz.Left;
            this.setman.Settings.MigrationWindowTop = mwiz.Top;
            this.setman.Settings.MigrationWindowState = mwiz.WindowState;

            var fromVersion = fromVersions.Where(f => f.Selected).FirstOrDefault();
            var toVersion = toVersions.Where(f => f.Selected).FirstOrDefault();
            if (mwiz.StartMigration && (fromVersion == null || toVersion == null))
            {
                this.ShowMessageAsync(
                    GetLocStr("CantStartMigration"),
                    GetLocStr("CantStartMigrationNoSelection"));
            }
            else
            {
                if (mwiz.StartMigration)
                {
                    if(fromVersion.modsPersistentDir == toVersion.modsPersistentDir)
                    {
                        this.ShowMessageAsync(
                            GetLocStr("CantStartMigration"),
                            GetLocStr("CantStartMigrationSameVersions"));
                    }
                    else
                    {
                        var toApp = this.modman.vivaldiInstallations.Where(f => f.version == toVersion.version).First();
                        toApp.migrateFrom(fromVersion, mwiz.deletePrevious, mwiz.clearTarget);
                        this.modman.selectVivaldiVersion(toApp.modsPersistentDir);
                        this.reconnectUI(true);
                    }
                }
            }

            mwiz = null;
            fromVersions.Clear();
            toVersions.Clear();
            fromVersions = null;
            toVersions = null;
            regm = null;
        }

        private void reconnectUI(bool reloadVersions = false)
        {
            var app = this.modman.selectedVersion;
            if (app == null) return;
            vivaldiScripts.ItemsSource = null;
            vivaldiStyles.ItemsSource = null;
            app.searchMods();
            vivaldiScripts.ItemsSource = app.installedScripts;
            vivaldiStyles.ItemsSource = app.installedStyles;
            if(reloadVersions)
            {
                vivaldiVersionsList.ItemsSource = null;
                vivaldiVersionsList.ItemsSource = this.modman.vivaldiInstallations;
            }
        }

        private void backup_Button_Click(object sender, RoutedEventArgs e)
        {
            var app = this.modman.selectedVersion;
            if (app == null || !app.Enabled) return;
            if (!app.isModsEnabled) return;
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.AddExtension = true;
            sfd.DefaultExt = ".zip";
            sfd.Filter = "Zip Archive|*.zip";
            if (sfd.ShowDialog() ?? false)
            {
                ZipFile.CreateFromDirectory(app.modsPersistentDir, sfd.FileName);
            }
        }

        private void DeleteDirectory(string path)
        {
            foreach (string directory in Directory.GetDirectories(path))
            {
                DeleteDirectory(directory);
            }

            try
            {
                Directory.Delete(path, true);
            }
            catch (IOException)
            {
                Directory.Delete(path, true);
            }
            catch (UnauthorizedAccessException)
            {
                Directory.Delete(path, true);
            }
        }

        private void restore_Button_Click(object sender, RoutedEventArgs e)
        {
            var app = this.modman.selectedVersion;
            if (app == null || !app.Enabled) return;
            if (!app.isModsEnabled) return;
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Zip File|*.zip";
            if (ofd.ShowDialog() ?? false)
            {
                this.DeleteDirectory(app.modsDir);
                this.DeleteDirectory(app.modsPersistentDir);
                Directory.CreateDirectory(app.modsDir);
                Directory.CreateDirectory(app.modsPersistentDir);
                ZipFile.ExtractToDirectory(ofd.FileName, app.modsDir);
                ZipFile.ExtractToDirectory(ofd.FileName, app.modsPersistentDir);
                this.reconnectUI();
            }
        }

        private void changeVersion_Click(object sender, RoutedEventArgs e)
        {
            if((e.Source as Button) != null)
            {
                var stackPanel = ((StackPanel)((Button)e.Source).Content);
                foreach (object child in stackPanel.Children)
                {
                    if (child is FrameworkElement)
                    {
                        if ((child as FrameworkElement) is Label)
                        {
                            string version = Convert.ToString((child as Label).Content);
                            this.modman.selectVivaldiVersion(version);
                            this.reconnectUI(true);
                            break;
                        }
                    }
                }
            }
        }

        private void removeVersion_Button_Click(object sender, RoutedEventArgs e)
        {
            if (this.modman.vivaldiInstallations.Count > 1)
            {
                this.setman.Settings.versionsDirectories.Remove(
                    System.IO.Path.Combine(this.modman.selectedVersion.installPath, this.modman.selectedVersion.version));
                this.modman.vivaldiInstallations.Remove(this.modman.selectedVersion);
                this.modman.selectVivaldiVersion(this.modman.vivaldiInstallations.First().modsPersistentDir);
                this.reconnectUI(true);
            }
        }

        private void addVersion_Button_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            CommonFileDialogResult result = dialog.ShowDialog();
            if (result.ToString() != "Cancel")
            {
                if(this.modman.addVivaldiVersion(dialog.FileName, true))
                {
                    this.setman.Settings.versionsDirectories.Add(dialog.FileName);
                }
                if (this.modman.vivaldiInstallations.Count() == 1)
                {
                    this.modman.selectVivaldiVersion(this.modman.vivaldiInstallations.First().modsPersistentDir);
                }
                this.reconnectUI(true);
            }
        }

        private void about_Click(object sender, RoutedEventArgs e)
        {
            this.ShowMessageAsync("Vivaldi Mod Manager",
                GetLocStr("AboutMessage") +
                "\nIcon made by https://www.flaticon.com/authors/webalys-freebies from www.flaticon.com.");
        }

        private void exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void EnglishCheck_Click(object sender, RoutedEventArgs e)
        {
            this.EnglishCheck.IsChecked = true;
            this.RussianCheck.IsChecked = false;
            this.ChangeLanguage("en");
            this.setman.Settings.Culture = "en";
        }

        private void RussianCheck_Click(object sender, RoutedEventArgs e)
        {
            this.EnglishCheck.IsChecked = false;
            this.RussianCheck.IsChecked = true;
            this.ChangeLanguage("ru");
            this.setman.Settings.Culture = "ru";
        }

        private void goDownloadMods_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(downloadModsUri);
        }

        private void goToCommunity_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(communityUri);
        }

        private void goToHelp_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(helpUri);
        }

        private void goToSnapshots_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(snapshotsUri);
        }

        private void goToVivaldiHome_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(vivaldiHomeUri);
        }

        private void goToHome_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(homeUri);
        }

        private void newMod_Click(object sender, RoutedEventArgs e)
        {
            var app = this.modman.selectedVersion;
            if (app == null) return;
            ModEditor modEd = new ModEditor(app.modsDir, app.modsPersistentDir);
            if (!this.setman.CleanStart)
            {
                if (this.setman.Settings.EditorWidth != 0) modEd.Width = this.setman.Settings.EditorWidth;
                if (this.setman.Settings.EditorHeight != 0) modEd.Height = this.setman.Settings.EditorHeight;
                if (this.setman.Settings.EditorLeft != 0) modEd.Left = this.setman.Settings.EditorLeft;
                if (this.setman.Settings.EditorTop != 0) modEd.Top = this.setman.Settings.EditorTop;
                if (this.setman.Settings.EditorState != WindowState.Minimized)
                    modEd.WindowState = this.setman.Settings.EditorState;
                modEd.WindowStartupLocation = WindowStartupLocation.Manual;
            }
            modEd.ShowDialog();
            this.setman.Settings.EditorWidth = modEd.Width;
            this.setman.Settings.EditorHeight = modEd.Height;
            this.setman.Settings.EditorLeft = modEd.Left;
            this.setman.Settings.EditorTop = modEd.Top;
            this.setman.Settings.EditorState = modEd.WindowState;
            app.searchMods();
            this.reconnectUI();
            modEd = null;
        }

        private void mainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            setman.Settings.MainWindowWidth = this.Width;
            setman.Settings.MainWindowHeight = this.Height;
            setman.Settings.MainWindowLeft = this.Left;
            setman.Settings.MainWindowTop = this.Top;
            setman.Settings.MainWindowState = this.WindowState;
            setman.SaveSettings();
        }
    }

    public static class ContextMenuLeftClickBehavior
    {
        public static bool GetIsLeftClickEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsLeftClickEnabledProperty);
        }

        public static void SetIsLeftClickEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsLeftClickEnabledProperty, value);
        }

        public static readonly DependencyProperty IsLeftClickEnabledProperty = DependencyProperty.RegisterAttached(
            "IsLeftClickEnabled",
            typeof(bool),
            typeof(ContextMenuLeftClickBehavior),
            new UIPropertyMetadata(false, OnIsLeftClickEnabledChanged));

        private static void OnIsLeftClickEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var uiElement = sender as UIElement;

            if (uiElement != null)
            {
                bool IsEnabled = e.NewValue is bool && (bool)e.NewValue;

                if (IsEnabled)
                {
                    if (uiElement is ButtonBase)
                        ((ButtonBase)uiElement).Click += OnMouseLeftButtonUp;
                    else
                        uiElement.MouseLeftButtonUp += OnMouseLeftButtonUp;
                }
                else
                {
                    if (uiElement is ButtonBase)
                        ((ButtonBase)uiElement).Click -= OnMouseLeftButtonUp;
                    else
                        uiElement.MouseLeftButtonUp -= OnMouseLeftButtonUp;
                }
            }
        }

        private static void OnMouseLeftButtonUp(object sender, RoutedEventArgs e)
        {
            var fe = sender as FrameworkElement;
            if (fe != null)
            {
                // if we use binding in our context menu, then it's DataContext won't be set when we show the menu on left click
                // (it seems setting DataContext for ContextMenu is hardcoded in WPF when user right clicks on a control, although I'm not sure)
                // so we have to set up ContextMenu.DataContext manually here
                if (fe.ContextMenu.DataContext == null)
                {
                    fe.ContextMenu.SetBinding(FrameworkElement.DataContextProperty, new Binding { Source = fe.DataContext });
                }
                fe.ContextMenu.IsOpen = true;
            }
        }

    }
}
