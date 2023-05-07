using Nabu.NetSim.UI.ViewModels;
using ReactiveUI.Blazor;
using Splat;

namespace Nabu.NetSim.UI.Views;

public partial class HomeView : ReactiveComponentBase<HomeViewModel>
{
 
    public HomeView()
    {
        ViewModel = Locator.Current.GetService<HomeViewModel>();
    }
}
