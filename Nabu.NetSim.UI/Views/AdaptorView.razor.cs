using Microsoft.AspNetCore.Components;
using Nabu.NetSim.UI.ViewModels;
using ReactiveUI.Blazor;

namespace Nabu.NetSim.UI.Views
{
    public partial class AdaptorView : ReactiveInjectableComponentBase<AdaptorViewModel>
    {
        public AdaptorView()
        {
            //ViewModel = Locator.Current.GetService<AdaptorViewModel>();
        }

        [Parameter]
        public IEnumerable<AdaptorSettings>? Adaptors { get; set; }

        private ICollection<AdaptorSettings> AdaptorList => Adaptors?.ToArray() ?? Array.Empty<AdaptorSettings>();

        protected override void OnInitialized()
        {
            Activated.Subscribe(_ => ViewModel?.Activator.Activate());
            Deactivated.Subscribe(_ => ViewModel?.Activator.Deactivate());
            base.OnInitialized();
        }
    }
}