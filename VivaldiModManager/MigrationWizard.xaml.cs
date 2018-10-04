using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace VivaldiModManager
{
    public partial class MigrationWizard : MetroWindow
    {
        public bool StartMigration = false;
        public bool deletePrevious = false;
        public bool clearTarget = false;
        public ObservableCollection<MigrateVersions> SourceItems { get; set; }
        public ObservableCollection<MigrateVersions> DestinationItems { get; set; }

        public MigrationWizard(ObservableCollection<MigrateVersions> src, ObservableCollection<MigrateVersions> dst)
        {
            InitializeComponent();
            this.SourceItems = src;
            this.DestinationItems = dst;
            fromList.ItemsSource = this.SourceItems;
            toList.ItemsSource = this.DestinationItems;
        }

        private void migrateButton_Click(object sender, RoutedEventArgs e)
        {
            this.StartMigration = true;
            this.clearTarget = this.clearTargetCheck.IsChecked ?? false;
            this.deletePrevious = this.deletePreviousCheck.IsChecked ?? false;
            this.Close();
        }
    }
}
