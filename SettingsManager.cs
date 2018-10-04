using System;
using System.IO;
using System.Xml;
using System.Windows;
using System.Xml.Serialization;

public class SettingsManager
{
    public static string settingsFile = "VivaldiModManager.xml";
    public SettingsStore Settings { set; get; }

    class SettingsStore
    {
        [XmlAttribute]
        public int MainWindowWidth { get; set; }
        [XmlAttribute]
        public int MainWindowHeight { get; set; }
        [XmlAttribute]
        public WindowState MainWindowState { get; set; }

        [XmlAttribute]
        public int MigrationWindowWidth { get; set; }
        [XmlAttribute]
        public int MigrationWindowHeight { get; set; }
        [XmlAttribute]
        public WindowState MigrationWindowState { get; set; }

        [XmlAttribute]
        public int EditorWidth { get; set; }
        [XmlAttribute]
        public int EditorHeight { get; set; }
        [XmlAttribute]
        public WindowState EditorState { get; set; }

        [XmlAttribute]
        public List<string> versionsDirectories { get; set; }
        [XmlAttribute]
        public string Culture { get; set; }
    }

    public SettingsManager(int MWWidth, int MWHeight, int MWState, int MGWidth, int MGHeight, int MGState, int EdWidth, int EdHeight, int EdState, List<string> verDirs, string culture)
	{
        if (File.Exists(settingsFile)) this.Settings = this.Load(settingsFile);
        else this.Settings = new SettingsStore()
        {
            MainWindowWidth = MWWidth,
            MainWindowHeight = MWHeight,
            MainWindowState = MWState,
            MigrationWindowWidth = MGWidth,
            MigrationWindowHeight = MGHeight,
            MigrationWindowState = MGState,
            EditorWidth = EdWidth,
            EditorHeight = EdHeight,
            EditorState = EdState,
            versionsDirectories = verDirs,
            Culture = culture
        };
	}

    public void Save(string FileName)
    {
        using (var writer = new System.IO.StreamWriter(FileName))
        {
            var serializer = new XmlSerializer(this.GetType());
            serializer.Serialize(writer, this);
            writer.Flush();
        }
    }

    public SettingsStore Load(string FileName)
    {
        using (var stream = System.IO.File.OpenRead(FileName))
        {
            var serializer = new XmlSerializer(typeof(SettingsStore));
            return serializer.Deserialize(stream) as SettingsStore;
        }
    }
}
