using CodeHollow.FeedReader;
using Nabu.Models;
using Nabu.Network;
using Nabu.Services;
using System.Reactive.Linq;

namespace Nabu.NetSim.UI.Services;

public class HeadlineService : IHeadlineService
{
    private const string FeedUrl = "https://www.nabunetwork.com/feed/";
    public IEnumerable<TickerItem> Headlines { get; private set; } = Array.Empty<TickerItem>();
    public ILog<HeadlineService> Log { get; }
    public IHttpCache Http { get; }

    public HeadlineService(ILog<HeadlineService> log, IHttpCache http)
    {
        Task.Run(GetHeadlines);
        Observable.Interval(TimeSpan.FromMinutes(10))
                  .Subscribe(async _ => await GetHeadlines());
        Log = log;
        Http = http;
    }

    private async Task GetHeadlines()
    {
        try
        {
            //var cachedFeed = await Http.GetFile(FeedUrl);
            var feed = await FeedReader.ReadAsync(FeedUrl);
            var items = feed.Items.Take(4).Select(i => new TickerItem(i.Title, i.Link, i.Description, i.Content));
            Headlines = items;
        }
        catch (Exception ex) 
        {
            Log.WriteError(null, ex);
        }
    }
}