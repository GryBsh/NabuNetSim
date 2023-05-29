using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nabu.NetSim.UI.ViewModels
{
    public class ButtonTrayViewModel : ReactiveObject
    {
        //public LogViewModel LogViewer { get; }
        public HomeViewModel Home { get; }
        public ButtonTrayViewModel(HomeViewModel home)
        {
            Home = home;
        }


    }
}
