﻿using Blazorise;
using LiteDB;
using Nabu.Models;
using Nabu.NetSim.UI.Services;
using ReactiveUI;
using System.Reactive.Linq;

namespace Nabu.NetSim.UI.ViewModels
{

    public class LogViewModel : ReactiveObject
    {
        LogService LogService { get; }
        public LogViewModel(
            LogService logService,
            HomeViewModel home
        )
        {
            Home = home;
            //Repository = repository;

            LogService = logService;
            Observable.Interval(TimeSpan.FromSeconds(10))
                 .Subscribe(_ =>
                 {
                     Refresh();
                     //GC.Collect();
                 });
        }
        HomeViewModel Home { get; set; }
        //IRepository<LogEntry> Repository { get; }

        void NotifyChange()
        {
            this.RaisePropertyChanged(nameof(PageCount));
            this.RaisePropertyChanged(nameof(CurrentPage));
            Home.RaisePropertyChanged(nameof(Home.Visible));
        }

        bool logVisible = false;
        public bool LogVisible { 
            get { return logVisible; } 
            set {
                logVisible = value;
                Home.Visible = !logVisible;
                LogService.Update = value;
                NotifyChange();
            }
        }
        public string ButtonText { get => LogVisible ? "Hide Log" : "Show Log"; }

        public Visibility Visibility => LogVisible ? Visibility.Visible : Visibility.Invisible;

        //public List<LogEntry> Entries { get; private set; } = new List<LogEntry>();

        public string DateTimeFormat { get; } = "yyyy-MM-dd HH:mm:ss.ffff";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 100;

        string search = string.Empty;

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
                this.RaisePropertyChanged(nameof(CurrentPage));
            }
        }

        public int PageCount
        {
            get
            {
                var total = LogService.Entries.Count;
                if (total is 0) return 0;
                var count = (int)Math.Ceiling((double)(total / PageSize));
                return count < 1 ? 1 : count;
            }
        }

        public void Refresh()
        {

            //Entries = Repository.SelectAll().OrderByDescending(e => e.Timestamp).ToList();

            if (LogVisible is false) return;
            //{
            //    LogService.Pause();
            //    return;
            //}
            //LogService.Refresh();
            this.RaisePropertyChanged(nameof(PageCount));
            this.RaisePropertyChanged(nameof(CurrentPage));
        }

        LogEntry Highlight(LogEntry e)
        {
            var term = Search.ToLowerInvariant();
            if (
                Search != string.Empty && (
                    e.Timestamp.ToString(DateTimeFormat).ToLowerInvariant().Contains(term) ||
                    e.Name.ToLowerInvariant().Contains(term) ||
                    e.Message.ToLowerInvariant().Contains(term)
                )
            ) e.Highlight = true;
            else e.Highlight = false;

            return e;
        }

        public IEnumerable<LogEntry> CurrentPage
        {
            get
            {
                return LogService
                        .Entries
                        .Skip((Page - 1) * PageSize)
                        .Take(PageSize)
                        /*
                         LogService.Repository.Query<ILiteQueryable<LogEntry>>(
                            q => q.OrderByDescending(e => e.Timestamp)
                                  .Skip((Page - 1) * PageSize)
                                  .Limit(PageSize)
                                  .ToEnumerable()
                        )*/
                        .Select(Highlight);
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
            LogVisible = !LogVisible;
            //this.RaisePropertyChanged(nameof(Search));
            this.RaisePropertyChanged(nameof(PageCount));
            //this.RaisePropertyChanged(nameof(Entries));
            this.RaisePropertyChanged(nameof(CurrentPage));
            this.RaisePropertyChanged(nameof(LogVisible));
        }

        
    }
}
