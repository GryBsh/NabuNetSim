using ReactiveUI;

namespace Nabu.NetSim.UI.ViewModels;

public class MainLayoutViewModel : ReactiveObject
{
    public bool SidebarVisible { get; set; } = true;
    public bool TopbarInvisible { get; set; } = true;
    public void ToggleSidebar()
    {
        SidebarVisible = !SidebarVisible;
        TopbarInvisible = SidebarVisible;
    }
}
