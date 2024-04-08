using Microsoft.AspNetCore.Components;
using Nabu.NetSim.UI.ViewModels;
using Nabu.Settings;
using ReactiveUI.Blazor;

namespace Nabu.NetSim.UI.Views
{
    public partial class AdaptorSettingsView : ReactiveInjectableComponentBase<AdaptorSettingsViewModel>
    {
        public AdaptorSettingsView()
        {
            //ViewModel = Locator.Current.GetService<AdaptorSettingsViewModel>();
        }

        [Parameter]
        public Action<AdaptorSettings> OnClose { get; set; } = (a) => { };

         

        protected override void OnInitialized()
        {
            Activated.Subscribe(_ => ViewModel?.Activator.Activate());
            Deactivated.Subscribe(_ => ViewModel?.Activator.Deactivate());
            base.OnInitialized();
        }
    }
}