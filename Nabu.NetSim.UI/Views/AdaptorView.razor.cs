using Nabu.NetSim.UI.ViewModels;
using ReactiveUI.Blazor;
using Splat;

namespace Nabu.NetSim.UI.Views;

public partial class AdaptorView : ReactiveComponentBase<HomeViewModel>
{
    void OnSourceChanged(AdaptorSettings settings, string value)
    {
        Task.Run(() =>
        {
            settings.Source = value;
            settings.Image = string.Empty;
        });
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
    }
}
