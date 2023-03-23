using System.Collections.ObjectModel;

namespace Nabu;
public interface IRetroNetChatClient
{
    ObservableCollection<string> Messages { get; }
    void Dispose();
    void Send(string message);
}
