using ProcessExplorerPro.Helpers;

namespace ProcessExplorerPro.Models
{
    public class NetworkItem : ViewModelBase
    {
        private string _protocol = "TCP";
        private string _localAddress = string.Empty;
        private int _localPort;
        private string _remoteAddress = string.Empty;
        private int _remotePort;
        private string _state = string.Empty;
        private int _pid;
        private string _processName = "Unknown";

        public string Protocol
        {
            get => _protocol;
            set => SetProperty(ref _protocol, value);
        }

        public string LocalAddress
        {
            get => _localAddress;
            set => SetProperty(ref _localAddress, value);
        }

        public int LocalPort
        {
            get => _localPort;
            set => SetProperty(ref _localPort, value);
        }

        public string RemoteAddress
        {
            get => _remoteAddress;
            set => SetProperty(ref _remoteAddress, value);
        }

        public int RemotePort
        {
            get => _remotePort;
            set => SetProperty(ref _remotePort, value);
        }

        public string State
        {
            get => _state;
            set => SetProperty(ref _state, value);
        }

        public int Pid
        {
            get => _pid;
            set => SetProperty(ref _pid, value);
        }

        public string ProcessName
        {
            get => _processName;
            set => SetProperty(ref _processName, value);
        }

        public string LocalEndpoint => $"{LocalAddress}:{LocalPort}";
        public string RemoteEndpoint => Protocol == "UDP" ? "*:*" : $"{RemoteAddress}:{RemotePort}";
    }
}
