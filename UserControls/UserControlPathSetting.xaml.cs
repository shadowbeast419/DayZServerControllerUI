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
    public sealed partial class UserControlPathSetting : INotifyPropertyChanged
    {
        private string _labelText = String.Empty;
        private string _selectedPath = String.Empty;
        private UserControlPathSettingState _status;

        /// <summary>
        /// Defines the appearance of the UserControl depending of the validation of the Path selected with this UserCtrl
        /// </summary>
        public UserControlPathSettingState Status
        {
            get => _status;
            set
            {
                switch (value)
                {
                    case UserControlPathSettingState.PathInvalid:
                        BorderOfImage.Background = Brushes.Firebrick;
                        ImagePathValid.Visibility = Visibility.Visible;
                        ImagePathValid.Source = new BitmapImage(new Uri(@"/Windows/icons8-aktualisieren-24.png", UriKind.Relative));

                        break;
                    case UserControlPathSettingState.PathValid:
                        BorderOfImage.Background = Brushes.Transparent;
                        ImagePathValid.Visibility = Visibility.Visible;
                        ImagePathValid.Source = new BitmapImage(new Uri(@"/Windows/icons8-ok-24.png", UriKind.Relative));

                        break;
                    case UserControlPathSettingState.Disabled:
                        BorderOfImage.Background = Brushes.Wheat;
                        ImagePathValid.Visibility = Visibility.Hidden;

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                _status = value;
                OnPropertyChanged();
            }
        }

        public string LabelText
        {
            get => _labelText;
            set
            {
                if (String.IsNullOrEmpty(value))
                    return;

                _labelText = value;
                LabelPath.Content = _labelText;
                OnPropertyChanged();
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
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Checks if the configured path is a valid one and applies the result to the UI (via Status property)
        /// </summary>
        private bool IsPathValid
        {
            get
            {
                bool isValidPath;

                switch (IsPathADirectory)
                {
                    case true:
                        isValidPath = Directory.Exists(_selectedPath);

                        break;

                    case false:
                        isValidPath = File.Exists(_selectedPath);

                        break;
                }

                Status = isValidPath ? UserControlPathSettingState.PathValid
                    : UserControlPathSettingState.PathInvalid;

                return isValidPath;
            }
        }

        /// <summary>
        /// Defines whether the target path should be a file or directory
        /// </summary>
        public bool IsPathADirectory { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;


        public UserControlPathSetting()
        {
            InitializeComponent();

            // SettingsWindow is the DataContext
            DataContext = this.Parent;

            Status = UserControlPathSettingState.PathInvalid;
            IsPathADirectory = true;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ButtonPathSelection_OnClick(object sender, RoutedEventArgs e)
        {
            switch (IsPathADirectory)
            {
                case true:
                    var folderBrowserDialog = new VistaFolderBrowserDialog
                    {
                        Description = LabelText,
                        UseDescriptionForTitle = true
                    };

                    folderBrowserDialog.ShowDialog();

                    if (!Directory.Exists(folderBrowserDialog.SelectedPath))
                    {
                        Status = UserControlPathSettingState.PathInvalid;
                        return;
                    }

                    _selectedPath = folderBrowserDialog.SelectedPath;

                    break;

                case false:
                    var fileBrowserDialog = new VistaOpenFileDialog
                    {
                        CheckFileExists = true,
                        CheckPathExists = true,
                        ValidateNames = true,
                        // Only allow log files, text files and executables
                        Filter = "All allowed Types|*.exe;*.log;*.txt|" + 
                                 "Text files (*.txt)|*.txt|" +
                                 "Executables (*.exe)|*.exe|" +
                                 "Log files (*.log)|*.log"
                    };

                    fileBrowserDialog.ShowDialog();

                    if (!File.Exists(fileBrowserDialog.FileName))
                    {
                        Status = UserControlPathSettingState.PathInvalid;
                        return;
                    }

                    _selectedPath = fileBrowserDialog.FileName;

                    break;
            }

            TextBoxPath.Text = _selectedPath;
            Status = UserControlPathSettingState.PathValid;
            OnPropertyChanged(nameof(SelectedPath));
        }

        private void UserControlPathSetting_OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            switch (IsEnabled)
            {
                case true:
                    switch (IsPathValid)
                    {
                        // UserControl is enabled and the path is valid
                        case true:
                            Status = UserControlPathSettingState.PathValid;

                            break;
                        
                        // UserControl is enabled and the path is invalid
                        case false:
                            Status = UserControlPathSettingState.PathInvalid;

                            break;
                    }

                    break;

                case false:
                    // User Control is disabled (doesn't matter if path is valid or not)
                    Status = UserControlPathSettingState.Disabled;

                    break;
            }

            OnPropertyChanged();
        }
    }
}
