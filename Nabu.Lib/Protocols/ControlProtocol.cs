using Gry;
using Gry.Protocols;
using Microsoft.Extensions.Logging;
using Nabu.Settings;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Nabu.Protocols;



public partial class ControlProtocol(
    ILogger<ControlProtocol> logger, 
    IEnumerable<ICtrlMessageHandler<AdaptorSettings, byte>> handlers
) : Protocol<AdaptorSettings>(logger)
{

    public override byte[] Messages => [0xEF];

    public override byte Version => 0x01;

    protected override async Task Handle(byte _, CancellationToken cancel)
    {

        Logger.LogDebug("Start");
        var request = CtrlRequest.FromBuffer(
            new([
                Read(), 
                Read(), 
                ..ReadFrame().Data.Span
            ])
        );
        
        var data = request.Data!.Value;

        var handler = handlers.FirstOrDefault(
            h => h.Types.Any(t => t == (byte)request.Type)
        );

        Logger.LogInformation("Type: {}, Command: {}", Format((byte)request.Type), Enum.GetName(request.Command));

        if (handler is null)
        {
            WritePrefixedFrame(
                CtrlTypeId.Error, 
                Error((byte)CtrlErrorCode.UnknownType, "Unknown Type")
            );
            return;
        }

        switch (request.Command)
        {
            case CtrlCommand.Set:
                var item = await Task.Run(
                    async () => await handler.Set(this, (byte)request.Type, data)
                );
                WritePrefixedFrame(
                    CtrlTypeId.Item, 
                    CtrlItem.ToBuffer(item)
                );
                break;
            case CtrlCommand.List:
                byte i = 0;
                var listType = (CtrlListType)data.Span[i++];
                byte pageSize = 0;
                byte page = 0;

                if (listType is CtrlListType.Paged)
                {
                    pageSize = data.Span[i++];
                    page = data.Span[i++];
                }

                Logger.LogInformation("List Type: {}, Page Size: {}, Page: {}", Enum.GetName(listType), pageSize, page);

                // Ensuring we await a real task, not a value task
                var list = await Task.Run(
                    async () => await handler.List(this, (byte)request.Type, data[i..])
                );

                if (!list.Any())
                {
                    list = [new CtrlItem(CtrlItemType.None, CtrlValueType.None, "No Items", null)];
                }
                if (list.Any(i => i.Type is CtrlItemType.Error))
                {
                    list = list.Where(i => i.Type is CtrlItemType.Error);
                }


                if (pageSize > 0)
                {
                    var (count, pg) = Page(pageSize, page, list);
                    Logger.LogInformation("Items: {}", count);
                    Logger.LogDebug("Page: {}", FormatSeparated(pg.ToArray()));
                    WritePrefixedFrame(CtrlTypeId.List, pg);
                    break;
                }
                var lst = List(list);
                Logger.LogInformation("List: {}", FormatSeparated(lst.ToArray()));
                WritePrefixedFrame(CtrlTypeId.List, lst);
                break;
        }

        WritePrefixedFrame(CtrlTypeId.Error, Error((byte)CtrlErrorCode.InvalidCommand, "Unknown Command"));
    }
}
