using ReactiveUI;

namespace Nabu.NetSimWeb.ViewModels;

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
