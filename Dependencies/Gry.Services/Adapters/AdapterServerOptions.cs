﻿using Gry.Caching;
using Lgc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Gry.Adapters;

public record AdapterServerOptions<TAdapter, TTCPAdapter, TSerialAdapter>
    where TAdapter : AdapterDefinition
    where TTCPAdapter : TAdapter
    where TSerialAdapter : TAdapter
{
    public List<TTCPAdapter> TCP { get; set; } = []; 
    public List<TSerialAdapter> Serial { get; set; } = [];
    public IEnumerable<TAdapter> Adapters
        => Enumerable.Concat<TAdapter>(TCP, Serial).ToArray();
}