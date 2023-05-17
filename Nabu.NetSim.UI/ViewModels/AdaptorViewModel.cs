using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nabu.NetSim.UI.ViewModels
{


    public class AdaptorViewModel : ReactiveObject
    {
        public HomeViewModel Home { get; }

        public MenuViewModel Menu { get; }

        public AdaptorViewModel(HomeViewModel home, MenuViewModel menu) {
            Home = home;
            Menu = menu;
        }

        public bool IsClient(AdaptorSettings context)
        {
            return context is TCPAdaptorSettings connection && connection.Connection is true;
        }

        public string AdaptorStatus(AdaptorSettings settings)
        {
            return settings.State switch
            {
                ServiceShould.Run => "Running",
                ServiceShould.Restart => "Stopping",
                ServiceShould.Stop => "Stopped",
                _ => "Unknown"
            };
        }
    }
}
