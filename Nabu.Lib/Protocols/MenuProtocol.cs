using Gry;
using Gry.Protocols;
using Microsoft.Extensions.Logging;
using Nabu.Settings;
using Nabu.Sources;
using System.Text.RegularExpressions;

namespace Nabu.Network
{
    /*
        [1      - 0x30
          2     - Length
           ...] - Command + Data

        Commands:
    
        Get Menu:
        [1      - 0x00 
          1     - Menu Index
           1]   - Page Index
    
        Returns:

        [2         - Frame Length
          (1       - Item Length
            ...)*] - bytes of item name, any number of times     

        The items for the specified page of the specified menu.

        Select Item:

        [1      - 0x01 
          1     - Menu Index
           1    - Page Index
            1]  - Item Index

        Returns:

        [1]   - See below
           
            
        The type of reboot to perform

        Adaptor List:

        [1]     - 0x10
    
        Returns:          
    
        [2         - Frame Length
          (1       - Item Length
            ...)*] - bytes of item name, any number of times  

        Boot Codes:
            - 0xFF - Restart
            - 0xFE - Cold Start
            - 0xXX - Load Menu XX

 
    */
    public static class MenuCommands
    {
        public const byte List = 0x00;
        public const byte Select = 0x01;

        public const byte DateTime = 0x10;
    }
    public partial class MenuProtocol : Protocol<AdaptorSettings>
    {
        public MenuProtocol(
            ILogger<MenuProtocol> logger,
            ISourceService sources,
            INabuNetwork network,
            GlobalSettings globalSettings
        ) : base(logger)
        {
            Sources = sources;
            Network = network;
            Settings = globalSettings;
        }

        GlobalSettings Settings { get; }
        public INabuNetwork Network { get; }
        public ISourceService Sources { get; }
        public override byte Version { get; } = 1;
        public override byte[] Messages { get; } = [0x30];

        static Memory<byte> DateTimeString(DateTime date)
        {
            return new([
                ..Bytes.FromASCII(date.ToString("yyyyMMdd")).Span,
                ..Bytes.FromASCII(date.ToString("HHmmss")).Span
            ]);
        }

        protected override Task Handle(byte unhandled, CancellationToken cancel)
        {

            var (_, data) = ReadFrame();
            unhandled = data.Span[0];
            Memory<byte> command = data.Length > 1 ? data[1..] : new();
            switch (unhandled)
            {
                case MenuCommands.List:
                    List(command);
                    break;
                case MenuCommands.Select:
                    SetProgram(command);
                    break;
                case MenuCommands.DateTime:
                    WriteFrame(DateTimeString(DateTime.Now));
                    break;
                //case MenuCommands.ListAdaptors:
                //    ListAdaptors();
                //    break;
            }
        
            return Task.CompletedTask;
        }

        const int pageSize = 20;


        bool IsNotPakFile(NabuProgram? program)
            => program is not null &&
                       !NabuLib.IsPakFile(program.Name);
        private bool IsPak(ProgramSource source, IEnumerable<NabuProgram> programs)
            => programs.Any(p => p.Name == Constants.CycleMenuPak) && !source.EnableExploitLoader;

        IEnumerable<ProgramSource> SourceList()
            => Sources.List.Where(s => !s.Name.LowerEquals(Adapter!.Source));

        IEnumerable<NabuProgram> ProgramPage(IEnumerable<NabuProgram> programs, int page)
            => programs.Where(IsNotPakFile).Skip(page * pageSize).Take(pageSize);

        byte[] MenuItem(string message)
        {
            message = message[..(message.Length > 32 ? 32 : message.Length)].Trim();
            message = ASCII().Replace(message, string.Empty);

            var str = NabuLib.ToSizedASCII(message);
            return str.ToArray();
        }

        void List(Memory<byte> data)
        {
            var menu = data.Span[0];
            var page = data.Span[1];
        

            (byte,byte[],int) mainMenu()
            {
                var sources = SourceList().Skip(page * pageSize).Take(pageSize);

                var itemCount = SourceList().Count();
                var pages = itemCount == pageSize ? 1 : (int)Math.Floor((double)(itemCount / pageSize) + 1);

                return (
                    (byte)pages,
                    sources
                       .SelectMany(s => MenuItem(s.Name))
                       .ToArray(),
                    sources.Count()
                );
            }

            (byte, byte[],int) sourceMenu()
            {
                var source = SourceList().Skip(menu-1).First();
                var programs = Network.Programs(source);

                if (IsPak(source, programs))
                {
                    return (1, NabuLib.ToSizedASCII($"Start {source.Name}").ToArray(), 1);
                }
            
                var items = ProgramPage(programs, page);

                if (!items.Any())
                {
                    return (0, [], 0);
                }

                var itemCount = programs.Where(IsNotPakFile).Count();
                var pages = itemCount == pageSize ? 1 : (int)Math.Floor((double)(itemCount / pageSize) + 1);

                return (
                    (byte)pages,
                    items
                       .SelectMany(s => MenuItem(s.DisplayName))
                       .ToArray(),
                    items.Count()
                );
            }

            var (pageCount, menuItems, count) = menu switch
            {
                0 => mainMenu(),
                _ => sourceMenu()
            };
        
            WriteFrame(pageCount, menuItems);

            Log($"Menu: {menu}, Page: {page + 1}/{pageCount} Sent ({count} Items)");
        }

        private void SetProgram(Memory<byte> data)
        {
            var index = data.Span[0];
            var page = data.Span[1];
            var item = data.Span[2];
            var source = SourceList().Skip(index-1).FirstOrDefault();

            if (source is null)
            {
                Log($"Source not found {index}");
                Write(0x00);
                return;
            }

            var programs = Network.Programs(source);
            var program = IsPak(source, programs) switch {
                true => programs.Where(p => p.Name == Constants.CycleMenuPak).FirstOrDefault(),
                false => ProgramPage(programs,page).Skip(item).FirstOrDefault()
            };

            if (program is null) {
                Log($"No program to set, No Action");
                Write(0x00);
                return; 
            }
            Log($"Set [{index}:{item}]: Source: {source.Name}, Program: {program.Name}");

            Adapter!.ReturnToSource = Adapter.Source;
            Adapter.ReturnToProgram = Adapter.Program;
            Adapter.Source = source.Name;
            Adapter.Program = program.Name;

            if (program.Name.StartsWith("Cloud CP/M", StringComparison.OrdinalIgnoreCase))
            {
                Write(0xFE);
            }
            Write(0xFF);
        }

        /*
        private void ListAdaptors()
        {
            var adaptors = Enumerable.Concat<AdaptorSettings>(
                Settings.Adaptors.Serial, 
                Settings.Adaptors.TCP
            ).SelectMany(
                s => MenuItem($"{s.Port}: {s.State}")    
            ).ToArray();
            var itemCount = adaptors.Count();
            var pages = itemCount == pageSize ? pageSize : Math.Floor((double)(itemCount / pageSize) + 1);
            WriteFrame((byte)pages, adaptors);
            Log("Adaptor List Sent");

        }
        */

        [GeneratedRegex("[^\\u0000-\\u007F]+")]
        private static partial Regex ASCII();
    }
}
