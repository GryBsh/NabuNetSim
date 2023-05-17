using Nabu.NetSim.UI.ViewModels;
using Splat;

namespace Nabu.NetSim.UI.Views
{
    public partial class StatusView
    {
        public StatusView()
        {
            ViewModel = Locator.Current.GetService<StatusViewModel>();
        }
    }
}