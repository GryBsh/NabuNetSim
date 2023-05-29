using Nabu.NetSim.UI.ViewModels;
using ReactiveUI.Blazor;
using Splat;

namespace Nabu.NetSim.UI.Views
{
    public partial class AdaptorSettingsView : ReactiveInjectableComponentBase<AdaptorSettingsViewModel>
    {
        public AdaptorSettingsView()
        {
            //ViewModel = Locator.Current.GetService<AdaptorSettingsViewModel>();
        }

        protected override void OnInitialized()
        {
            Activated.Subscribe(_ => ViewModel?.Activator.Activate());
            Deactivated.Subscribe(_ => ViewModel?.Activator.Deactivate());
            base.OnInitialized();
        }
    }
}