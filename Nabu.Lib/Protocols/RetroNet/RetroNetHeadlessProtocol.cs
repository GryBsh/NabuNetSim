﻿using Gry;
using Gry.Protocols;
using Microsoft.Extensions.Logging;
using Nabu.Network;
using Nabu.Settings;
using Nabu.Sources;
using System.Text;
using System.Text.RegularExpressions;

namespace Nabu.Protocols.RetroNet;
    INabuNetwork network,
    GlobalSettings globalSettings
    {
        Adapter!.ReturnToSource = Adapter!.Source;
        Adapter!.ReturnToProgram = Adapter!.Program;
    }