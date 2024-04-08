using Nabu.Models;

namespace Nabu.Services;

public interface IHeadlineService
{
    IEnumerable<TickerItem> Headlines { get; }
}