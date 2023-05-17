using Nabu.NetSim.UI.ViewModels;
using Splat;

namespace Nabu.NetSim.UI.Views
{
    public partial class HomeView
    {
        public HomeView()
        {
            ViewModel = Locator.Current.GetService<HomeViewModel>();
        }
    }
}