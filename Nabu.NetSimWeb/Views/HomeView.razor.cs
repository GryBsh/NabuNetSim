using Nabu.NetSimWeb.ViewModels;
using ReactiveUI.Blazor;
using Splat;

namespace Nabu.NetSimWeb.Views;

public partial class HomeView : ReactiveComponentBase<HomeViewModel>
{
    public HomeView()
    {
        ViewModel = Locator.Current.GetService<HomeViewModel>();
    }
}
