using ProcessExplorerPro.Helpers;

namespace ProcessExplorerPro.Models
{
    public class ServiceItem : ViewModelBase
    {
        private string _name = string.Empty;
        private string _displayName = string.Empty;
        private int _pid;
        private string _status = "Stopped";
        private string _startType = "Manual";
        private string _logOnAs = "LocalSystem";
        private string _description = string.Empty;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, value);
        }

        public int Pid
        {
            get => _pid;
            set => SetProperty(ref _pid, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public string StartType
        {
            get => _startType;
            set => SetProperty(ref _startType, value);
        }

        public string LogOnAs
        {
            get => _logOnAs;
            set => SetProperty(ref _logOnAs, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }
    }
}
