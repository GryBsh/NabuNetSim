namespace Nabu.NetSim.UI.Views
{
    public partial class PackagesView
    {
        public PackagesView()
        {
            //ViewModel = Locator.Current.GetService<PackagesViewModel>();
        }

        protected override void OnInitialized()
        {
            Activated.Subscribe(_ => ViewModel?.Activator.Activate());
            Deactivated.Subscribe(_ => ViewModel?.Activator.Deactivate());
            base.OnInitialized();
        }
    }
}