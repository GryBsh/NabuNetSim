using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nabu.NetSim.UI.ViewModels
{
    public class LogButtonViewModel : ReactiveObject
    {
        public LogViewModel LogViewer { get; }

        public LogButtonViewModel(LogViewModel logViewer)
        {
            LogViewer = logViewer;
        }


    }
}
