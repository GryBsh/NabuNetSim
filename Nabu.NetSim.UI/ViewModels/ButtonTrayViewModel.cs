using Napa;
using ReactiveUI;

namespace Nabu.NetSim.UI.ViewModels
{
    public class ButtonTrayViewModel(HomeViewModel home, IPackageManager packages) : ReactiveObject
    {
        //public LogViewModel LogViewer { get; }
        public HomeViewModel Home { get; } = home;

        public IPackageManager Packages { get; } = packages;

        public bool UpdatesAvailable => Packages.Available.Any();
    }
}