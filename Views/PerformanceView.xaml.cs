using System.Windows.Controls;
using ProcessExplorerPro.ViewModels;

namespace ProcessExplorerPro.Views
{
    public partial class PerformanceView : UserControl
    {
        private readonly PerformanceViewModel _viewModel;

        public PerformanceView()
        {
            InitializeComponent();
            _viewModel = new PerformanceViewModel();
            DataContext = _viewModel;

            _viewModel.PerformanceMetricsUpdated += ViewModel_PerformanceMetricsUpdated;

            Unloaded += PerformanceView_Unloaded;
        }

        private void ViewModel_PerformanceMetricsUpdated(Services.PerformanceService.SystemMetrics metrics)
        {
            Dispatcher.Invoke(() =>
            {
                // 1. Update Mini Preview Charts on the Left
                MiniCpuChart.AddValue(metrics.CpuUsage);
                MiniRamChart.AddValue(metrics.RamUsage);
                MiniGpuChart.AddValue(metrics.GpuUsage);
                MiniDiskChart.AddValue(metrics.DiskUsage);
                MiniNetworkChart.AddValue(metrics.NetworkSpeedKbps);

                // 2. Update Large Detailed Charts on the Right
                DetailedCpuChart.AddValue(metrics.CpuUsage);
                DetailedRamChart.AddValue(metrics.RamUsage);
                DetailedGpuChart.AddValue(metrics.GpuUsage);

                // Scale Disk y-axis to match maximum input
                if (metrics.DiskUsage > DetailedDiskChart.MaxValue)
                {
                    DetailedDiskChart.MaxValue = Math.Max(100.0, metrics.DiskUsage * 1.3);
                }
                DetailedDiskChart.AddValue(metrics.DiskUsage);

                // Scale Network y-axis to match throughput
                if (metrics.NetworkSpeedKbps > DetailedNetworkChart.MaxValue)
                {
                    DetailedNetworkChart.MaxValue = Math.Max(1000.0, metrics.NetworkSpeedKbps * 1.3);
                }
                DetailedNetworkChart.AddValue(metrics.NetworkSpeedKbps);
            });
        }

        private void PerformanceView_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            _viewModel.StopTimer();
            _viewModel.PerformanceMetricsUpdated -= ViewModel_PerformanceMetricsUpdated;
        }
    }
}
