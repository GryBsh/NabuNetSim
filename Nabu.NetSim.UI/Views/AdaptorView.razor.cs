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

namespace Nabu.NetSim.UI.Views
{
    public partial class AdaptorView : ReactiveInjectableComponentBase<AdaptorViewModel>
    {
        [Parameter]
        public IEnumerable<AdaptorSettings>? Adaptors { get; set; }
        ICollection<AdaptorSettings> AdaptorList => Adaptors?.ToArray() ?? Array.Empty<AdaptorSettings>();
        public AdaptorView()
        {
            //ViewModel = Locator.Current.GetService<AdaptorViewModel>();
        }
    }
}