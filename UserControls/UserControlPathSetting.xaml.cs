using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
using Ookii.Dialogs.Wpf;

namespace DayZServerControllerUI.UserControls
{
    /// <summary>
    /// Interaction logic for UserControlPathSetting.xaml
    /// </summary>
    public partial class UserControlPathSetting : UserControl, INotifyPropertyChanged
    {
        private string _labelText = String.Empty;
        private string _selectedPath = String.Empty;
        private bool _selectionValid = false;

        public string LabelText
        {
            get => _labelText;
            set
            {
                if (String.IsNullOrEmpty(value))
                    return;

                _labelText = value;
                OnPropertyChanged(LabelText);
            }
        }

        public string? SelectedPath
        {
            get => _selectedPath;
            set
            {
                if (String.IsNullOrEmpty(value))
                    return;

                _selectedPath = value;
                OnPropertyChanged(SelectedPath);

                switch (IsPathADirectory)
                {
                    case true:
                        SelectionValid = Directory.Exists(value);

                        break;

                    case false:
                        SelectionValid = File.Exists(value);

                        break;
                }
            }
        }

        public bool IsPathADirectory { get; set; }

        public bool SelectionValid
        {
            get => _selectionValid;
            private set
            {
                switch (value)
                {
                    case true:
                        BorderOfImage.Background = Brushes.YellowGreen;
                        ImagePathValid.Source = new BitmapImage(new Uri(@"/Windows/icons8-ok-24.png", UriKind.Relative));

                        break;

                    case false:
                        BorderOfImage.Background = Brushes.Firebrick;
                        ImagePathValid.Source = new BitmapImage(new Uri(@"/Windows/icons8-aktualisieren-24.png", UriKind.Relative));

                        break;
                }
            }
        }

        public event Action? ValidPathSelected;
        public event PropertyChangedEventHandler? PropertyChanged;

        public UserControlPathSetting()
        {
            InitializeComponent();

            SelectionValid = false;
            IsPathADirectory = true;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ButtonPathSelection_OnClick(object sender, RoutedEventArgs e)
        {
            switch (IsPathADirectory)
            {
                case true:
                    var folderBrowserDialog = new VistaFolderBrowserDialog();

                    if (!folderBrowserDialog.ShowDialog().GetValueOrDefault() || !Directory.Exists(folderBrowserDialog.SelectedPath))
                    {
                        SelectionValid = false;
                        return;
                    }

                    _selectedPath = folderBrowserDialog.SelectedPath;

                    break;

                case false:
                    var fileBrowserDialog = new VistaOpenFileDialog();

                    if (!fileBrowserDialog.ShowDialog().GetValueOrDefault() || !File.Exists(fileBrowserDialog.FileName))
                    {
                        SelectionValid = false;
                        return;
                    }

                    _selectedPath = fileBrowserDialog.FileName;

                    break;
            }

            SelectionValid = true;
            ValidPathSelected?.Invoke();
        }
    }
}
