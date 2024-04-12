using Blazorise;
using CodeHollow.FeedReader;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nabu.Logs;
using Nabu.Models;
using Nabu.NetSim.UI.Services;
using Nabu.Services;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Xml.Linq;

namespace Nabu.NetSim.UI.ViewModels;



public class HeadlineViewModel : ReactiveObject, IActivatableViewModel
{
    public HeadlineViewModel(
        ILogger<HeadlineService> logger,
        IOptionsMonitor<HeadlinesOptions> options
    )
    {
        

        this.WhenActivated(
        disposables =>
            {
                Observable.Interval(TimeSpan.FromMinutes(10))
                    .Subscribe(async _ => await GetHeadlines())
                    .DisposeWith(disposables);
            }
        );
        Task.Run(GetHeadlines);
        Logger = logger;
        Options = options;
    }

    public ViewModelActivator Activator { get; } = new();
    public Visibility CollapsedVisibility => NewsExpanded ? Visibility.Invisible : Visibility.Visible;
    public Visibility ExpandedVisibility => NewsExpanded ? Visibility.Visible : Visibility.Invisible;
    public ObservableCollection<TickerItem> Headlines { get; set; } = new();
    public bool NewsExpanded { get; private set; }

    ILogger<HeadlineService> Logger { get; }
    IOptionsMonitor<HeadlinesOptions> Options { get; }

    private HeadlineFeed[] Feeds => [.. Options.CurrentValue.Feeds];

    private async Task GetHeadlines()
    {
        var items = new List<TickerItem>();
        try
        {
            foreach (var f in Feeds)
            {
                XNamespace dcNamespace = "http://purl.org/dc/elements/1.1/";
                string? GetAuthor(FeedItem i)
                {
                    return i.SpecificItem.Element.Element(dcNamespace + "creator")?.Value;
                }

                TickerItem Item(string feedName, FeedItem i)
                {
                    return new TickerItem(
                        $"{feedName}: {i.Title}", 
                        i.Link, 
                        i.Description
                         .Replace("<div>", string.Empty)
                         .Replace("</div>", string.Empty), 
                        i.Content
                    );
                }
                

                var feed = await FeedReader.ReadAsync(f.Url);

                if (f.Name?.Equals("youtube", StringComparison.OrdinalIgnoreCase) is true)
                {
                    var authors = feed.Items.Select(GetAuthor).Distinct();
                    foreach (var auth in authors) {
                        var i = feed.Items.First(i => GetAuthor(i) == auth);
                        items.Add(
                            Item(f.Name, i)
                        ); ;
                    }
                }
                else
                {
                    foreach (var i in feed.Items.Take(4))
                        items.Add(
                            Item(f.Name, i)
                        );
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(string.Empty, ex);
        }

        Headlines = new(items.OrderBy(o => Random.Shared.Next()));
        this.RaisePropertyChanged(nameof(Headlines));
    }

    public void ExpandCollapseNews()
    {
        NewsExpanded = !NewsExpanded;
        this.RaisePropertyChanged(nameof(NewsExpanded));
        this.RaisePropertyChanged(nameof(ExpandedVisibility));
        this.RaisePropertyChanged(nameof(CollapsedVisibility));
        this.RaisePropertyChanged(nameof(Headlines));
    }

    
}