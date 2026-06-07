using System.Windows.Controls;
using ProcessExplorerPro.ViewModels;

namespace ProcessExplorerPro.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
            DataContext = new SettingsViewModel();
        }
    }
}
