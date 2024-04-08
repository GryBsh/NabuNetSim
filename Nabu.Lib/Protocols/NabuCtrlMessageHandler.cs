using Gry.Protocols;
using Gry.Settings;
using Microsoft.Extensions.Logging;
using Nabu.Network;
using Nabu.Settings;
using Nabu.Sources;

namespace Nabu.Protocols;

public class NabuCtrlMessageHandler(
    ILogger<NabuCtrlMessageHandler> logger,
    GlobalSettings settings, 
    SettingsProvider provider, 
    ISourceService sources, 
    INabuNetwork network
) : ICtrlMessageHandler<AdaptorSettings, byte>
{
    public byte[] Types { get; } = [(byte)NabuCtrlItemType.Source, (byte)NabuCtrlItemType.Program];

    public Task<CtrlItem> Set(
        Protocol<AdaptorSettings> protocol,
        byte type,
        Memory<byte> data
    )
    {
        var i = 0;
        var setType = (CtrlSetType)data.Span[i++];
        var adapterId = data.Span[i++];
        var adapter = adapterId is 0xFF ?
                                protocol.Adapter :
                                settings.Adapters
                                .Skip(adapterId)
                                .FirstOrDefault()
                                ;

        if (adapter is null)
        {
            return Task.FromResult(
                CtrlItem.Error(
                    (byte)NabuCtrlErrorCode.InvalidAdapter,
                    "Invalid Adapter"
                )
            );
        }

        switch ((NabuCtrlItemType)type)
        {
            case NabuCtrlItemType.Source:
                return Task.FromResult(SetSource());
            case NabuCtrlItemType.Program:
                return Task.FromResult(SetProgram());
            case NabuCtrlItemType.Setting:
                if (setType is not CtrlSetType.Value)
                {
                    return Task.FromResult(
                        CtrlItem.Error(
                            (byte)CtrlErrorCode.InvalidSetType,
                            "Invalid Set Type"
                        )
                    );
                }
                var settingId = data.Span[2];
                var valueLength = data.Span[3];
                var value = data[4..];

                var setting = provider.Settings(settings).Skip(settingId).FirstOrDefault();

                if (setting is null)
                {
                    return Task.FromResult(
                        CtrlItem.Error(
                            (byte)NabuCtrlErrorCode.InvalidSetting,
                            "Invalid Setting"
                        )
                    );
                }

                try
                {
                    setting?.SetValue(settings, CtrlItem.GetValue(setting.Type, value));
                }
                catch (Exception ex)
                {
                    return Task.FromResult(
                        CtrlItem.Error(
                            (byte)CtrlErrorCode.Unknown,
                            ex.Message
                        )
                    );
                }

                return Task.FromResult(
                    CtrlItem.FromValue(
                        CtrlItemType.OK,
                        setting!.Label,
                        setting.GetValue(settings)
                    )
                );
        }
        
        return Task.FromResult(
            CtrlItem.Error(
                (byte)CtrlErrorCode.UnknownType,
                "Invalid Type"
            )
        );

        CtrlItem SetSource()
        {
            if (setType is not CtrlSetType.Select)
            {

                return CtrlItem.Error(
                    (byte)CtrlErrorCode.InvalidSetType,
                    "Invalid Set Type"
                );
            }

            var sourceId = data.Span[i++];

            var source = sources.List.Skip(sourceId).FirstOrDefault();
            if (source is null)
            {
                return CtrlItem.Error(
                    (byte)NabuCtrlErrorCode.InvalidSource,
                    "Invalid Source"
                );
            }

            adapter.Source = source.Name;
            return new(CtrlItemType.OK, CtrlValueType.None, string.Empty, null);
        }

        CtrlItem SetProgram()
        {
            var setSource = SetSource();
            if (setSource.Type is not CtrlItemType.OK)
            {
                return setSource;
            }
            var programId = data.Span[i++];
            var program = network.Programs(adapter.Source).Skip(programId).FirstOrDefault();
            if (program is null)
            {
                return CtrlItem.Error(
                    (byte)NabuCtrlErrorCode.InvalidProgram,
                    "Invalid Program"
                );
            }
            adapter.Program = program.Name;
            return CtrlItem.FromValue(
                CtrlItemType.OK,
                string.Empty,
                null
            );
        }
    }

    public Task<IEnumerable<CtrlItem>> List(
        Protocol<AdaptorSettings> protocol,
        byte type,
        Memory<byte> data
    )
    {

        switch ((NabuCtrlItemType)type)
        {
            case NabuCtrlItemType.Source:
                logger.LogInformation("Listing Sources");
                return Task.FromResult(ListSources());
            case NabuCtrlItemType.Program:
                return Task.FromResult(ListPrograms());
            case NabuCtrlItemType.Setting:
                var adapterId = data.Span[0];
                var adapter = adapterId is 0xFF ?
                                protocol.Adapter :
                                settings.Adapters.Skip(adapterId).FirstOrDefault();

                var settingsList = adapterId switch
                {
                    0xFF => from setting in provider.Settings(settings)
                            select CtrlItem.FromValue(
                                (CtrlItemType)NabuCtrlItemType.Setting,
                                setting.Label,
                                setting.GetValue(settings)
                            ),
                    _ => from setting in provider.Settings(adapter)
                            select CtrlItem.FromValue(
                                (CtrlItemType)NabuCtrlItemType.Setting,
                                setting.Label,
                                setting.GetValue(adapter)
                            )
                };

                return Task.FromResult(settingsList);
        }
        return Task.FromResult<IEnumerable<CtrlItem>>(
            [ new (CtrlItemType.Error, 0x00, "Invalid Type",null) ]
        );

        IEnumerable<CtrlItem> ListSources()
        {
            return from source in sources.List
                   where !source.Name.LowerEquals(protocol.Adapter!.Source)
                   select new CtrlItem(
                       (CtrlItemType)NabuCtrlItemType.Source,
                       CtrlValueType.None,
                       source.Name,
                       null
                   );                                                                                                                  
        }   

        IEnumerable<CtrlItem> ListPrograms()
        {
            var sourceId = data.Span[0];
            logger.LogInformation("Listing Programs for Source {}", sourceId);
            var programSource = sources.List.Skip(sourceId).FirstOrDefault();
            if (programSource is null)
            {
                logger.LogError("Invalid Source {}", sourceId);
                return [new CtrlItem(CtrlItemType.Error, 0x00, "Invalid Source", null)];
            }
            var programs = network.Programs(programSource);
            return  from program in programs
                    where !NabuLib.IsPakFile(program.Name)
                    select new CtrlItem(
                        (CtrlItemType)NabuCtrlItemType.Program,
                        CtrlValueType.None,
                        program.Name,
                        null
                    );
        }
    }

}
