using System.Windows.Threading;
using ProcessExplorerPro.Helpers;
using ProcessExplorerPro.Services;

namespace ProcessExplorerPro.ViewModels
{
    public class PerformanceViewModel : ViewModelBase
    {
        private readonly PerformanceService _performanceService = new();
        private readonly DispatcherTimer _timer;

        private double _cpuUsage;
        private double _ramUsage;
        private double _gpuUsage;
        private double _diskUsage;
        private double _networkSpeedKbps;

        private string _cpuDetails = "N/A";
        private string _ramDetails = "N/A";
        private string _gpuDetails = "N/A";
        private string _diskDetails = "N/A";
        private string _networkDetails = "N/A";

        private string _selectedMetric = "CPU"; // "CPU", "RAM", "GPU", "Disk", "Network"

        public double CpuUsage { get => _cpuUsage; set => SetProperty(ref _cpuUsage, value); }
        public double RamUsage { get => _ramUsage; set => SetProperty(ref _ramUsage, value); }
        public double GpuUsage { get => _gpuUsage; set => SetProperty(ref _gpuUsage, value); }
        public double DiskUsage { get => _diskUsage; set => SetProperty(ref _diskUsage, value); }
        public double NetworkSpeedKbps { get => _networkSpeedKbps; set => SetProperty(ref _networkSpeedKbps, value); }

        public string CpuDetails { get => _cpuDetails; set => SetProperty(ref _cpuDetails, value); }
        public string RamDetails { get => _ramDetails; set => SetProperty(ref _ramDetails, value); }
        public string GpuDetails { get => _gpuDetails; set => SetProperty(ref _gpuDetails, value); }
        public string DiskDetails { get => _diskDetails; set => SetProperty(ref _diskDetails, value); }
        public string NetworkDetails { get => _networkDetails; set => SetProperty(ref _networkDetails, value); }

        public string SelectedMetric
        {
            get => _selectedMetric;
            set => SetProperty(ref _selectedMetric, value);
        }

        public event Action<PerformanceService.SystemMetrics>? PerformanceMetricsUpdated;

        public PerformanceViewModel()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            UpdateMetrics();
        }

        public void StopTimer()
        {
            _timer.Stop();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            UpdateMetrics();
        }

        private void UpdateMetrics()
        {
            try
            {
                var metrics = _performanceService.GetSystemMetrics();

                CpuUsage = metrics.CpuUsage;
                RamUsage = metrics.RamUsage;
                GpuUsage = metrics.GpuUsage;
                DiskUsage = metrics.DiskUsage;
                NetworkSpeedKbps = metrics.NetworkSpeedKbps;

                CpuDetails = $"{metrics.CpuUsage:F1}% de utilização";
                RamDetails = $"{metrics.RamUsedGb:F1} GB de {metrics.RamTotalGb:F1} GB ({metrics.RamUsage:0}%)";
                GpuDetails = $"{metrics.GpuUsage:F1}% de utilização";
                DiskDetails = $"{metrics.DiskSpeedMbps:F1} MB/s de E/S";
                NetworkDetails = metrics.NetworkSpeedKbps > 1024 
                    ? $"{metrics.NetworkSpeedKbps / 1024.0:F2} Mbps" 
                    : $"{metrics.NetworkSpeedKbps:F1} Kbps";

                PerformanceMetricsUpdated?.Invoke(metrics);
            }
            catch
            {
                // ignore WMI lockups
            }
        }
    }
}
