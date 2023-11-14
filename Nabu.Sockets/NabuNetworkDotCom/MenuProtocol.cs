using Nabu.Services;

namespace Nabu.Network.NabuNetworkCom;

/*
    
    [1      - 0x30 
      1     - Menu Index
       1]   - Page Index
    
    [1          - Frame Length
      1|n*]     - Item Length n, n bytes of item name, any number of times     

    [1      - 0x31 
      1     - Menu Index
       1    - Page Index
        1]  - Item Index

    
    Next Action:
        - 0x00 - None
        - 0xFE - Reload (TODO)
        - 0xFF - Restart
*/
public static class MenuCommands
{
    public const byte List = 0x30;
    public const byte Select = 0x31;
}
public class MenuProtocol : Protocol
{
    public MenuProtocol(
        ILog<MenuProtocol> logger,
        ISourceService sources,
        INabuNetwork network,
        AdaptorSettings? settings = null) :
        base(logger, settings)
    {
        Sources = sources;
        Network = network;
    }
    private void SetReturn()
    {
        Adaptor.ReturnToSource = Adaptor.Source;
        Adaptor.ReturnToProgram = Adaptor.Program;
    }

    public INabuNetwork Network { get; }
    public ISourceService Sources { get; }
    public override byte Version { get; } = 1;
    public override byte[] Commands { get; } = new byte[]
    {
        MenuCommands.List,
        MenuCommands.Select
    };

    protected override Task Handle(byte unhandled, CancellationToken cancel)
    {
        switch (unhandled)
        {
            case MenuCommands.List:
                List();
                break;
            case MenuCommands.Select:
                SetProgram();
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

    void List()
    {
        var menu = Read();
        var page = Read();
        
        (int,byte[]) mainMenu()
        {
            var sources = SourcePage(page);

            return (
                (int)Math.Floor((double)(SourceList().Count() / pageSize)) + 1,
                sources
                   .SelectMany(s => NabuLib.ToSizedASCII(s.Name).ToArray())
                   .ToArray()
            );
        }

        (int, byte[]) sourceMenu()
        {
            var source = SourceList().Skip(menu-1).First();
            var programs = Network.Programs(source);

            if (IsPak(source, programs))
            {
                
                return (1, NabuLib.ToSizedASCII($"Start {source.Name}").ToArray());
            }
            
            var items = ProgramPage(programs, page);

            if (!items.Any())
                items = new[] { new NabuProgram { DisplayName = "No Programs" } };

            return (
                (int)Math.Floor((double)(ProgramList(programs).Count() / pageSize)) + 1,
                items
                   .SelectMany(p => NabuLib.ToSizedASCII(p.DisplayName).ToArray())
                   .ToArray()
            );
        }

        var (count, menuItems) = menu switch
        {
            0 => mainMenu(),
            _ => sourceMenu()
        };

        //ushort size = (ushort)(1 + menuItems.Length);
        //Write(NabuLib.FromUShort2(size));
        //Write((byte)count);
        //Write(menuItems);
        WriteFrame(
            (byte)count,
            menuItems
        );

        Log($"Menu: {menu}, Page: {page + 1}/{count} Sent ");
    }

    private void SetProgram()
    {
        var index = Read();
        var page = Read();
        var item = Read();
        var source = Source(index - 1);

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

        SetReturn();
        Adaptor.Source = source.Name;
        Adaptor.Program = program.Name;
        Write(0xFF);
    }

    #endregion
}
