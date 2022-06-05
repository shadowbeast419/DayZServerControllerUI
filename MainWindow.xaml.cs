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
using System.Windows.Navigation;
using System.Windows.Shapes;
using DayZServerControllerUI.CtrlLogic;
using DayZServerControllerUI.LogParser;

namespace DayZServerControllerUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModelMain;
        private readonly LogParserViewModel _viewModelLogParser;
        private Logging _logger;

        public MainWindow()
        {
            InitializeComponent();

            _logger = new Logging(TextBoxLogging);
            _viewModelMain = new MainViewModel(ref _logger);
            _viewModelLogParser = new LogParserViewModel();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Initialize the logic
                await _viewModelMain.Initialize();
                _viewModelMain.AttachDiscordBotToLogger(ref _logger);
                _viewModelMain.PropertyChanged += ViewModelMain_PropertyChanged;

                _viewModelLogParser.Init();
                UserControlStatistics.Init(_viewModelLogParser);
                UserControlRankings.Init(_viewModelLogParser);
            }
            catch (IOException ex)
            {
                MessageBox.Show($"Initialization error: {ex.Message}", "Exception", MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Close();
            }

            // Check Server Status at Startup
            if (_viewModelMain.IsInitialized)
            {
                switch (_viewModelMain.IsServerRunning)
                {
                    case true:
                        LabelServerStatus.Content = $"Running";
                        LabelServerStatus.Foreground = Brushes.YellowGreen;

                        break;

                    case false:
                        LabelServerStatus.Content = $"Process not detected";
                        LabelServerStatus.Foreground = new SolidColorBrush(Color.FromRgb(160, 39, 39));

                        break;
                }
            }
        }

        private void ViewModelMain_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
