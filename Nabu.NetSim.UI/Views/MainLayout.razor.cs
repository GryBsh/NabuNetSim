using Nabu.NetSim.UI.ViewModels;
using Splat;

namespace Nabu.NetSim.UI.Views
{
    public partial class MainLayout
    {
        public MainLayout()
        {
            ViewModel = Locator.Current.GetService<MainLayoutViewModel>();
        }
    }
}