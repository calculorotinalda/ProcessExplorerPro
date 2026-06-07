using ProcessExplorerPro.Helpers;

namespace ProcessExplorerPro.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private bool _isDarkTheme = true;
        private string _refreshRate = "Normal (2s)";
        private bool _isAiEnabled = true;
        private bool _isVirusTotalEnabled = true;

        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set => SetProperty(ref _isDarkTheme, value);
        }

        public string RefreshRate
        {
            get => _refreshRate;
            set => SetProperty(ref _refreshRate, value);
        }

        public bool IsAiEnabled
        {
            get => _isAiEnabled;
            set => SetProperty(ref _isAiEnabled, value);
        }

        public bool IsVirusTotalEnabled
        {
            get => _isVirusTotalEnabled;
            set => SetProperty(ref _isVirusTotalEnabled, value);
        }

        public List<string> RefreshRateOptions { get; } = new()
        {
            "Rápido (0.5s)",
            "Normal (2s)",
            "Lento (5s)",
            "Pausado"
        };
    }
}
