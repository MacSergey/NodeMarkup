using ModsCommon.Utilities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace NodeMarkup.Utilities
{
    public class Dependences
    {
        public Dictionary<Type, int> Total { get; } = EnumExtension.GetEnumValues<Type>().ToDictionary(i => i, i => 0);
        public bool Exist => Total.Values.Any(i => i != 0);

        public int Lines
        {
            get => Total[Type.Lines];
            set => Total[Type.Lines] = value;
        }
        public int Rules
        {
            get => Total[Type.Rules];
            set => Total[Type.Rules] = value;
        }
        public int Crosswalks
        {
            get => Total[Type.Crosswalks];
            set => Total[Type.Crosswalks] = value;
        }
        public int CrosswalkBorders
        {
            get => Total[Type.CrosswalkBorders];
            set => Total[Type.CrosswalkBorders] = value;
        }
        public int Fillers
        {
            get => Total[Type.Fillers];
            set => Total[Type.Fillers] = value;
        }

        public enum Type
        {
            [Description(nameof(Localize.Tool_DeleteDependenceLines))]
            Lines,

            [Description(nameof(Localize.Tool_DeleteDependenceRules))]
            Rules,

            [Description(nameof(Localize.Tool_DeleteDependenceCrosswalks))]
            Crosswalks,

            [Description(nameof(Localize.Tool_DeleteDependenceCrosswalkBorders))]
            CrosswalkBorders,

            [Description(nameof(Localize.Tool_DeleteDependenceFillers))]
            Fillers,
        }
    }
}
