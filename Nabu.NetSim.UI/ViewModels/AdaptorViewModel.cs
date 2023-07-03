using Blazorise;
using Nabu.Services;
using ReactiveUI;
using System.Diagnostics;

namespace Nabu.NetSim.UI.ViewModels
{
    public class AdaptorViewModel : ReactiveObject
    {
        public AdaptorViewModel(HomeViewModel home, AdaptorSettingsViewModel menu, Settings settings, ProcessService process, ISimulation simulation)
        {
            Home = home;
            Menu = menu;
            Settings = settings;
            Process = process;
            Simulation = simulation;
        }

        public HomeViewModel Home { get; }
        public AdaptorSettingsViewModel Menu { get; }
        public ProcessService Process { get; }
        public Settings Settings { get; }
        public ISimulation Simulation { get; }
        private CancellationToken? EmulatorProcess { get; set; }

        public string AdaptorButtonText(AdaptorSettings settings)
        {
            return settings.State switch
            {
                ServiceShould.Run => "Stop Adaptor",
                _ => "Start Adaptor"
            };
        }

        public string AdaptorStatus(AdaptorSettings settings)
        {
            return settings.State switch
            {
                ServiceShould.Run => "Running",
                ServiceShould.Restart => "Stopping",
                ServiceShould.Stop => "Stopped",
                _ => "Unknown"
            };
        }

        public bool RunAvailable(AdaptorSettings context)
            => ShouldRun(context) &&
                context.Running &&
                (EmulatorProcess is null || EmulatorProcess.Value.IsCancellationRequested);

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

        public void ToggleAdaptor(AdaptorSettings settings)
        {
            if (settings is TCPAdaptorSettings connection && connection.Connection)
                connection.ListenTask?.Cancel();
            else
                Simulation?.ToggleAdaptor(settings);

            this.RaisePropertyChanged(nameof(RunAvailable));
        }
    }
}