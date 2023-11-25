using Nabu.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Nabu.Network.NabuNetworkCom;




public static class Headless
{
    public const byte Menu = 0x20;
    public const byte MenuItem = 0x21;
    public const byte SetCycle = 0x22;
    public const byte SetPath = 0x23;

    public const byte MenuFrame = 0x30;
    public const byte SetMenuItem = 0x31;

    public const string SourceName = "headless";
}

public class HeadlessProtocol : Protocol
{
    public HeadlessProtocol(
        ILog<HeadlessProtocol> logger,
        ISourceService sources,
        INabuNetwork network,
        AdaptorSettings? settings = null) :
        base(logger, settings)
    {
        Sources = sources;
        Network = network;
    }

    public override byte[] Commands { get; } = new byte[]
    {
        Headless.Menu,
        Headless.MenuItem,
        Headless.SetCycle,
        Headless.SetPath,
    };

    public INabuNetwork Network { get; }
    public ISourceService Sources { get; }
    public override byte Version { get; } = 1;

    protected override Task Handle(byte unhandled, CancellationToken cancel)
    {
        //Log($"Received: {Format(unhandled)}");
        switch (unhandled)
        {
            case Headless.Menu:
                MenuCount();
                break;

            case Headless.MenuItem:
                MenuItem();
                break;

            case Headless.SetCycle:
                SetCycle();
                break;

            case Headless.SetPath:
                SetPath();
                break;
        }
        return Task.CompletedTask;
    }

    private IEnumerable<ProgramSource> ExcludeCurrent(IEnumerable<ProgramSource> sources)
        => Sources.All().Where(s => !s.Name.LowerEquals(Adaptor.Source));

    private bool IsPak(ProgramSource source, IEnumerable<NabuProgram> programs)
                => programs.Any(p => p.Name == Constants.CycleMenuPak) && !source.EnableExploitLoader;
    
    
    private void MenuCount()
    {
        var index = Read();

        int sourceMenu()
        {
            var source = Source(index - 1);
            var programs = Network.Programs(source);

            if (IsPak(source, programs)) return 1;
            return programs.Count();
        }

        var count = index switch
        {
            0 => ExcludeCurrent(Sources.All()).Count(),
            _ => sourceMenu(),
        };
        Log($"Menu: {index}. Count: {count}");
        Write((byte)count);
    }

   

    private void MenuItem()
    {
        var index = Read();

        string mainMenu()
        {
            var item = Read();
            var source = Source(item);
            Log($"Source: {source.Name}");
            return source.Name;
        }

        string sourceMenuItem()
        {
            var item = Read();
            var source = Source(index - 1);

            if (IsPak(source, Network.Programs(source)))
            {
                Log($"Source: {source.Name}, Program: Main");
                return $"Start {source.Name}";
            }

            NabuProgram? program = Program(source, item);
            var name = program switch
            {
                null => string.Empty,
                _ => program.DisplayName
            };
            Log($"Source: {source.Name}, Program: {program?.Name}");
            return name;
        }

        var name = index switch
        {
            0 => mainMenu(),
            _ => sourceMenuItem()
        };

        Write(NabuLib.ToSizedASCII(name).ToArray());
    }

    bool IsNotPakFile(NabuProgram? program)
                => program is not null &&
                   !NabuLib.IsPakFile(program.Name);
    private NabuProgram? Program(ProgramSource source, int index)
        => Network.Programs(source).Where(IsNotPakFile).Skip(index).FirstOrDefault();

    private void SetCycle()
    {
        var index = Read();
        var item = Read();
        var source = Source(index - 1);
        var program = Program(source, item);

        if (program is null) return;
        Log($"Set [{index}:{item}]: Source: {source.Name}, Program: {program.Name}");

        SetReturn();
        Adaptor.Source = source.Name;
        Adaptor.Program = program.Name;
    }

    private void SetPath()
    {
        var path = Reader.ReadString();

        Sources.Refresh(new() { Name = Headless.SourceName, SourceType = SourceType.Local, Path = path });
        Log($"Set Path: path");

        SetReturn();
        Adaptor.Source = Headless.SourceName;
        Adaptor.Program = string.Empty;
    }

    private void SetReturn()
    {
        Adaptor.ReturnToSource = Adaptor.Source;
        Adaptor.ReturnToProgram = Adaptor.Program;
    }

    private ProgramSource Source(int index)
        => ExcludeCurrent(Sources.All()).Skip(index).FirstOrDefault() ??
           new();
}