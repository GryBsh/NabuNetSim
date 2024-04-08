using Blazorise;
using Gry;
using Gry.Adapters;
using Nabu.Network;
using Nabu.Settings;
using ReactiveUI;

namespace Nabu.NetSim.UI.ViewModels
{
    public class EmulatorButtonViewModel : ReactiveObject, IActivatableViewModel
    {
        public EmulatorButtonViewModel(GlobalSettings settings, ProcessService process)
        {
            Settings = settings;
            Process = process;
        }

        public ViewModelActivator Activator { get; } = new();
        public ProcessService Process { get; }
        public GlobalSettings Settings { get; }
        private CancellationToken? EmulatorProcess { get; set; }

        public bool RunAvailable(AdaptorSettings context)
        => ShouldRun(context) &&
            context.Adapter?.State is AdapterState.Running && (
                Process.IsRunning(Settings.EmulatorPath) is null ||
                (EmulatorProcess is null || EmulatorProcess.Value.IsCancellationRequested)
            );

        public void RunEmulator(AdaptorSettings context)
        {
            if (!RunAvailable(context))
                return;

            EmulatorProcess = Process.Start(Settings.EmulatorPath);
        }

        public Visibility RunVisibility(AdaptorSettings context, bool changed)
        {
            return ShouldRun(context) && changed ? Visibility.Visible : Visibility.Invisible;
        }

        public bool ShouldRun(AdaptorSettings context)
        {
            if (context.Type is AdapterType.TCP && Model.As<AdapterDefinition, TCPAdaptorSettings>(context) is var t)
            {
                return !t.Connection && Settings.EmulatorPath != string.Empty;
            }
            return false;
        }
    }
}