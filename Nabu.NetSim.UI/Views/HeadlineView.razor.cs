namespace Nabu.NetSim.UI.Views
{
    public partial class HeadlineView
    {
        protected override void OnInitialized()
        {
            Activated.Subscribe(_ => ViewModel?.Activator.Activate());
            Deactivated.Subscribe(_ => ViewModel?.Activator.Deactivate());
            base.OnInitialized();
        }
    }
}