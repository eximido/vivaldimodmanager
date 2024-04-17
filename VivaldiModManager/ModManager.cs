using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Collections.ObjectModel;

namespace VivaldiModManager
{
    public class ModManager
    {
        public SettingsManager setman;
        public Action reconnectUI { get; set; }
        public ObservableCollection<VivaldiPackage> vivaldiInstallations = new ObservableCollection<VivaldiPackage>();
        public VivaldiPackage selectedVersion
        {
            get
            {
                try
                {
                    return this.vivaldiInstallations.Where(f => f.isSelected).Single();
                }
                catch
                {
                    return null;
                }
            }
        }

        public class vivaldiMod
        {
            public string fileName { get; set; }
            public string filePath { get; set; }
            public string fileRealpath { get; set; }
            public bool isEnabled { get; set; }
            public Action ModRemoved;
            public SettingsManager SetMan;

            public void ToggleMod(bool parameter)
            {
                if (!parameter)
                {
                    string newPath = this.filePath + ".disabled";
                    string newRealpath = this.fileRealpath + ".disabled";
                    if (!File.Exists(this.filePath) || !File.Exists(this.fileRealpath)) return;
                    File.Move(this.filePath, newPath);
                    File.Move(this.fileRealpath, newRealpath);
                    this.filePath = newPath;
                    this.fileRealpath = newRealpath;
                    this.isEnabled = false;
                }
                else
                {
                    string newPath = this.filePath.Replace(".disabled", "");
                    string newRealpath = this.fileRealpath.Replace(".disabled", "");
                    if (!File.Exists(this.filePath) || !File.Exists(this.fileRealpath)) return;
                    File.Move(this.filePath, newPath);
                    File.Move(this.fileRealpath, newRealpath);
                    this.filePath = newPath;
                    this.fileRealpath = newRealpath;
                    this.isEnabled = false;
                }
            }

            public void EditMod()
            {
                ModEditor modEd = new ModEditor(this.filePath, this.fileRealpath);
                if (!this.SetMan.CleanStart)
                {
                    if (this.SetMan.Settings.EditorWidth != 0) modEd.Width = this.SetMan.Settings.EditorWidth;
                    if (this.SetMan.Settings.EditorHeight != 0) modEd.Height = this.SetMan.Settings.EditorHeight;
                    if (this.SetMan.Settings.EditorLeft != 0) modEd.Left = this.SetMan.Settings.EditorLeft;
                    if (this.SetMan.Settings.EditorTop != 0) modEd.Top = this.SetMan.Settings.EditorTop;
                    if (this.SetMan.Settings.EditorState != WindowState.Minimized)
                        modEd.WindowState = this.SetMan.Settings.EditorState;
                    modEd.WindowStartupLocation = WindowStartupLocation.Manual;
                }
                modEd.SetMan = this.SetMan;
                modEd.Show();
            }

            public void ExtractMod()
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.DefaultExt = Path.GetExtension(this.fileName);
                sfd.FileName = this.fileName;
                if(sfd.ShowDialog() ?? false)
                {
                    try
                    {
                        if(File.Exists(this.filePath))
                            File.Copy(this.filePath, sfd.FileName, true);
                    }
                    catch { }
                }
            }

            public void RemoveMod()
            {
                File.Delete(this.filePath);
                File.Delete(this.fileRealpath);
                this.ModRemoved();
            }

            RelayCommand _toggleModCommand; public ICommand ToggleModCommand
            {
                get
                {
                    if (_toggleModCommand == null)
                    {
                        _toggleModCommand = new RelayCommand(param => this.ToggleMod((bool)param),
                            param => true);
                    }
                    return _toggleModCommand;
                }
            }

            RelayCommand _editModCommand; public ICommand EditModCommand
            {
                get
                {
                    if (_editModCommand == null)
                    {
                        _editModCommand = new RelayCommand(param => this.EditMod(),
                            param => true);
                    }
                    return _editModCommand;
                }
            }

            RelayCommand _extractModCommand; public ICommand ExtractModCommand
            {
                get
                {
                    if (_extractModCommand == null)
                    {
                        _extractModCommand = new RelayCommand(param => this.ExtractMod(),
                            param => true);
                    }
                    return _extractModCommand;
                }
            }

            RelayCommand _removeModCommand; public ICommand RemoveModCommand
            {
                get
                {
                    if (_removeModCommand == null)
                    {
                        _removeModCommand = new RelayCommand(param => this.RemoveMod(),
                            param => true);
                    }
                    return _removeModCommand;
                }
            }
        }

        public class VivaldiPackage
        {
            public string version { get; set; }
            public string installPath { get; set; }
            public string modsPersistentDir { get; set; }
            public string modsDir { get; set; }
            public string browserHtml { get; set; }
            public string modLoader { get; set; }
            public bool isModsEnabled { get; set; }
            public bool isSelected { get; set; }
            public bool requiresAdminRights { get; set; }
            public static bool adminRightsRequested = false;
            public bool Enabled { get; set; }
            public ObservableCollection<vivaldiMod> installedStyles { get; set; }
            public ObservableCollection<vivaldiMod> installedScripts { get; set; }
            public SettingsManager setman;
            public Action ThisVersionProbablyDeleted { get; set; }

            public VivaldiPackage(string version, string installPath, SettingsManager SetManager, bool isSelected)
            {
                this.setman = SetManager;
                this.installedStyles = new ObservableCollection<vivaldiMod>();
                this.installedScripts = new ObservableCollection<vivaldiMod>();
                this.version = version;
                this.installPath = installPath;
                this.modsPersistentDir = Path.Combine(installPath, ".vivaldimods", version);
                this.modsDir = Path.Combine(installPath, version, "resources", "vivaldi", "user_mods");
                this.browserHtml = Path.Combine(installPath, version, "resources", "vivaldi", "window.html");
                this.modLoader = Path.Combine(installPath, version, "resources", "vivaldi", "injectMods.js");
                this.isSelected = isSelected;
                this.requiresAdminRights = this.installPath.Contains(":\\Program Files");

                if (this.requiresAdminRights && !(new WindowsPrincipal(WindowsIdentity.GetCurrent())).IsInRole(WindowsBuiltInRole.Administrator))
                {
                    // try to request admin rights if needed, but do it only once
                    if (!adminRightsRequested) {
                        adminRightsRequested = true;
                        // try to restart the program as admin
                        var exeName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                        ProcessStartInfo startInfo = new ProcessStartInfo(exeName);
                        startInfo.Verb = "runas";
                        startInfo.UseShellExecute = true;
                        try {
                            System.Diagnostics.Process.Start(startInfo);
                            // exit from the old app if restart was successful
                            Application.Current.Shutdown();
                            return;
                        } catch (Exception) {
                            // if user hasn't provided admin rights then proceed like nothing happened
                        }
                    }
                    // if we don't have admin rights and were unable to restart the app as admin, just mark this installation as disabled
                    this.Enabled = false;
                } else
                {
                    this.Enabled = true;
                }

                if (!File.Exists(this.browserHtml)) return;

                this.isModsEnabled = File.ReadAllText(this.browserHtml).Contains("<script src=\"injectMods.js\"></script>");
                this.initModsEnabled();
            }

            public void Copy(string sourceDirectory, string targetDirectory)
            {
                DirectoryInfo diSource = new DirectoryInfo(sourceDirectory);
                DirectoryInfo diTarget = new DirectoryInfo(targetDirectory);
                CopyAll(diSource, diTarget);
            }

            public void CopyAll(DirectoryInfo source, DirectoryInfo target)
            {
                Directory.CreateDirectory(target.FullName);
                foreach (FileInfo fi in source.GetFiles())
                {
                    fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
                }

                foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
                {
                    DirectoryInfo nextTargetSubDir =
                        target.CreateSubdirectory(diSourceSubDir.Name);
                    CopyAll(diSourceSubDir, nextTargetSubDir);
                }
            }

            public void initModsEnabled()
            {
                if (this.isModsEnabled && this.Enabled)
                {
                    if (!Directory.Exists(this.modsDir))
                    {
                        if (this.Enabled)
                        {
                            if (Directory.Exists(this.modsPersistentDir))
                            {
                                Directory.CreateDirectory(this.modsDir);
                                this.Copy(this.modsPersistentDir, this.modsDir);
                            }
                            else
                            {
                                Directory.CreateDirectory(this.modsDir);
                                Directory.CreateDirectory(this.modsDir + "\\css");
                                Directory.CreateDirectory(this.modsDir + "\\js");
                            }
                        }
                        else return;
                    }
                    if (!Directory.Exists(this.modsPersistentDir))
                    {
                        if (this.Enabled)
                        {
                            Directory.CreateDirectory(this.modsPersistentDir);
                            Directory.CreateDirectory(this.modsPersistentDir + "\\css");
                            Directory.CreateDirectory(this.modsPersistentDir + "\\js");
                        }
                        else return;
                    }
                    if (!File.Exists(this.modLoader))
                    {
                        if (this.Enabled) File.WriteAllText(this.modLoader, ModLoader.injectMods);
                        else
                        {
                            this.isModsEnabled = false;
                            return;
                        }
                    }
                    this.searchMods();
                }
            }

            public void installMod(string mod, bool refresh = false)
            {
                if (mod.EndsWith(".css"))
                {
                    File.Copy(mod, Path.Combine(this.modsPersistentDir + "\\css\\", Path.GetFileName(mod)), true);
                    File.Copy(mod, Path.Combine(this.modsDir + "\\css\\", Path.GetFileName(mod)), true);
                    if (refresh) this.searchMods();
                }
                if (mod.EndsWith(".js"))
                {
                    File.Copy(mod, Path.Combine(this.modsPersistentDir + "\\js\\", Path.GetFileName(mod)), true);
                    File.Copy(mod, Path.Combine(this.modsDir + "\\js\\", Path.GetFileName(mod)), true);
                    if (refresh) this.searchMods();
                }
            }

            public void searchMods()
            {
                this.installedStyles.Clear();
                this.installedScripts.Clear();

                if (!this.Enabled || !this.isModsEnabled) return;

                var css = Directory.EnumerateFiles(this.modsPersistentDir + "\\css");
                var js = Directory.EnumerateFiles(this.modsPersistentDir + "\\js");
                foreach(string item in css)
                {
                    var vMod = new vivaldiMod()
                    {
                        fileName = Path.GetFileName(item),
                        filePath = Path.Combine(this.modsPersistentDir + "\\css\\", Path.GetFileName(item)),
                        fileRealpath = Path.Combine(this.modsDir + "\\css\\", Path.GetFileName(item)),
                        isEnabled = true,
                        SetMan = this.setman
                    };
                    vMod.ModRemoved = this.searchMods;
                    if (item.EndsWith(".disabled"))
                    {
                        vMod.isEnabled = false;
                    }
                    this.installedStyles.Add(vMod);
                }
                foreach (string item in js)
                {
                    var vMod = new vivaldiMod()
                    {
                        fileName = Path.GetFileName(item),
                        filePath = Path.Combine(this.modsPersistentDir + "\\js\\", Path.GetFileName(item)),
                        fileRealpath = Path.Combine(this.modsDir + "\\js\\", Path.GetFileName(item)),
                        isEnabled = true,
                        SetMan = this.setman
                    };
                    vMod.ModRemoved = this.searchMods;
                    if (item.EndsWith(".disabled"))
                    {
                        vMod.isEnabled = false;
                    }
                    this.installedScripts.Add(vMod);
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

            public void migrateFrom(MigrateVersions version, bool deletePrevious, bool clearTarget)
            {
                if (!File.Exists(this.browserHtml))
                {
                    this.ThisVersionProbablyDeleted();
                    return;
                }

                if (this.Enabled)
                {
                    if (!this.isModsEnabled) this.ToggleMods(true);
                    this.initModsEnabled();
                    if (clearTarget)
                    {
                        if(Directory.Exists(this.modsPersistentDir))
                        {
                            this.DeleteDirectory(this.modsPersistentDir);
                            Directory.CreateDirectory(this.modsPersistentDir);
                        }
                        if (Directory.Exists(this.modsDir))
                        {
                            this.DeleteDirectory(this.modsDir);
                            Directory.CreateDirectory(this.modsDir);
                        }
                    }
                    this.Copy(version.modsPersistentDir, this.modsPersistentDir);
                    this.Copy(modsPersistentDir, this.modsDir);
                    if (deletePrevious)
                    {
                        Directory.Delete(version.modsPersistentDir, true);
                        if (Directory.Exists(version.modsDir)) Directory.Delete(version.modsDir, true);
                    }
                    this.searchMods();
                }
            }

            public void ToggleMods(bool parameter)
            {
                if (!File.Exists(this.browserHtml))
                {
                    this.ThisVersionProbablyDeleted();
                    return;
                }

                if (!parameter)
                {
                    string browserHtmlText = File.ReadAllText(this.browserHtml);
                    Regex matchModLoader = new Regex(@"(\s+)?\<script\ssrc\=\""injectMods.js\""\>\<\/script\>", RegexOptions.Multiline);
                    browserHtmlText = matchModLoader.Replace(browserHtmlText, "");
                    File.WriteAllText(this.browserHtml, browserHtmlText);
                    this.isModsEnabled = false;
                    this.installedScripts.Clear();
                    this.installedStyles.Clear();
                }
                else
                {
                    string browserHtmlText = File.ReadAllText(this.browserHtml);
                    Regex indentRegex = new Regex(@"(\s+)\<title\>", RegexOptions.Multiline);
                    string indent = indentRegex.Match(browserHtmlText).Groups[1].Value;
                    browserHtmlText = browserHtmlText.Replace("<body>",
                        "<body>" + indent + "<script src=\"injectMods.js\"></script>");
                    File.WriteAllText(this.browserHtml, browserHtmlText);
                    this.initModsEnabled();
                    this.isModsEnabled = true;
                }
            }

            RelayCommand _toggleModsCommand; public ICommand ToggleModsCommand
            {
                get
                {
                    if (_toggleModsCommand == null)
                    {
                        _toggleModsCommand = new RelayCommand(param => this.ToggleMods((bool)param),
                            param => this.Enabled);
                    }
                    return _toggleModsCommand;
                }
            }
        }

        public ModManager(SettingsManager SetManager)
        {
            this.setman = SetManager;
            this.searchVivaldiInstallations();
            List<string> versions = new List<string>(this.setman.Settings.versionsDirectories);
            foreach (string version in versions)
            {
                if (!this.addVivaldiVersion(version, true))
                {
                    this.setman.Settings.versionsDirectories.Remove(version);
                }
            }
            var firstInList = this.vivaldiInstallations.FirstOrDefault();
            if (firstInList != null) this.selectVivaldiVersion(firstInList.modsPersistentDir);
        }

        public void vivaldiPackageWasDeleted(string deletedPersistentPath)
        {
            MessageBox.Show(MainWindow.GetLocStr("VersionWasDeleted"),
                MainWindow.GetLocStr("Warning"),
                MessageBoxButton.OK, MessageBoxImage.Warning);
            var toDelete = this.vivaldiInstallations.Where(f => f.modsPersistentDir == deletedPersistentPath).First();
            this.vivaldiInstallations.Remove(toDelete);
            var firstInList = this.vivaldiInstallations.FirstOrDefault();
            if (firstInList != null) this.selectVivaldiVersion(firstInList.modsPersistentDir);
            this.reconnectUI();
        }

        public void searchVivaldiInstallations()
        {
            this.vivaldiInstallations.Clear();
            string installPathLocal = (string)Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\vivaldi.exe", "Path", null);
            string installPathGlobal = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\vivaldi.exe", "Path", null);

            if (installPathLocal != null) this.addVivaldiVersion(installPathLocal);
            if (installPathGlobal != null) this.addVivaldiVersion(installPathGlobal);

            var firstInList = this.vivaldiInstallations.FirstOrDefault();
            if (firstInList != null) this.selectVivaldiVersion(firstInList.modsPersistentDir);
        }

        public bool addVivaldiVersion(string path, bool versionDirectory = false)
        {
            if(Directory.Exists(path))
            {
                Regex regm = new Regex(@"(\d\.[\d\.]+)$");
                if (versionDirectory)
                {
                    if (File.Exists(path + "\\vivaldi.dll"))
                    {
                        string version = regm.Match(path).Value;
                        string installPath = path.Replace(version, "");
                        if (this.vivaldiInstallations.Where(f => (f.installPath.StartsWith(installPath) || f.modsDir.StartsWith(path)) && f.version == version).Count() == 0)
                        {
                            var ver = new VivaldiPackage(version, installPath, this.setman, false);
                            ver.ThisVersionProbablyDeleted += () => this.vivaldiPackageWasDeleted(ver.modsPersistentDir);
                            this.vivaldiInstallations.Add(ver);
                            return true;
                        }
                        else return false;
                    }
                    else return false;
                }
                else
                {
                    bool success = false;
                    Regex reg = new Regex(@"Application\\(\d\.[\d\.]+)$");
                    var versionDirectories = Directory.EnumerateDirectories(path, "*.*", SearchOption.AllDirectories)
                        .Where(f => reg.IsMatch(f)).Distinct().ToList();
                    foreach (string vDirectory in versionDirectories)
                    {
                        if (Directory.Exists(vDirectory))
                        {
                            string version = regm.Match(vDirectory).Value;
                            var ver = new VivaldiPackage(version, path, this.setman, !(this.vivaldiInstallations.Count() > 0));
                            ver.ThisVersionProbablyDeleted += () => this.vivaldiPackageWasDeleted(ver.modsPersistentDir);
                            this.vivaldiInstallations.Add(ver);
                            success = true;
                        }
                    }
                    return success;
                }
            }
            return false;
        }

        public void selectVivaldiVersion(string modsPersistentDir)
        {
            if (this.selectedVersion != null) this.selectedVersion.isSelected = false;
            var ver = this.vivaldiInstallations.Where(f => f.modsPersistentDir == modsPersistentDir).FirstOrDefault();
            if (ver != null) ver.isSelected = true;
        }
    }
}
