using System.Windows.Controls;
using ProcessExplorerPro.ViewModels;

namespace ProcessExplorerPro.Views
{
    public partial class NetworkView : UserControl
    {
        private readonly NetworkViewModel _viewModel;

        public NetworkView()
        {
            InitializeComponent();
            _viewModel = new NetworkViewModel();
            DataContext = _viewModel;

            Unloaded += NetworkView_Unloaded;
        }

        private void NetworkView_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            _viewModel.StopTimer();
        }
    }
}
