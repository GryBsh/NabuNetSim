using Nabu.Messages;
using Nabu.Services;
using System;
using System.Text.RegularExpressions;

namespace Nabu.Network;

/*
    [1      - 0x30
      2     - Length
       ...] - Frame Data

    [1      - 0x00 
      1     - Menu Index
       1]   - Page Index
    
    [2          - Frame Length
      1|n*]     - Item Length n, n bytes of item name, any number of times     

    [1      - 0x01 
      1     - Menu Index
       1    - Page Index
        1]  - Item Index


    [1]     - 0x10
          

    Next Action:
        - 0xFF - Restart
        - 0xFE - Cold Start
*/
public static class MenuCommands
{
    public const byte List = 0x00;
    public const byte Select = 0x01;

    public const byte ListAdaptors = 0x10;
}
public partial class MenuProtocol : Protocol
{
    public MenuProtocol(
        ILog<MenuProtocol> logger,
        ISourceService sources,
        INabuNetwork network,
        Settings globalSettings,
        AdaptorSettings? settings = null) :
        base(logger, settings)
    {
        Sources = sources;
        Network = network;
        Settings = globalSettings;
    }
    private void SetReturn()
    {
       
    }

    Settings Settings { get; }
    public INabuNetwork Network { get; }
    public ISourceService Sources { get; }
    public override byte Version { get; } = 1;
    public override byte[] Commands { get; } = new byte[]
    {
        0x30
    };

    protected override Task Handle(byte unhandled, CancellationToken cancel)
    {

        var (_, data) = ReadFrame();
        unhandled = data[0];
        byte[] command = data.Length > 1 ? data[1..] : Array.Empty<byte>();
        switch (unhandled)
        {
            case MenuCommands.List:
                List(ref command);
                break;
            case MenuCommands.Select:
                SetProgram(ref command);
                break;
            case MenuCommands.ListAdaptors:
                ListAdaptors();
                break;
        }
        
        return Task.CompletedTask;
    }

    #region Framed Version
    const int pageSize = 20;


    bool IsNotPakFile(NabuProgram? program)
        => program is not null &&
                   !NabuLib.IsPakFile(program.Name);
    private bool IsPak(ProgramSource source, IEnumerable<NabuProgram> programs)
        => programs.Any(p => p.Name == Constants.CycleMenuPak) && !source.EnableExploitLoader;

    IEnumerable<ProgramSource> SourceList()
        => Sources.All().Where(s => !s.Name.LowerEquals(Adaptor.Source));

    ProgramSource? Source(int index) => SourceList().Skip(index).FirstOrDefault();

    IEnumerable<ProgramSource> SourcePage(int page)
        => SourceList().Skip(page * pageSize).Take(pageSize);
    IEnumerable<NabuProgram> ProgramList(IEnumerable<NabuProgram> programs) 
        => programs.Where(IsNotPakFile);
    IEnumerable<NabuProgram> ProgramPage(IEnumerable<NabuProgram> programs, int page)
        => ProgramList(programs).Skip(page * pageSize).Take(pageSize);

    byte[] MenuItem(string message)
    {
        message = message[..(message.Length > 32 ? 32 : message.Length)].Trim();
        message = ASCII().Replace(message, string.Empty);

        var str = NabuLib.ToSizedASCII(message);
        return str.ToArray();
    }

    void List(ref byte[] data)
    {
        var menu = data[0];
        var page = data[1];
        

        (byte,byte[],int) mainMenu()
        {
            var sources = SourceList().Skip(page * pageSize).Take(pageSize);

            var itemCount = SourceList().Count();
            var pages = itemCount == pageSize ? pageSize : (int)Math.Floor((double)(itemCount / pageSize) + 1);

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
                items = new[] { new NabuProgram { DisplayName = "No Programs" } };

            var itemCount = ProgramList(programs).Count();
            var pages = itemCount == pageSize ? pageSize : (int)Math.Floor((double)(itemCount / pageSize) + 1);

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

    private void SetProgram(ref byte[] data)
    {
        var index = data[0];
        var page = data[1];
        var item = data[2];
        var source = SourceList().Skip(index-1).FirstOrDefault();

        if (source is null) return;
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

        Adaptor.ReturnToSource = Adaptor.Source;
        Adaptor.ReturnToProgram = Adaptor.Program;
        Adaptor.Source = source.Name;
        Adaptor.Program = program.Name;
        Write(0xFF);
    }

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

    [GeneratedRegex("[^\\u0000-\\u007F]+")]
    private static partial Regex ASCII();

    #endregion
}
