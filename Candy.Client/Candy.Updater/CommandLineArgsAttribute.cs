using System;
using System.Collections.Generic;
using System.Linq;

namespace Candy.Updater
{
    public sealed class CommandLineArgsAttribute : Attribute
    {
        public IReadOnlyList<string> Keys { get; private set; }

        public CommandLineArgsAttribute(params string[] keys)
        {
            Keys = keys.ToArray();
        }
    }
}