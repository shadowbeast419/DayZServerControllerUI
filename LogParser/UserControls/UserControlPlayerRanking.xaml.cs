using System;
using System.Collections.Generic;
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

namespace DayZServerControllerUI.LogParser.UserControls
{
    /// <summary>
    /// Interaction logic for UserControlPlayerRanking.xaml
    /// </summary>
    public partial class UserControlPlayerRanking : UserControl
    {
        private LogParserViewModel? _viewModel;
        private bool _showTimeInHours = true;

        public UserControlPlayerRanking()
        {
            InitializeComponent();
        }

        public void Init(LogParserViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        private void UpdateRanking(List<PlayerStatistics> playerStatistics)
        {
            IEnumerable<PlayerStatisticsDataItem> dataItems = playerStatistics.Select(x => x.ToDataItem(_showTimeInHours));

            this.Dispatcher.Invoke(() =>
            {
                dataGridRanking.ItemsSource = dataItems;
            });
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _showTimeInHours = true;

            if (_viewModel == null)
                return;

            UpdateRanking(_viewModel.OnlineStatistics.ToList());
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _showTimeInHours = false;

            if (_viewModel == null)
                return;

            UpdateRanking(_viewModel.OnlineStatistics.ToList());
        }
    }
}
