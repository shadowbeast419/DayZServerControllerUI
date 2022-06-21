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

        public MainWindow()
        {
            InitializeComponent();

            _logger = new Logging(TextBoxLogging);

            _viewModelMain = new MainViewModel(ref _logger);

            _viewModelLogParser = new LogParserViewModel();
            this.DataContext = _viewModelLogParser;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Initialize the logic
                _viewModelMain.Initialized += ViewModelMain_Initialized;
                await _viewModelMain.StartInitializingAsync();

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

        #region ViewModelLogParser Events

        private void ViewModelLogParser_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (_disableRefresh)
                return;
        }

        #endregion

        #region ViewModelMain Events

        /// <summary>
        /// Logic has finished initialing, now we can properly take care of the DayZ Server
        /// </summary>
        private void ViewModelMain_Initialized()
        {
            _viewModelMain.AttachDiscordBotToLogger(ref _logger);
            _viewModelMain.PropertyChanged += ViewModelMain_PropertyChanged;
            _viewModelMain.ServerRestarting += ViewModelMain_ServerRestarting;
        }

        private void ViewModelMain_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (_disableRefresh || !_viewModelMain.IsInitialized)
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

        #region UI Events

        private void ButtonDiscordLink_OnClick(object sender, RoutedEventArgs e)
        {
            // TODO: Refer to Discord-IO Link (http://discord.io/AustrianDayZ)
            // throw new NotImplementedException();
        }

        private void MenuItemConfigurePaths_OnClick(object sender, RoutedEventArgs e)
        {
            _viewModelMain.SettingsWindowVisible = true;
        }

        private void MenuItemResetPaths_OnClick(object sender, RoutedEventArgs e)
        {
            _viewModelMain.ClearPaths();
        }
        private void MenuItemAbout_OnClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void MenuItemUsefulLinks_OnClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _viewModelMain.Dispose();
        }

        #endregion
    }
}
