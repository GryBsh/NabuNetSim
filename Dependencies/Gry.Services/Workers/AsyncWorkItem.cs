﻿namespace Gry.Workers;public record AsyncWorkItem<T>(string Id, T Value);