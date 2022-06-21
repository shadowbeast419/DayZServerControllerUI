using System.Windows.Controls;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Controls.DataVisualization.Charting;

namespace DayZServerControllerUI.LogParser.UserControls
{
    /// <summary>
    /// Interaction logic for UserControlPlayerStatistics.xaml
    /// </summary>
    public partial class UserControlPlayerStatistics
    {
        private LogParserViewModel? _viewModel;

        public UserControlPlayerStatistics()
        {
            InitializeComponent();
        }

        public void Init(LogParserViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        private void ComboBoxPlayers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_viewModel == null)
                return;

            DayZPlayer selectedPlayer = (DayZPlayer)comboBoxPlayers.SelectedItem;
            PlayerStatistics? statistics = _viewModel.OnlineStatistics.FirstOrDefault(x => x.Player == selectedPlayer);

            if (statistics != null)
            {
                // Load the data for the selected player
                LoadBarChartData(statistics);
            }
        }

        private void LoadBarChartData(PlayerStatistics playerStatistics)
        {
            onlineTimeChart.Title = $"{playerStatistics.Player} Online Statistics";

            List<KeyValuePair<string, double>> chartData = new List<KeyValuePair<string, double>>();

            foreach (var statisticsPair in playerStatistics.OnlineTimePerDay)
            {
                chartData.Add(new KeyValuePair<string, double>(statisticsPair.Key.ToShortDateString(),
                    statisticsPair.Value.TotalMinutes));
            }

            ((ColumnSeries)onlineTimeChart.Series[0]).ItemsSource = chartData;
        }
    }
}
