using Microsoft.Extensions.Logging;
using Nabu.Adaptor;

namespace Nabu.Network;

public class RetroNetProtocol : Protocol
{
    public RetroNetProtocol(ILogger logger) : base(logger)
    {
        
    }

    public override byte[] Commands { get; } = new byte[] {
        RetroNetCommands.FileCopy,
        RetroNetCommands.FileDelete,
        RetroNetCommands.FileHandleAppend,
        RetroNetCommands.FileHandleClose,
        RetroNetCommands.FileHandleDelete,
        RetroNetCommands.FileHandleDetails,
        RetroNetCommands.FileHandleInsert,
        RetroNetCommands.FileOpen,
        RetroNetCommands.FileHandleRead,
        RetroNetCommands.FileHandleReadSequence,
        RetroNetCommands.FileHandleReplace,
        RetroNetCommands.FileHandleSeek,
        RetroNetCommands.FileHandleSize,
        RetroNetCommands.FileHandleTruncate,
        RetroNetCommands.FileIndexStat,
        RetroNetCommands.FileList,
        RetroNetCommands.FileMove,
        RetroNetCommands.FileSize,
        RetroNetCommands.FileStat
    };

    protected override byte Version {get;} = 0x01;

    public override Task Handle(byte unhandled, CancellationToken cancel)
    {
        switch (unhandled) {
            case RetroNetCommands.FileOpen:
                return HandleFileOpen(cancel);
            case RetroNetCommands.FileHandleSize:
                return HandleFileHandleSize(cancel);
            case RetroNetCommands.FileHandleRead:
                return HandleFileHandleRead(cancel);
            case RetroNetCommands.FileHandleClose:
                return HandleFileHandleClose(cancel);
            case RetroNetCommands.FileSize:
                return HandleFileSize(cancel);
            case RetroNetCommands.FileHandleAppend:
                return HandleFileHandleAppend(cancel);
            case RetroNetCommands.FileHandleInsert:
                return HandleFileHandleInsert(cancel);
            case RetroNetCommands.FileHandleDelete:
                return HandleFileHandleDelete(cancel);
            case RetroNetCommands.FileHandleReplace:
                return HandleFileHandleReplace(cancel);
            case RetroNetCommands.FileDelete:             
                return HandleFileDelete(cancel);
            case RetroNetCommands.FileCopy:
                return HandleFileCopy(cancel);
            case RetroNetCommands.FileMove:
                return HandleFileMove(cancel);
            case RetroNetCommands.FileHandleTruncate:
                return HandleFileHandleTruncate(cancel);
            case RetroNetCommands.FileList:
                return HandleFileList(cancel);
            case RetroNetCommands.FileIndexStat:     
                return HandleFileIndexStat(cancel);
            case RetroNetCommands.FileStat:    
                return HandleFileStat(cancel);
            case RetroNetCommands.FileHandleDetails:
                return HandleFileHandleDetails(cancel);
            case RetroNetCommands.FileHandleReadSequence:
                return HandleFileHandleReadSequence(cancel);
            case RetroNetCommands.FileHandleSeek:
                return HandleFileHandleSeek(cancel);
            default:
                Warning($"Unsupported message: {Format(unhandled)}");
                return Task.CompletedTask;
        }
    }

    private Task HandleFileHandleSeek(CancellationToken cancel)
    {
        throw new NotImplementedException();
    }

    private Task HandleFileHandleReadSequence(CancellationToken cancel)
    {
        throw new NotImplementedException();
    }

    private Task HandleFileHandleDetails(CancellationToken cancel)
    {
        throw new NotImplementedException();
    }

    private Task HandleFileStat(CancellationToken cancel)
    {
        throw new NotImplementedException();
    }

    private Task HandleFileIndexStat(CancellationToken cancel)
    {
        throw new NotImplementedException();
    }

    private Task HandleFileList(CancellationToken cancel)
    {
        throw new NotImplementedException();
    }

    private Task HandleFileHandleTruncate(CancellationToken cancel)
    {
        throw new NotImplementedException();
    }

    private Task HandleFileMove(CancellationToken cancel)
    {
        throw new NotImplementedException();
    }

    private Task HandleFileCopy(CancellationToken cancel)
    {
        throw new NotImplementedException();
    }

    private Task HandleFileDelete(CancellationToken cancel)
    {
        throw new NotImplementedException();
    }

    private Task HandleFileHandleReplace(CancellationToken cancel)
    {
        throw new NotImplementedException();
    }

    private Task HandleFileHandleDelete(CancellationToken cancel)
    {
        throw new NotImplementedException();
    }

    private Task HandleFileHandleInsert(CancellationToken cancel)
    {
        throw new NotImplementedException();
    }

    private Task HandleFileHandleAppend(CancellationToken cancel)
    {
        throw new NotImplementedException();
    }

    private Task HandleFileSize(CancellationToken cancel)
    {
        throw new NotImplementedException();
    }

    private Task HandleFileHandleClose(CancellationToken cancel)
    {
        throw new NotImplementedException();
    }

    private Task HandleFileHandleRead(CancellationToken cancel)
    {
        throw new NotImplementedException();
    }

    private Task HandleFileHandleSize(CancellationToken cancel)
    {
        throw new NotImplementedException();
    }

    private Task HandleFileOpen(CancellationToken cancel)
    {
        throw new NotImplementedException();
    }
}

