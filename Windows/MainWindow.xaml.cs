using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using DayZServerControllerUI.CtrlLogic;
using DayZServerControllerUI.LogParser;

namespace DayZServerControllerUI.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
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
            _viewModelMain.SettingsValidStatusChanged += ViewModelMain_SettingsValidStatusChanged;

            _viewModelLogParser = new LogParserViewModel();
            _viewModelLogParser.PropertyChanged += ViewModelLogParser_PropertyChanged;
            this.DataContext = _viewModelLogParser;
        }

        private async void ViewModelMain_SettingsValidStatusChanged(bool obj)
        {
            if (_viewModelMainInitialized || !obj)
                return;

            // Initialization can now be done with all settings being valid
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
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_viewModelMain.IsInitialized)
                return;

            // Check Server Status at Startup
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

        private void ButtonDiscordLink_OnClick(object sender, RoutedEventArgs e)
        {
            // TODO: Refer to Discord-IO Link (http://discord.io/AustrianDayZ)
            // throw new NotImplementedException();
        }

        private void ImageButtonDiscordLink_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // TODO: Scale image properly to the size of the button
            // throw new NotImplementedException();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _viewModelMain.Dispose();
        }
    }
}
