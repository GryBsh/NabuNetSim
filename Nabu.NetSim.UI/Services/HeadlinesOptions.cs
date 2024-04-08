﻿using Gry;
using Lgc;

namespace Nabu.NetSim.UI.Services;

public record HeadlinesOptions : Model, IDependencyOptions
{
    public List<HeadlineFeed> Feeds { get; set; } = [];
}