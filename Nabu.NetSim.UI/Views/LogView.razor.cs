using Nabu.NetSim.UI.ViewModels;
using Splat;

namespace Nabu.NetSim.UI.Views
{
    public partial class LogView
    {
        public LogView()
        {
            //ViewModel = Locator.Current.GetService<LogViewModel>();
            //Task.Run(() => ViewModel!.Refresh());
        }

        protected override void OnInitialized()
        {
            Activated.Subscribe(_ => ViewModel?.Activator.Activate());
            Deactivated.Subscribe(_ => ViewModel?.Activator.Deactivate());
            base.OnInitialized();
        }
    }
}