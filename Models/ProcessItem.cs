using System.Collections.ObjectModel;
using ProcessExplorerPro.Helpers;

namespace ProcessExplorerPro.Models
{
    public class ProcessItem : ViewModelBase
    {
        private string _name = string.Empty;
        private int _pid;
        private int _parentPid;
        private string _user = "System";
        private double _cpuPercent;
        private long _memoryBytes;
        private string _memoryString = "0 B";
        private double _gpuPercent;
        private long _diskBytesPerSec;
        private string _diskString = "0 B/s";
        private long _networkBytesPerSec;
        private string _networkString = "0 B/s";
        private int _threadsCount;
        private int _handlesCount;
        private string _priority = "Normal";
        private string _status = "Running";
        private string _path = string.Empty;
        private string _publisher = "Unknown";
        private string _integrityLevel = "Medium";
        private string _description = "Active system process";
        private bool _isSuspicious;
        private int _riskScore;
        private int _depth;
        private bool _isExpanded = true;
        private bool _isVisible = true;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public int Pid
        {
            get => _pid;
            set => SetProperty(ref _pid, value);
        }

        public int ParentPid
        {
            get => _parentPid;
            set => SetProperty(ref _parentPid, value);
        }

        public string User
        {
            get => _user;
            set => SetProperty(ref _user, value);
        }

        public double CpuPercent
        {
            get => _cpuPercent;
            set => SetProperty(ref _cpuPercent, value);
        }

        public long MemoryBytes
        {
            get => _memoryBytes;
            set
            {
                if (SetProperty(ref _memoryBytes, value))
                {
                    MemoryString = FormatBytes(value);
                }
            }
        }

        public string MemoryString
        {
            get => _memoryString;
            set => SetProperty(ref _memoryString, value);
        }

        public double GpuPercent
        {
            get => _gpuPercent;
            set => SetProperty(ref _gpuPercent, value);
        }

        public long DiskBytesPerSec
        {
            get => _diskBytesPerSec;
            set
            {
                if (SetProperty(ref _diskBytesPerSec, value))
                {
                    DiskString = FormatBytes(value) + "/s";
                }
            }
        }

        public string DiskString
        {
            get => _diskString;
            set => SetProperty(ref _diskString, value);
        }

        public long NetworkBytesPerSec
        {
            get => _networkBytesPerSec;
            set
            {
                if (SetProperty(ref _networkBytesPerSec, value))
                {
                    NetworkString = FormatBytes(value) + "/s";
                }
            }
        }

        public string NetworkString
        {
            get => _networkString;
            set => SetProperty(ref _networkString, value);
        }

        public int ThreadsCount
        {
            get => _threadsCount;
            set => SetProperty(ref _threadsCount, value);
        }

        public int HandlesCount
        {
            get => _handlesCount;
            set => SetProperty(ref _handlesCount, value);
        }

        public string Priority
        {
            get => _priority;
            set => SetProperty(ref _priority, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public string Path
        {
            get => _path;
            set => SetProperty(ref _path, value);
        }

        public string Publisher
        {
            get => _publisher;
            set => SetProperty(ref _publisher, value);
        }

        public string IntegrityLevel
        {
            get => _integrityLevel;
            set => SetProperty(ref _integrityLevel, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public bool IsSuspicious
        {
            get => _isSuspicious;
            set => SetProperty(ref _isSuspicious, value);
        }

        public int RiskScore
        {
            get => _riskScore;
            set => SetProperty(ref _riskScore, value);
        }

        public int Depth
        {
            get => _depth;
            set
            {
                if (SetProperty(ref _depth, value))
                {
                    OnPropertyChanged(nameof(DepthMargin));
                }
            }
        }

        public System.Windows.Thickness DepthMargin => new System.Windows.Thickness(Depth * 15, 0, 0, 0);

        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        public ObservableCollection<ProcessItem> Children { get; } = new();

        private static string FormatBytes(long bytes)
        {
            string[] suffix = { "B", "KB", "MB", "GB", "TB" };
            int i;
            double dblSByte = bytes;
            for (i = 0; i < suffix.Length && bytes >= 1024; i++, bytes /= 1024)
            {
                dblSByte = bytes / 1024.0;
            }
            // If i is 0, return bytes without decimal point
            return i == 0 ? $"{dblSByte:0} {suffix[i]}" : $"{dblSByte:0.0} {suffix[i]}";
        }
    }
}
