using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ProcessExplorerPro.Helpers;
using ProcessExplorerPro.Models;
using ProcessExplorerPro.Services;

namespace ProcessExplorerPro.ViewModels
{
    public class ProcessesViewModel : ViewModelBase
    {
        private readonly ProcessService _processService = new();
        private readonly AiService _aiService = new();
        private readonly DispatcherTimer _timer;

        private string _searchText = string.Empty;
        private string _selectedViewMode = "Tree"; // "Tree" or "Flat"
        private string _selectedGroupMode = "None"; // "None", "User", "Category"
        private ProcessItem? _selectedProcess;

        // Details Panel Properties
        private string _detailPublisher = string.Empty;
        private string _detailVersion = string.Empty;
        private string _detailDate = string.Empty;
        private string _detailCommandLine = string.Empty;
        private string _detailIntegrity = string.Empty;
        private string _detailRiskLevel = "Low";
        private int _detailRiskScore;
        private string _detailAiExplanation = string.Empty;
        private string _detailVirusTotal = string.Empty;

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplyFilterAndSort();
                }
            }
        }

        public string SelectedViewMode
        {
            get => _selectedViewMode;
            set
            {
                if (SetProperty(ref _selectedViewMode, value))
                {
                    ApplyFilterAndSort();
                }
            }
        }

        public string SelectedGroupMode
        {
            get => _selectedGroupMode;
            set
            {
                if (SetProperty(ref _selectedGroupMode, value))
                {
                    ApplyFilterAndSort();
                }
            }
        }

        public ProcessItem? SelectedProcess
        {
            get => _selectedProcess;
            set
            {
                if (SetProperty(ref _selectedProcess, value))
                {
                    LoadProcessDetails(value);
                }
            }
        }

        // Selected process details binding properties
        public string DetailPublisher { get => _detailPublisher; set => SetProperty(ref _detailPublisher, value); }
        public string DetailVersion { get => _detailVersion; set => SetProperty(ref _detailVersion, value); }
        public string DetailDate { get => _detailDate; set => SetProperty(ref _detailDate, value); }
        public string DetailCommandLine { get => _detailCommandLine; set => SetProperty(ref _detailCommandLine, value); }
        public string DetailIntegrity { get => _detailIntegrity; set => SetProperty(ref _detailIntegrity, value); }
        public string DetailRiskLevel { get => _detailRiskLevel; set => SetProperty(ref _detailRiskLevel, value); }
        public int DetailRiskScore { get => _detailRiskScore; set => SetProperty(ref _detailRiskScore, value); }
        public string DetailAiExplanation { get => _detailAiExplanation; set => SetProperty(ref _detailAiExplanation, value); }
        public string DetailVirusTotal { get => _detailVirusTotal; set => SetProperty(ref _detailVirusTotal, value); }

        public ObservableCollection<ProcessItem> DisplayedProcesses { get; } = new();
        public ObservableCollection<string> LoadedModules { get; } = new();
        public ObservableCollection<ProcessThreadInfo> ProcessThreads { get; } = new();

        public ICommand KillCommand { get; }
        public ICommand ChangePriorityCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand ToggleExpandCommand { get; }

        private List<ProcessItem> _cachedFlat = new();
        private List<ProcessItem> _cachedTree = new();

        public event Action<ProcessItem>? SelectedProcessMetricsUpdated;

        public ProcessesViewModel()
        {
            KillCommand = new RelayCommand(ExecuteKill, CanInteractWithProcess);
            ChangePriorityCommand = new RelayCommand(ExecuteChangePriority, CanInteractWithProcess);
            ExportCommand = new RelayCommand(ExecuteExport);
            ToggleExpandCommand = new RelayCommand(ExecuteToggleExpand);

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2) // Processes refresh every 2 seconds by default
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            // Run first sample asynchronously to allow UI to render first
            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(RefreshList), DispatcherPriority.Background);
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            RefreshList();
        }

        public void StopTimer()
        {
            _timer.Stop();
        }

        private void RefreshList()
        {
            try
            {
                var result = _processService.GetProcesses();
                _cachedFlat = result.FlatList;
                _cachedTree = result.TreeList;

                ApplyFilterAndSort();

                // Keep detail panel updated for selected process
                if (SelectedProcess != null)
                {
                    var updatedSelected = _cachedFlat.FirstOrDefault(p => p.Pid == SelectedProcess.Pid);
                    if (updatedSelected != null)
                    {
                        SelectedProcess.CpuPercent = updatedSelected.CpuPercent;
                        SelectedProcess.MemoryBytes = updatedSelected.MemoryBytes;
                        SelectedProcess.GpuPercent = updatedSelected.GpuPercent;
                        SelectedProcess.DiskBytesPerSec = updatedSelected.DiskBytesPerSec;
                        SelectedProcess.ThreadsCount = updatedSelected.ThreadsCount;
                        SelectedProcess.HandlesCount = updatedSelected.HandlesCount;

                        SelectedProcessMetricsUpdated?.Invoke(updatedSelected);
                    }
                }
            }
            catch
            {
                // ignore
            }
        }

        private void ApplyFilterAndSort()
        {
            DisplayedProcesses.Clear();
            var filtered = new List<ProcessItem>();

            if (SelectedViewMode == "Tree" && string.IsNullOrEmpty(SearchText))
            {
                // Render tree hierarchy via DFS flattening
                foreach (var root in _cachedTree)
                {
                    AddTreeItemToFlatList(root, filtered);
                }
            }
            else
            {
                // Flat mode or Searched list
                var list = _cachedFlat;
                if (!string.IsNullOrEmpty(SearchText))
                {
                    string search = SearchText.ToLowerInvariant();
                    list = list.Where(p => p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) || 
                                           p.Pid.ToString().Contains(search)).ToList();
                }

                // If grouping by User
                if (SelectedGroupMode == "User")
                {
                    filtered = list.OrderBy(p => p.User).ThenBy(p => p.Name).ToList();
                }
                // If grouping by AI Category
                else if (SelectedGroupMode == "Category")
                {
                    filtered = list.OrderBy(p => p.Description).ThenBy(p => p.Name).ToList();
                }
                else
                {
                    // Sort by CPU by default
                    filtered = list.OrderByDescending(p => p.CpuPercent).ToList();
                }

                // In flat mode, depth is zeroed
                foreach (var item in filtered)
                {
                    item.Depth = 0;
                }
            }

            foreach (var item in filtered)
            {
                DisplayedProcesses.Add(item);
            }
        }

        private void AddTreeItemToFlatList(ProcessItem item, List<ProcessItem> list)
        {
            list.Add(item);
            if (item.IsExpanded && item.Children.Count > 0)
            {
                foreach (var child in item.Children)
                {
                    AddTreeItemToFlatList(child, list);
                }
            }
        }

        private void ExecuteToggleExpand(object? obj)
        {
            if (obj is ProcessItem item)
            {
                item.IsExpanded = !item.IsExpanded;
                ApplyFilterAndSort();
            }
        }

        private bool CanInteractWithProcess(object? obj)
        {
            return SelectedProcess != null && SelectedProcess.Pid > 4;
        }

        private void ExecuteKill(object? obj)
        {
            if (SelectedProcess == null) return;

            var result = MessageBox.Show(
                $"Tem a certeza de que deseja terminar o processo {SelectedProcess.Name} (PID: {SelectedProcess.Pid})?",
                "Terminar Processo - Process Explorer Pro",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using var proc = Process.GetProcessById(SelectedProcess.Pid);
                    proc.Kill(true);
                    MessageBox.Show("Processo terminado com sucesso.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                    RefreshList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao terminar o processo: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExecuteChangePriority(object? obj)
        {
            if (SelectedProcess == null || obj is not string priorityStr) return;

            try
            {
                using var proc = Process.GetProcessById(SelectedProcess.Pid);
                ProcessPriorityClass priorityClass = priorityStr switch
                {
                    "RealTime" => ProcessPriorityClass.RealTime,
                    "High" => ProcessPriorityClass.High,
                    "AboveNormal" => ProcessPriorityClass.AboveNormal,
                    "BelowNormal" => ProcessPriorityClass.BelowNormal,
                    "Idle" => ProcessPriorityClass.Idle,
                    _ => ProcessPriorityClass.Normal
                };

                proc.PriorityClass = priorityClass;
                SelectedProcess.Priority = priorityStr;
                MessageBox.Show($"Prioridade alterada para {priorityStr}.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao alterar prioridade: {ex.Message}\nNota: Alterar para RealTime ou gerir processos do sistema requer privilégios elevados.", "Acesso Negado", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteExport(object? obj)
        {
            if (obj is not string format) return;

            var sfd = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Exportar Relatório - Process Explorer Pro",
                FileName = $"ProcessExplorerPro_Export_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (format == "CSV")
            {
                sfd.Filter = "CSV Files (*.csv)|*.csv";
                if (sfd.ShowDialog() == true)
                {
                    ExportService.ExportToCsv(_cachedFlat, sfd.FileName);
                    MessageBox.Show("Dados exportados para CSV com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else if (format == "JSON")
            {
                sfd.Filter = "JSON Files (*.json)|*.json";
                if (sfd.ShowDialog() == true)
                {
                    ExportService.ExportToJson(_cachedFlat, sfd.FileName);
                    MessageBox.Show("Dados exportados para JSON com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else if (format == "HTML")
            {
                sfd.Filter = "HTML Files (*.html)|*.html";
                if (sfd.ShowDialog() == true)
                {
                    ExportService.ExportToHtml(_cachedFlat, sfd.FileName);
                    MessageBox.Show("Relatório HTML gerado com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void LoadProcessDetails(ProcessItem? process)
        {
            LoadedModules.Clear();
            ProcessThreads.Clear();

            if (process == null)
            {
                DetailPublisher = string.Empty;
                DetailVersion = string.Empty;
                DetailDate = string.Empty;
                DetailCommandLine = string.Empty;
                DetailIntegrity = string.Empty;
                DetailRiskLevel = "Low";
                DetailRiskScore = 0;
                DetailAiExplanation = string.Empty;
                DetailVirusTotal = string.Empty;
                return;
            }

            // 1. Fetch file version details
            string version = "N/A";
            string dateStr = "Unknown";
            string cmdLine = "N/A";

            if (File.Exists(process.Path))
            {
                try
                {
                    var fvi = FileVersionInfo.GetVersionInfo(process.Path);
                    version = fvi.ProductVersion ?? fvi.FileVersion ?? "N/A";
                    
                    var creationTime = File.GetCreationTime(process.Path);
                    dateStr = creationTime.ToString("yyyy-MM-dd HH:mm:ss");
                }
                catch
                {
                    // ignore version fetch issues
                }
            }

            DetailPublisher = process.Publisher;
            DetailVersion = version;
            DetailDate = dateStr;
            DetailIntegrity = process.IntegrityLevel;

            // Load WMI-based commandline arguments
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Pid}");
                using var collection = searcher.Get();
                foreach (ManagementObject obj in collection)
                {
                    cmdLine = obj["CommandLine"]?.ToString() ?? "N/A";
                }
            }
            catch
            {
                // ignore
            }
            DetailCommandLine = cmdLine;

            // 2. Fetch DLL modules and Threads in background
            Task.Run(() =>
            {
                var modules = ProcessHelper.GetLoadedModules(process.Pid);
                var threads = ProcessHelper.GetProcessThreads(process.Pid);

                if (Application.Current == null) return;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Ensure process is still selected
                    if (SelectedProcess?.Pid != process.Pid) return;

                    foreach (var mod in modules)
                        LoadedModules.Add(mod);

                    foreach (var t in threads)
                        ProcessThreads.Add(t);
                });
            });

            // 3. AI Safety Evaluation details
            var aiReport = _aiService.AnalyzeProcess(process.Name, process.Path, !process.Publisher.Equals("Unknown"), process.Publisher);
            DetailRiskScore = aiReport.RiskScore;
            DetailRiskLevel = aiReport.RiskScore >= 75 ? "Perigo" : (aiReport.RiskScore >= 40 ? "Alerta" : "Seguro");
            
            var findingsSb = new System.Text.StringBuilder();
            findingsSb.AppendLine(aiReport.Description);
            findingsSb.AppendLine("\n**Análise de Segurança:**");
            foreach (var finding in aiReport.Findings)
            {
                findingsSb.AppendLine($"- {finding}");
            }
            findingsSb.AppendLine("\n**Recomendações:**");
            foreach (var suggestion in aiReport.Suggestions)
            {
                findingsSb.AppendLine($"- {suggestion}");
            }
            DetailAiExplanation = findingsSb.ToString();
            DetailVirusTotal = $"{aiReport.VirusTotalStatus} ({aiReport.VirusTotalPositives}/{aiReport.VirusTotalTotal})";
        }
    }
}
