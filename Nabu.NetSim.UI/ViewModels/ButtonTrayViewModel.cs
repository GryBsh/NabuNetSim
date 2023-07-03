using ReactiveUI;

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