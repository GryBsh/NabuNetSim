using Nabu.NetSimWeb.ViewModels;
using ReactiveUI.Blazor;
using Splat;

namespace Nabu.NetSimWeb.Views;

public partial class AdaptorView : ReactiveComponentBase<HomeViewModel>
{
    void OnSourceChanged(AdaptorSettings settings, string value)
    {
        Task.Run(() => {
            settings.Source = value;
            settings.Image = string.Empty;
        });
    }
    
    protected override void OnInitialized()
    {
        base.OnInitialized();
    }
}
