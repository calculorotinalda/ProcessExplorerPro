using System.Windows;
using System.Windows.Controls;
using ProcessExplorerPro.Models;
using ProcessExplorerPro.ViewModels;

namespace ProcessExplorerPro.Views
{
    public partial class ProcessesView : UserControl
    {
        private readonly ProcessesViewModel _viewModel;

        public ProcessesView()
        {
            InitializeComponent();
            _viewModel = new ProcessesViewModel();
            DataContext = _viewModel;

            _viewModel.SelectedProcessMetricsUpdated += ViewModel_SelectedProcessMetricsUpdated;

            Unloaded += ProcessesView_Unloaded;
        }

        private void ViewModel_SelectedProcessMetricsUpdated(ProcessItem updatedProcess)
        {
            Dispatcher.Invoke(() =>
            {
                // Safety double check
                if (_viewModel.SelectedProcess == null || _viewModel.SelectedProcess.Pid != updatedProcess.Pid) return;

                // Add values to process detail charts
                ProcessCpuChart.AddValue(updatedProcess.CpuPercent);

                // Memory in MB
                double memMb = updatedProcess.MemoryBytes / (1024.0 * 1024.0);
                if (memMb > ProcessRamChart.MaxValue)
                {
                    ProcessRamChart.MaxValue = Math.Max(128.0, memMb * 1.3);
                }
                ProcessRamChart.AddValue(memMb);

                // Disk in KB/s
                double diskKb = updatedProcess.DiskBytesPerSec / 1024.0;
                if (diskKb > ProcessDiskChart.MaxValue)
                {
                    ProcessDiskChart.MaxValue = Math.Max(500.0, diskKb * 1.3);
                }
                ProcessDiskChart.AddValue(diskKb);
            });
        }

        private void CloseDetails_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.SelectedProcess = null;
        }

        private void ProcessesView_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.StopTimer();
            _viewModel.SelectedProcessMetricsUpdated -= ViewModel_SelectedProcessMetricsUpdated;
        }
    }
}
