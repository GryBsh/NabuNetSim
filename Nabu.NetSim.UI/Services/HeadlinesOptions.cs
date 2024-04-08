using Gry;
using Lgc;

namespace Nabu.NetSim.UI.Services;
public record HeadlineFeed{    public string? Name { get; set; }    public string? Url { get; set; }}
public record HeadlinesOptions : Model, IDependencyOptions
{
    public List<HeadlineFeed> Feeds { get; set; } = [];
}
