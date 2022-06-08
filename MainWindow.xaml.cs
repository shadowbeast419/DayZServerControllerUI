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
using DayZServerControllerUI.Windows;

namespace DayZServerControllerUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SettingsWindow _settingsWindow;
        private readonly MainViewModel _viewModelMain;
        private readonly LogParserViewModel _viewModelLogParser;
        private Logging _logger;
        private bool _disableRefresh;
        private bool _viewModelMainInitialized;
        private bool _viewModelLogParserInitilized;

        public MainWindow()
        {
            InitializeComponent();

            _logger = new Logging(TextBoxLogging);

            _viewModelMain = new MainViewModel(ref _logger);
            _viewModelMain.PropertyChanged += ViewModelMain_PropertyChanged;

            _viewModelLogParser = new LogParserViewModel();
            _viewModelLogParser.PropertyChanged += ViewModelLogParser_PropertyChanged;
            this.DataContext = _viewModelLogParser;

            _settingsWindow = new SettingsWindow();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Initialize the logic
                await _viewModelMain.Initialize();
                _viewModelMain.AttachDiscordBotToLogger(ref _logger);
                _viewModelMain.PropertyChanged += ViewModelMain_PropertyChanged;
                _viewModelMain.ModUpdateDetected += ViewModelMain_ModUpdateDetected;
                _viewModelMain.ServerRestarting += ViewModelMain_ServerRestarting;
                _viewModelMainInitialized = true;

                _viewModelLogParser.Init();
                UserControlStatistics.Init(_viewModelLogParser);
                UserControlRankings.Init(_viewModelLogParser);
                _viewModelLogParserInitilized = true;
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

        #region ViewModelLogParser

        private void ViewModelLogParser_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (_disableRefresh || !_viewModelLogParserInitilized)
                return;


        }

        #endregion

        #region ViewModelMain

        private void ViewModelMain_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (_disableRefresh || !_viewModelMainInitialized)
                return;

            _disableRefresh = true;

            switch (e.PropertyName)
            {
                case "RestartPeriodProgress":
                    this.Dispatcher.Invoke(() =>
                    {
                        ProgressBarRestartPeriod.Value = _viewModelMain.RestartPeriodProgress;
                    });

                    break;
            }

            _disableRefresh = false;
        }
        private void ViewModelMain_ServerRestarting()
        {
            throw new NotImplementedException();
        }

        private void ViewModelMain_ModUpdateDetected()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
