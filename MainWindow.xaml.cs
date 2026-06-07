using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ProcessExplorerPro.Views;

namespace ProcessExplorerPro
{
    public partial class MainWindow : Window
    {
        private DashboardView? _dashboardView;
        private ProcessesView? _processesView;
        private ServicesView? _servicesView;
        private NetworkView? _networkView;
        private PerformanceView? _performanceView;
        private SettingsView? _settingsView;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            NavigateToTag("Dashboard");
        }

        private void TitleBarGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void NavigationButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag != null)
            {
                NavigateToTag(rb.Tag.ToString() ?? string.Empty);
            }
        }

        private void NavigateToTag(string tag)
        {
            switch (tag)
            {
                case "Dashboard":
                    _dashboardView ??= new DashboardView();
                    ContentFrame.Navigate(_dashboardView);
                    break;
                case "Processes":
                    _processesView ??= new ProcessesView();
                    ContentFrame.Navigate(_processesView);
                    break;
                case "Services":
                    _servicesView ??= new ServicesView();
                    ContentFrame.Navigate(_servicesView);
                    break;
                case "Network":
                    _networkView ??= new NetworkView();
                    ContentFrame.Navigate(_networkView);
                    break;
                case "Performance":
                    _performanceView ??= new PerformanceView();
                    ContentFrame.Navigate(_performanceView);
                    break;
                case "Settings":
                    _settingsView ??= new SettingsView();
                    ContentFrame.Navigate(_settingsView);
                    break;
            }
        }
    }
}
