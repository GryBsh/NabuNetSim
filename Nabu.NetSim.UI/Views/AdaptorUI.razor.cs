using Nabu.NetSim.UI.ViewModels;
using ReactiveUI.Blazor;
using Splat;

namespace Nabu.NetSim.UI.Views;

public partial class AdaptorUI : ReactiveComponentBase<HomeViewModel>
{
 
    public AdaptorUI()
    {
        ViewModel = Locator.Current.GetService<HomeViewModel>();
    }
}
