using System.Windows;
using System.Windows.Controls;
using ProcessExplorerPro.ViewModels;

namespace ProcessExplorerPro.Views
{
    public partial class DashboardView : UserControl
    {
        private readonly DashboardViewModel _viewModel;

        public DashboardView()
        {
            InitializeComponent();
            _viewModel = new DashboardViewModel();
            DataContext = _viewModel;

            // Subscribe to metrics events to update charts in real-time
            _viewModel.MetricsUpdated += ViewModel_MetricsUpdated;

            Unloaded += DashboardView_Unloaded;
        }

        private void ViewModel_MetricsUpdated(Services.PerformanceService.SystemMetrics metrics)
        {
            // Update each chart view
            Dispatcher.Invoke(() =>
            {
                CpuChart.AddValue(metrics.CpuUsage);
                RamChart.AddValue(metrics.RamUsage);
                GpuChart.AddValue(metrics.GpuUsage);
                DiskChart.AddValue(metrics.DiskUsage);
                NetworkChart.AddValue(metrics.NetworkSpeedKbps);
                TempChart.AddValue(metrics.SystemTemp);
            });
        }

        private void DashboardView_Unloaded(object sender, RoutedEventArgs e)
        {
            // Stop timers when navigating away to free resources
            _viewModel.StopTimer();
            _viewModel.MetricsUpdated -= ViewModel_MetricsUpdated;
        }
    }
}
