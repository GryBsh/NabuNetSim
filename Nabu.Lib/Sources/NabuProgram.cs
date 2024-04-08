﻿using Gry;
using Nabu.Network;

namespace Nabu.Sources
{
    public record NabuProgram
    {
        public NabuProgram()
        {
        }

        public NabuProgram(
            string displayName,
            string name,
            string source,
            string path,
            SourceType sourceType,
            ImageType imageType,
            IProgramPatch[] patches,
            bool isPakMenu = false,
            IDictionary<string, object?>? options = null,
            string? category = null,
        )
        {
            DisplayName = displayName;
            Name = name;
            Source = source;
            Path = path;
            SourceType = sourceType;
            ImageType = imageType;
            Patches = patches;
            IsPakMenu = isPakMenu;
            Options = options ?? Options;
            Category = category;
        }

        public string? Category { get; set; }

        public string DisplayName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public IDictionary<string, object?> Options { get; set; } = new DataDictionary();
        
        public SourceType SourceType { get; set; }
        public ImageType ImageType { get; set; }
        
        public IList<IProgramPatch> Patches { get; set; } = new List<IProgramPatch>();
        
        public bool IsPakMenu { get; set; }
        public bool UseCPMDirect => Option<bool>(nameof(UseCPMDirect));
        public bool Headless
        public T? Option<T>(string name)
        {
            if (Options.TryGetValue(name, out var value) is true)
            {
                return (T?)value;
            }
            return default;
        }
    }
}