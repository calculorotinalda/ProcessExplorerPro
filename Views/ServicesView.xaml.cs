using System.Windows.Controls;
using ProcessExplorerPro.ViewModels;

namespace ProcessExplorerPro.Views
{
    public partial class ServicesView : UserControl
    {
        public ServicesView()
        {
            InitializeComponent();
            DataContext = new ServicesViewModel();
        }
    }
}
