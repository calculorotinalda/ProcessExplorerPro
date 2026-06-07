using System.Collections.ObjectModel;
using System.Windows.Threading;
using ProcessExplorerPro.Helpers;
using ProcessExplorerPro.Models;
using ProcessExplorerPro.Services;

namespace ProcessExplorerPro.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        private readonly PerformanceService _performanceService = new();
        private readonly ProcessService _processService = new();
        private readonly DispatcherTimer _timer;

        private double _cpuUsage;
        private double _ramUsage;
        private double _gpuUsage;
        private double _diskUsage;
        private double _networkSpeedKbps;
        private double _systemTemp;
        
        private string _ramDetails = string.Empty;
        private string _diskDetails = string.Empty;
        private string _networkDetails = string.Empty;

        public double CpuUsage
        {
            get => _cpuUsage;
            set => SetProperty(ref _cpuUsage, value);
        }

        public double RamUsage
        {
            get => _ramUsage;
            set => SetProperty(ref _ramUsage, value);
        }

        public double GpuUsage
        {
            get => _gpuUsage;
            set => SetProperty(ref _gpuUsage, value);
        }

        public double DiskUsage
        {
            get => _diskUsage;
            set => SetProperty(ref _diskUsage, value);
        }

        public double NetworkSpeedKbps
        {
            get => _networkSpeedKbps;
            set => SetProperty(ref _networkSpeedKbps, value);
        }

        public double SystemTemp
        {
            get => _systemTemp;
            set => SetProperty(ref _systemTemp, value);
        }

        public string RamDetails
        {
            get => _ramDetails;
            set => SetProperty(ref _ramDetails, value);
        }

        public string DiskDetails
        {
            get => _diskDetails;
            set => SetProperty(ref _diskDetails, value);
        }

        public string NetworkDetails
        {
            get => _networkDetails;
            set => SetProperty(ref _networkDetails, value);
        }

        public ObservableCollection<ProcessItem> TopProcesses { get; } = new();

        public event Action<PerformanceService.SystemMetrics>? MetricsUpdated;

        public DashboardViewModel()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            // Run first sample asynchronously to allow UI to render first
            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(UpdateMetrics), DispatcherPriority.Background);
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            UpdateMetrics();
        }

        private void UpdateMetrics()
        {
            try
            {
                // 1. Fetch system metrics
                var metrics = _performanceService.GetSystemMetrics();
                
                CpuUsage = metrics.CpuUsage;
                RamUsage = metrics.RamUsage;
                GpuUsage = metrics.GpuUsage;
                DiskUsage = metrics.DiskUsage;
                NetworkSpeedKbps = metrics.NetworkSpeedKbps;
                SystemTemp = metrics.SystemTemp;

                RamDetails = $"{metrics.RamUsedGb:F1} GB / {metrics.RamTotalGb:F1} GB";
                DiskDetails = $"{metrics.DiskSpeedMbps:F1} MB/s";
                NetworkDetails = metrics.NetworkSpeedKbps > 1024 
                    ? $"{metrics.NetworkSpeedKbps / 1024.0:F2} Mbps" 
                    : $"{metrics.NetworkSpeedKbps:F1} Kbps";

                // Notify charts in the view
                MetricsUpdated?.Invoke(metrics);

                // 2. Fetch processes to determine top resource consumers
                var processData = _processService.GetProcesses();
                var topCpu = processData.FlatList
                    .OrderByDescending(p => p.CpuPercent)
                    .Take(5)
                    .ToList();

                TopProcesses.Clear();
                foreach (var p in topCpu)
                {
                    TopProcesses.Add(p);
                }
            }
            catch
            {
                // Gracefully handle query interruptions
            }
        }

        public void StopTimer()
        {
            _timer.Stop();
        }
    }
}
