using Nabu.NetSim.UI.ViewModels;
using ReactiveUI.Blazor;

namespace Nabu.NetSim.UI.Views
{
    public partial class FilesView : ReactiveInjectableComponentBase<FilesViewModel>
    {
        public FilesView()
        {
            //ViewModel = Locator.Current.GetService<FilesViewModel>();
        }

        protected override void OnInitialized()
        {
            Activated.Subscribe(_ => ViewModel?.Activator.Activate());
            Deactivated.Subscribe(_ => ViewModel?.Activator.Deactivate());
            base.OnInitialized();
        }
    }
}