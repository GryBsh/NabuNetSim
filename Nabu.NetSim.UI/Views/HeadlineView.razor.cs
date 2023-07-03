using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Net.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using Nabu.NetSim.UI.Views;
using Nabu.NetSim.UI.ViewModels;
using ReactiveUI.Blazor;
using Blazorise;
using CodeHollow.FeedReader;
using ReactiveUI;
using Nabu.Adaptor;
using Nabu.NetSim.UI.Models;
using Nabu.Models;
using Splat;
using System.Web;

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