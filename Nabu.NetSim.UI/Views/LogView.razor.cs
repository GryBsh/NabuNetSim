using Nabu.NetSim.UI.ViewModels;
using Splat;

namespace Nabu.NetSim.UI.Views
{
    public partial class LogView
    {
        public LogView()
        {
            ViewModel = Locator.Current.GetService<LogViewModel>();
            Task.Run(() => ViewModel!.Refresh());
        }
    }
}