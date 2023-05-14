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
using Nabu.NetSim.UI.ViewModels;
using ReactiveUI.Blazor;
using Blazorise;
using CodeHollow.FeedReader;
using ReactiveUI;
using Nabu.Adaptor;
using Nabu.NetSim.UI.Models;
using Nabu.NetSim.UI.Views;
using Splat;

namespace Nabu.NetSim.UI.Views
{
    public partial class LogView
    {
        public LogView()
        {
            ViewModel = Locator.Current.GetService<LogViewModel>();
            Task.Run(() => ViewModel!.Refresh());
        }
    }
}