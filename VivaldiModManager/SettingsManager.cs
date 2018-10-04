using System;
using System.IO;
using System.Xml;
using System.Windows;
using System.Xml.Serialization;
using System.Collections.Generic;

public class SettingsManager
{
    public static string settingsFile = "VivaldiModManager.xml";
    public SettingsStore Settings { set; get; }
    private bool _cleanStart;
    public bool CleanStart {
        get
        {
            return this._cleanStart;
        }
    }

    public class SettingsStore
    {
        [XmlAttribute]
        public double MainWindowWidth { get; set; }
        [XmlAttribute]
        public double MainWindowHeight { get; set; }
        [XmlAttribute]
        public double MainWindowLeft { get; set; }
        [XmlAttribute]
        public double MainWindowTop { get; set; }
        [XmlAttribute]
        public WindowState MainWindowState { get; set; }

        [XmlAttribute]
        public double MigrationWindowWidth { get; set; }
        [XmlAttribute]
        public double MigrationWindowHeight { get; set; }
        [XmlAttribute]
        public double MigrationWindowLeft { get; set; }
        [XmlAttribute]
        public double MigrationWindowTop { get; set; }
        [XmlAttribute]
        public WindowState MigrationWindowState { get; set; }

        [XmlAttribute]
        public double EditorWidth { get; set; }
        [XmlAttribute]
        public double EditorHeight { get; set; }
        [XmlAttribute]
        public double EditorLeft { get; set; }
        [XmlAttribute]
        public double EditorTop { get; set; }
        [XmlAttribute]
        public WindowState EditorState { get; set; }

        [XmlArray("versionsDirectories")]
        public List<string> versionsDirectories { get; set; }
        [XmlAttribute]
        public string Culture { get; set; }
    }

    public SettingsManager(double MWWidth, double MWHeight, double MWLeft, double MWTop, WindowState MWState,
        double MGWidth, double MGHeight, double MGLeft, double MGTop, WindowState MGState,
        double EdWidth, double EdHeight, double EdLeft, double EdTop, WindowState EdState,
        List<string> verDirs, string culture)
	{
        if (File.Exists(settingsFile))
        {
            this.Settings = this.Load(settingsFile);
            this._cleanStart = false;
        }
        else
        {
            this.Settings = new SettingsStore()
            {
                MainWindowWidth = MWWidth,
                MainWindowHeight = MWHeight,
                MainWindowLeft = MWLeft,
                MainWindowTop = MWTop,
                MainWindowState = MWState,
                MigrationWindowWidth = MGWidth,
                MigrationWindowHeight = MGHeight,
                MigrationWindowLeft = MGLeft,
                MigrationWindowTop = MGTop,
                MigrationWindowState = MGState,
                EditorWidth = EdWidth,
                EditorHeight = EdHeight,
                EditorLeft = EdLeft,
                EditorTop = EdTop,
                EditorState = EdState,
                versionsDirectories = verDirs,
                Culture = culture
            };
            this._cleanStart = true;
        }
	}

    public bool SaveSettings()
    {
        try
        {
            this.Save(settingsFile);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void Save(string FileName)
    {
        using (var writer = new System.IO.StreamWriter(FileName))
        {
            var serializer = new XmlSerializer(this.Settings.GetType());
            serializer.Serialize(writer, this.Settings);
            writer.Flush();
        }
    }

    private SettingsStore Load(string FileName)
    {
        using (var stream = System.IO.File.OpenRead(FileName))
        {
            var serializer = new XmlSerializer(typeof(SettingsStore));
            return serializer.Deserialize(stream) as SettingsStore;
        }
    }
}
