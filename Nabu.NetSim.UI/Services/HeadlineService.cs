using CodeHollow.FeedReader;
using System.Reactive.Linq;
using Nabu.Models;

namespace Nabu.NetSim.UI.Services;

public class HeadlineService
{
    const string FeedUrl = "https://www.nabunetwork.com/feed/";
    public IEnumerable<TickerItem> Headlines { get; private set; } = Array.Empty<TickerItem>();

    public HeadlineService()
    {
        Task.Run(GetHeadlines);
        Observable.Interval(TimeSpan.FromMinutes(10))
                  .Subscribe(async _ => await GetHeadlines());
    }

    public async Task GetHeadlines()
    {
        try
        {
            var feed = await FeedReader.ReadAsync(FeedUrl);
            var items = feed.Items.Take(4).Select(i => new TickerItem(i.Title, i.Link));
            Headlines = items;
        }
        catch
        {
            if (Headlines.Any())
                return;

        }
    }
}

