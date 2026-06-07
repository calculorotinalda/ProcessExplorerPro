using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Threading;
using ProcessExplorerPro.Helpers;
using ProcessExplorerPro.Models;

namespace ProcessExplorerPro.ViewModels
{
    public class NetworkViewModel : ViewModelBase
    {
        private string _searchText = string.Empty;
        private string _selectedProtocol = "Todos"; // "Todos", "TCP", "UDP"
        private DispatcherTimer _timer;

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplyFilter();
                }
            }
        }

        public string SelectedProtocol
        {
            get => _selectedProtocol;
            set
            {
                if (SetProperty(ref _selectedProtocol, value))
                {
                    ApplyFilter();
                }
            }
        }

        public ObservableCollection<NetworkItem> DisplayedConnections { get; } = new();

        public ICommand RefreshCommand { get; }

        private List<NetworkItem> _allConnections = new();

        public NetworkViewModel()
        {
            RefreshCommand = new RelayCommand(_ => RefreshConnections());

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5) // Refresh connections every 5 seconds
            };
            _timer.Tick += (s, e) => RefreshConnections();
            _timer.Start();

            RefreshConnections();
        }

        public void StopTimer()
        {
            _timer.Stop();
        }

        private void RefreshConnections()
        {
            Task.Run(() =>
            {
                var list = new List<NetworkItem>();
                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "netstat.exe",
                        Arguments = "-ano",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(startInfo);
                    if (process != null)
                    {
                        using var reader = process.StandardOutput;
                        string output = reader.ReadToEnd();
                        var lines = output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                        // Cache active process PID maps
                        var processes = Process.GetProcesses();
                        var processMap = new Dictionary<int, string>();
                        foreach (var p in processes)
                        {
                            processMap[p.Id] = p.ProcessName;
                        }

                        foreach (var line in lines)
                        {
                            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length < 4) continue;

                            string proto = parts[0].Trim();
                            if (proto != "TCP" && proto != "UDP") continue;

                            string local = parts[1];
                            string remote = parts[2];
                            string state = string.Empty;
                            int pid = 0;

                            if (proto == "TCP")
                            {
                                if (parts.Length >= 5)
                                {
                                    state = parts[3];
                                    int.TryParse(parts[4], out pid);
                                }
                                else if (parts.Length == 4) // some entries might skip state if listening or weird config
                                {
                                    state = "UNKNOWN";
                                    int.TryParse(parts[3], out pid);
                                }
                            }
                            else // UDP
                            {
                                state = "N/A";
                                int.TryParse(parts[3], out pid);
                            }

                            SplitEndpoint(local, out string localAddr, out int localPort);
                            SplitEndpoint(remote, out string remoteAddr, out int remotePort);

                            processMap.TryGetValue(pid, out string? procName);
                            procName ??= (pid == 0 ? "System Idle" : (pid == 4 ? "System" : "Unknown"));

                            list.Add(new NetworkItem
                            {
                                Protocol = proto,
                                LocalAddress = localAddr,
                                LocalPort = localPort,
                                RemoteAddress = remoteAddr,
                                RemotePort = remotePort,
                                State = state,
                                Pid = pid,
                                ProcessName = procName
                            });
                        }
                    }
                }
                catch
                {
                    // Catch netstat exceptions
                }

                // Update UI Collection
                App.Current.Dispatcher.Invoke(() =>
                {
                    _allConnections = list.OrderBy(c => c.ProcessName).ToList();
                    ApplyFilter();
                });
            });
        }

        private void ApplyFilter()
        {
            DisplayedConnections.Clear();
            var filtered = _allConnections.AsEnumerable();

            // Protocol filter
            if (SelectedProtocol == "TCP")
                filtered = filtered.Where(c => c.Protocol == "TCP");
            else if (SelectedProtocol == "UDP")
                filtered = filtered.Where(c => c.Protocol == "UDP");

            // Search text filter
            if (!string.IsNullOrEmpty(SearchText))
            {
                string search = SearchText.ToLowerInvariant();
                filtered = filtered.Where(c => c.ProcessName.Contains(search, StringComparison.OrdinalIgnoreCase) || 
                                               c.LocalEndpoint.Contains(search) || 
                                               c.RemoteEndpoint.Contains(search) || 
                                               c.Pid.ToString().Contains(search) ||
                                               c.State.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var connection in filtered)
            {
                DisplayedConnections.Add(connection);
            }
        }

        private static void SplitEndpoint(string endpoint, out string address, out int port)
        {
            address = endpoint;
            port = 0;

            int lastColon = endpoint.LastIndexOf(':');
            if (lastColon > 0)
            {
                address = endpoint.Substring(0, lastColon);
                string portStr = endpoint.Substring(lastColon + 1);
                int.TryParse(portStr, out port);
            }
        }
    }
}
