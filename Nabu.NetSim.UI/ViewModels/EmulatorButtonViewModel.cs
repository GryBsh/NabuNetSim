using Blazorise;
using Nabu.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nabu.NetSim.UI.ViewModels
{
    public class EmulatorButtonViewModel : ReactiveObject, IActivatableViewModel
    {
        public EmulatorButtonViewModel(Settings settings, ProcessService process)
        {
            Settings = settings;
            Process = process;
        }

        public ViewModelActivator Activator { get; } = new();
        public ProcessService Process { get; }
        public Settings Settings { get; }
        private CancellationToken? EmulatorProcess { get; set; }

        public bool RunAvailable(AdaptorSettings context)
        => ShouldRun(context) &&
            context.Running && (
                Process.IsRunning(Settings.EmulatorPath) is null ||
                (EmulatorProcess is null || EmulatorProcess.Value.IsCancellationRequested)
            );

        public void RunEmulator(AdaptorSettings context)
        {
            if (!RunAvailable(context))
                return;

            EmulatorProcess = Process.Start(Settings.EmulatorPath);
        }

        public Visibility RunVisibility(AdaptorSettings context)
        {
            return ShouldRun(context) ? Visibility.Visible : Visibility.Invisible;
        }

        public bool ShouldRun(AdaptorSettings context)
        {
            return (context is TCPAdaptorSettings t && !t.Connection && Settings.EmulatorPath != string.Empty);
        }
    }
}