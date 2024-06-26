﻿using Microsoft.Extensions.Logging;
using Nabu.Settings;

namespace Nabu;


public abstract class NabuService : NabuBase
{
    public NabuService(ILogger logger, AdaptorSettings settings, string? label = null) : base(logger, label)
    {
        Adaptor = settings;
    }

    protected AdaptorSettings Adaptor { get; set; }

    protected override string LogMessage(string message)
    {
        var label = !string.IsNullOrWhiteSpace(Label) ? $"{Label}:" : string.Empty;
        return $"{label}{Adaptor.Port}: {message}";
    }
}