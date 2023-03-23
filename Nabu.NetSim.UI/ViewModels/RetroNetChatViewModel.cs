using ReactiveUI;
using System.Collections.ObjectModel;

namespace Nabu.NetSim.UI.ViewModels;

public class RetroNetChatViewModel : ReactiveObject
{
    

    readonly CancellationTokenSource Cancel = new();
    readonly IRetroNetChatClient Client;

    public ObservableCollection<string> ChatLog { get; }

    public RetroNetChatViewModel(IRetroNetChatClient client)
    {
        Client = client;
        ChatLog = Client.Messages;
    }
    
    public string Message { get; set; } = string.Empty;
    public void Send()
    {
        Client.Send(Message);
        Message = string.Empty;
        this.RaisePropertyChanged(nameof(Message)); 
    }


}
