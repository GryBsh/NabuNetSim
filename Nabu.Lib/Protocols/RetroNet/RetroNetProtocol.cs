using Gry.Adapters;
using Gry.Caching;
using Gry.Protocols;
using Gry.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nabu.Network;
using Nabu.Protocols.RetroNet;
using Nabu.Settings;
using System.Reactive.Linq;
using System.Text.RegularExpressions;

namespace Nabu.Protocols.RetroNet
{
    public partial class RetroNetProtocol : Protocol<AdaptorSettings>
    {        private readonly IOptions<CacheOptions> cacheOptions;        //private readonly GlobalSettings Global;        public RetroNetProtocol(
            ILogger<RetroNetProtocol> logger,
            HttpClient httpClient,
            INabuNetwork nabuNet,
            IFileCache cache,
            IOptions<CacheOptions> options,            LocationService location
        ) : base(logger)
        {
            HttpClient = httpClient;
            NabuNet = nabuNet;
            FileCache = cache;            cacheOptions = options;            Location = location;            //Global = settings;            
        }

        public override byte[] Messages { get; } = [
            RetroNetCommands.FileCopy,
            RetroNetCommands.FileDelete,
            RetroNetCommands.FileHandleAppend,
            RetroNetCommands.FileHandleClose,
            RetroNetCommands.FileHandleDeleteRange,
            RetroNetCommands.FileHandleDetails,
            RetroNetCommands.FileHandleInsert,
            RetroNetCommands.FileHandleRead,
            RetroNetCommands.FileHandleReadSequence,
            RetroNetCommands.FileHandleReplaceRange,
            RetroNetCommands.FileHandleSeek,
            RetroNetCommands.FileHandleSize,
            RetroNetCommands.FileHandleTruncate,
            RetroNetCommands.FileIndexStat,
            RetroNetCommands.FileList,
            RetroNetCommands.FileMove,
            RetroNetCommands.FileOpen,
            RetroNetCommands.FileSize,
            RetroNetCommands.FileStat,
            RetroNetCommands.TCPHandleOpen,
            RetroNetCommands.TCPHandleClose,
            RetroNetCommands.TCPHandleSize,
            RetroNetCommands.TCPHandleRead,
            RetroNetCommands.TCPHandleWrite,
            RetroNetCommands.TCPServerClientCount,
            RetroNetCommands.TCPServerAvailable,
            RetroNetCommands.TCPServerRead,
            RetroNetCommands.TCPServerWrite,
        ];

        public override byte Version { get; } = 0x01;
        private FileDetails[]? CurrentList { get; set; }
        private IFileCache FileCache { get; }        public LocationService Location { get; }        private HttpCache? Http { get; set; }
        private CacheOptions CacheOptions => cacheOptions.Value;
        private HttpClient HttpClient { get; }
        private INabuNetwork NabuNet { get; }
        private Dictionary<byte, IRetroNetFileHandle> Slots { get; } = [];

        

        public override bool Attach(AdaptorSettings settings, Stream stream)
        {
            Http = new(HttpClient, Logger, FileCache, cacheOptions, Location, settings.Port);
            return base.Attach(settings, stream);
        }

        public override void Detach()
        {
            ShutdownTCPServer();
            base.Detach();
        }

        public override void Reset()
        {
            if (Slots.Count > 0)
            {
                var cancel = CancellationToken.None;
                if (Slots.Count > 0)
                {
                    foreach (var b in Slots.Keys)
                        Slots[b].Close(cancel);
                    Slots.Clear();
                }

                base.Reset();
            }
            ShutdownTCPServer();
        }
                
        protected override async Task Handle(byte unhandled, CancellationToken cancel)
        {
            try
            {
                Listener(Adapter);

                switch (unhandled)
                {
                    case RetroNetCommands.FileOpen:
                        await FileOpen(cancel);
                        break;

                    case RetroNetCommands.FileHandleSize:
                        await FileHandleSize(cancel);
                        break;

                    case RetroNetCommands.FileHandleRead:
                        await FileHandleRead(cancel);
                        break;

                    case RetroNetCommands.FileHandleClose:
                        await FileHandleClose(cancel);
                        break;

                    case RetroNetCommands.FileSize:
                        await FileSize(cancel);
                        break;

                    case RetroNetCommands.FileHandleAppend:
                        await FileHandleAppend(cancel);
                        break;

                    case RetroNetCommands.FileHandleInsert:
                        await FileHandleInsert(cancel);
                        break;

                    case RetroNetCommands.FileHandleDeleteRange:
                        await FileHandleDelete(cancel);
                        break;

                    case RetroNetCommands.FileHandleReplaceRange:
                        await FileHandleReplace(cancel);
                        break;

                    case RetroNetCommands.FileDelete:
                        await FileDelete(cancel);
                        break;

                    case RetroNetCommands.FileCopy:
                        await FileCopy(cancel);
                        break;

                    case RetroNetCommands.FileMove:
                        await FileMove(cancel);
                        break;

                    case RetroNetCommands.FileHandleTruncate:
                        await FileHandleTruncate(cancel);
                        break;

                    case RetroNetCommands.FileList:
                        await FileList(cancel);
                        break;

                    case RetroNetCommands.FileIndexStat:
                        await FileIndexStat(cancel);
                        break;

                    case RetroNetCommands.FileStat:
                        await FileStat(cancel);
                        break;

                    case RetroNetCommands.FileHandleDetails:
                        await FileHandleDetails(cancel);
                        break;

                    case RetroNetCommands.FileHandleReadSequence:
                        await FileHandleReadSequence(cancel);
                        break;

                    case RetroNetCommands.FileHandleSeek:
                        await FileHandleSeek(cancel);
                        break;

                    case RetroNetCommands.TCPHandleOpen:
                        await TCPHandleOpen();
                        break;

                    case RetroNetCommands.TCPHandleClose:
                        await TCPHandleHandleClose();
                        break;

                    case RetroNetCommands.TCPHandleSize:
                        await TCPHandleSize();
                        break;

                    case RetroNetCommands.TCPHandleRead:
                        await TCPHandleRead(cancel);
                        break;

                    case RetroNetCommands.TCPHandleWrite:
                        await TCPHandleWrite(cancel);
                        break;

                    case RetroNetCommands.TCPServerClientCount:
                        await TCPServerClientCount();
                        break;

                    case RetroNetCommands.TCPServerAvailable:
                        await TCPServerAvailable();
                        break;

                    case RetroNetCommands.TCPServerRead:
                        await TCPServerRead(cancel);
                        break;

                    case RetroNetCommands.TCPServerWrite:
                        await TCPServerWrite(cancel);
                        break;

                    default:
                        Warning($"Unsupported message: {Format(unhandled)}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Error(ex.Message);
            }
        }

        [GeneratedRegex("ftp://.*")]
        private static partial Regex Ftp();

        [GeneratedRegex("0x//.*")]
        private static partial Regex Memory();

        private async Task FileCopy(CancellationToken cancel)
        {
            var source = ReadString();
            var destination = ReadString();
            var flags = (CopyMoveFlags)Read();
            Log($"Copy: {source} -> {destination}");
            await FileHandler(source).Copy(source, destination, flags);
        }

        private async Task FileDelete(CancellationToken cancel)
        {
            var filename = ReadString();
            await FileHandler(filename).Delete(filename);
            Log($"Deleted: {filename}");
        }

        private IRetroNetFileHandler FileHandler(string filename)
        {
            return filename switch
            {
                _ when NabuLib.IsHttp(filename) => new RetroNetHttpHandler(                    Logger,                     HttpClient,                     Adapter!,                     FileCache,                     Location,                     cacheOptions                ),
                _ => new RetroNetFileHandler(                    Logger,                     Adapter!                )
            };
        }

        private Task FileIndexStat(CancellationToken cancel)
        {
            var index = ReadShort();
            var file = CurrentList![index];
            Log($"List Details: {index}");
            Writer.Write(file);
            return Task.CompletedTask;
        }

        private async Task FileList(CancellationToken cancel)
        {
            var path = ReadString();
            var wildcard = ReadString();
            var flags = (FileListFlags)Read();
            Log($"List: {path}\\{wildcard}");
            CurrentList = (await FileHandler(path).List(path, wildcard, flags)).ToArray();
            Writer.Write((short)CurrentList.Length);
        }

        private async Task FileMove(CancellationToken cancel)
        {
            var source = ReadString();
            var destination = ReadString();
            var flags = (CopyMoveFlags)Read();
            Log($"Move: {source} -> {destination}");
            await FileHandler(source).Move(source, destination, flags);
        }

        private async Task FileSize(CancellationToken cancel)
        {
            var filename = ReadString();
            //var handle = NextIndex();
            //await FileOpen(filename, FileOpenFlags.ReadOnly, handle, cancel);
            Log($"Size: {filename}");

            var size = await FileHandler(filename).Size(filename);
            Writer.Write(size);
        }

        private async Task FileStat(CancellationToken cancel)
        {
            var filename = ReadString();
            var details = await FileHandler(filename).FileDetails(filename);
            Log($"Details: {filename}");
            Writer.Write(details);
        }

        private byte NextSlotIndex()
        {
            for (int i = 0x00; i < 0xFF; i++)
            {
                if (Slots.ContainsKey((byte)i)) continue;
                return (byte)i;
            }
            return 0xFF;
        }

        private string ReadString() => Reader.ReadString();

        private void WriteBuffer(Memory<byte> bytes)
        {
            Writer.Write(NabuLib.FromUShort((ushort)bytes.Length));
            Writer.Write(bytes.ToArray());
        }
    }
}