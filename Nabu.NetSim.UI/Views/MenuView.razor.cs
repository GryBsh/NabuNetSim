using Nabu.NetSim.UI.ViewModels;
using Splat;

namespace Nabu.NetSim.UI.Views
{
    public partial class MenuView
    {
        public MenuView()
        {
            ViewModel = Locator.Current.GetService<MenuViewModel>();
        }
    }
}