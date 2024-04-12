
using Nabu.NetSim.UI.Models;
using Nabu.NetSim.UI.Services;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Nabu.NetSim.UI.ViewModels;

public class LogViewModel : ReactiveObject, IActivatableViewModel
{
    public ViewModelActivator Activator { get; }
    //private ILogService LogService { get; }

    ILogTailingService LogService { get; }

    public LogViewModel(
        ILogTailingService logService,
        HomeViewModel home
    )
    {
        Home = home;        //Repository = repository;        Activator = new();
        LogService = logService;
        //LogService.RefreshMode = RefreshMode.None;
        this.WhenActivated(
            disposables =>
            {
                Observable
                    .Interval(TimeSpan.FromSeconds(5), RxApp.TaskpoolScheduler)
                    .Subscribe(
                        _ =>
                        {
                            NotifyChange();
                            //GC.Collect();
                        }
                    ).DisposeWith(disposables);

                Home.ObservableForProperty(h => h.VisiblePage)
                    .Subscribe(
                        visible =>
                        {
                            if (visible.Value is VisiblePage.Logs)
                            {
                                ToggleVisible();
                            }
                        }
                    ).DisposeWith(disposables);
            }

        );
    }

    public HomeViewModel Home { get; }    //IRepository<LogEntry> Repository { get; }    private void NotifyChange()
    {
        this.RaisePropertyChanged(nameof(PageCount));
        this.RaisePropertyChanged(nameof(CurrentPage));
        this.RaisePropertyChanged(nameof(PageSize));
    }

    public bool LogVisible { get; set; }

    int page = 1;
    public int Page
    {
        get => page;
        set
        {
            page = value;
            this.RaisePropertyChanged(nameof(Page));
            this.RaisePropertyChanged(nameof(CurrentPage));
            this.RaisePropertyChanged(nameof(Search));
        }
    }

    int pageSize = 100;
    public int PageSize
    {   
        get => pageSize;
        set
        {
            pageSize = value;
            Page = 1;
            this.RaisePropertyChanged(nameof(PageSize));
            this.RaisePropertyChanged(nameof(PageCount));
            this.RaisePropertyChanged(nameof(CurrentPage));
        }
    }

    private string search = string.Empty;

    public string Search
    {
        get
        {
            return search;
        }
        set
        {
            search = value ?? string.Empty;

            this.RaisePropertyChanged(nameof(Search));
            //this.RaisePropertyChanged(nameof(CurrentPage));
        }
    }

    public int PageCount
    {
        get
        {
            /*
            var total = LogService.Count;
            if (total is 0) return 0;
            var rounded = Math.Ceiling((double)total / PageSize);
            var count = (int)rounded;
            return count < 1 ? 1 : count;
            */
            return LogService.PageCount(PageSize);
        }
    }

    public void Refresh()
    {
        if (LogVisible is false) return;
    }

    /*
    private LogEntry Highlight(LogEntry e)
    {
        var term = Search.ToLowerInvariant();
        if (
            Search != string.Empty && (
                e.Timestamp.ToString().Contains(term, StringComparison.InvariantCultureIgnoreCase) ||
                e.Name.Contains(term, StringComparison.InvariantCultureIgnoreCase) ||
                e.Message.Contains(term, StringComparison.InvariantCultureIgnoreCase)
            )
        ) e.Highlight = true;
        else e.Highlight = false;

        return e;
    }
    */

    //private (int, int, int, ICollection<IGrouping<LogKey, LogEntry>>)? PageCache { get; set; }
    private (int, int, int, ICollection<string>)? PageCache { get; set; }

    //public ICollection<IGrouping<LogKey, LogEntry>> CurrentPage
    public ICollection<string> CurrentPage
    {
        get
        {
            //var count = LogService.Count;
            var count = LogService.Count();
            //if (PageCache is not null)
            //{
            //    var (page, total, size, entries) = PageCache.Value;
            //    if (page == Page && total == count && size == PageSize)
            //        return entries;
            //}

            var newPage =
                LogService
                    .GetPage(Page, PageSize)
                    //.GroupBy(l => l.Key)
                    .ToList();

            //PageCache = (
            //    Page,
            //    PageSize,
            //    count,
            //    newPage
            //);

            return newPage;
        }
    }

    public void SetPage(string page) => Page = int.Parse(page);

    public void PageBack()
    {
        if (Page is 1) return;

        Page -= 1;
        this.RaisePropertyChanged(nameof(Page));
        this.RaisePropertyChanged(nameof(CurrentPage));
    }

    public void PageForward()
    {
        if (Page == PageCount || PageCount is 0) return;

        Page += 1;
        this.RaisePropertyChanged(nameof(Page));
        this.RaisePropertyChanged(nameof(CurrentPage));
    }

    public bool IsActivePage(int page)
        => Page == page || page is 0;

    public void ClearSearch()
    {
        Search = string.Empty;
        this.RaisePropertyChanged(nameof(Search));
    }

    public void ToggleVisible()
    {
        this.RaisePropertyChanged(nameof(Search));
        this.RaisePropertyChanged(nameof(PageCount));
        this.RaisePropertyChanged(nameof(CurrentPage));
        this.RaisePropertyChanged(nameof(LogVisible));
    }
}