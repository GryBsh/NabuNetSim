using Blazorise;
using Nabu.Models;
using Nabu.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nabu.NetSim.UI.ViewModels
{
    public class HeadlineViewModel : ReactiveObject, IActivatableViewModel
    {
        public HeadlineViewModel(IHeadlineService news)
        {
            News = news;

            this.WhenActivated(
            disposables =>
                {
                    Observable.Interval(TimeSpan.FromMinutes(10), RxApp.TaskpoolScheduler)
                        .Subscribe(_ => GetHeadlines())
                        .DisposeWith(disposables);
                }
            );
        }

        public ViewModelActivator Activator { get; } = new();
        public Visibility CollapsedVisibility => NewsExpanded ? Visibility.Invisible : Visibility.Visible;
        public Visibility ExpandedVisibility => NewsExpanded ? Visibility.Visible : Visibility.Invisible;
        public ICollection<TickerItem> Headlines => News.Headlines.ToList();
        public bool NewsExpanded { get; private set; }
        private IHeadlineService News { get; }

        public void ExpandCollapseNews()
        {
            NewsExpanded = !NewsExpanded;
            this.RaisePropertyChanged(nameof(NewsExpanded));
            this.RaisePropertyChanged(nameof(ExpandedVisibility));
            this.RaisePropertyChanged(nameof(CollapsedVisibility));
            this.RaisePropertyChanged(nameof(Headlines));
        }

        public void GetHeadlines()
        {
            this.RaisePropertyChanged(nameof(Headlines));
        }
    }
}