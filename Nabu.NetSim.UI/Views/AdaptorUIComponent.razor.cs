using Nabu.NetSim.UI.ViewModels;
using ReactiveUI.Blazor;
using Splat;

namespace Nabu.NetSim.UI.Views;

public partial class AdaptorUIComponent : ReactiveComponentBase<HomeViewModel>
{
    public AdaptorUIComponent()
    {
        ViewModel = Locator.Current.GetService<HomeViewModel>();
    }
}
