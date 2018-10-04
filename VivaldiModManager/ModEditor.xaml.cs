using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using ICSharpCode.AvalonEdit;
using Path = System.IO.Path;

namespace VivaldiModManager
{
    public partial class ModEditor : MetroWindow
    {
        public string fileName { get; set; }
        public string fileExt { get; set; }
        public string filePath { get; set; }
        public string fileRealPath { get; set; }
        public bool IsNewFile { get; set; }

        public ModEditor(string path = "", string realPath = "")
        {
            InitializeComponent();
            this.filePath = path;
            this.fileRealPath = realPath;
            if (File.Exists(this.filePath))
            {
                modEditor.Load(this.filePath);
                this.fileName = Path.GetFileNameWithoutExtension(this.filePath);
                this.fileExt = Path.GetExtension(this.filePath);
                this.IsNewFile = false;
            }
            else
            {
                this.fileName = "";
                this.IsNewFile = true;
                this.fileExt = ".js";
            }
            this.fileNameBox.Text = this.fileName;
            if (this.fileExt == ".js") extBox.SelectedIndex = 0;
            else extBox.SelectedIndex = 1;
            modEditor.SyntaxHighlighting =
                    ICSharpCode.AvalonEdit.Highlighting.HighlightingManager
                    .Instance.GetDefinitionByExtension(this.fileExt);
            saveButton.IsEnabled = !this.IsNewFile;
            extBox.IsEnabled = this.IsNewFile;
            fileNameBox.IsEnabled = this.IsNewFile;
        }

        private void extBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string ext = (extBox.SelectedItem as Label).Content.ToString();
            modEditor.SyntaxHighlighting =
                    ICSharpCode.AvalonEdit.Highlighting.HighlightingManager
                    .Instance.GetDefinitionByExtension(
                        Path.GetExtension(ext));
            this.fileExt = ext;
        }

        private void fileNameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.fileName = fileNameBox.Text;
        }

        private bool SaveFile()
        {
            if (this.IsNewFile)
            {
                if (this.fileName == String.Empty)
                {
                    this.ShowMessageAsync(
                        MainWindow.GetLocStr("Warning"),
                        MainWindow.GetLocStr("EnterFileName"));
                    return false;
                }
                else
                {
                    string type = this.fileExt.Substring(1);
                    modEditor.Save(Path.Combine(this.filePath, type, this.fileName + this.fileExt));
                    modEditor.Save(Path.Combine(this.fileRealPath, type, this.fileName + this.fileExt));
                    return true;
                }
            }
            else
            {
                modEditor.Save(this.filePath);
                modEditor.Save(this.fileRealPath);
                return true;
            }
        }

        private void saveAndCloseButton_Click(object sender, RoutedEventArgs e)
        {
            if(this.SaveFile()) this.Close();
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            this.SaveFile();
        }
    }
}
