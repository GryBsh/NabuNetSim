using Nabu.NetSim.UI.ViewModels;
using ReactiveUI.Blazor;
using Splat;

namespace Nabu.NetSim.UI.Views
{
    public partial class StatusView : ReactiveInjectableComponentBase<StatusViewModel>
    {
        public StatusView()
        {
            //ViewModel = Locator.Current.GetService<StatusViewModel>();
            //ViewModel.Activator.Activate();
        }

        protected override void OnInitialized()
        {
            Activated.Subscribe(_ => ViewModel?.Activator.Activate());
            Deactivated.Subscribe(_ => ViewModel?.Activator.Deactivate());
            base.OnInitialized();
        }
    }
}