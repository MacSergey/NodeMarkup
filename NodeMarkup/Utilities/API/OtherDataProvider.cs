using NodeMarkup.API;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using IStyleData = NodeMarkup.API.IStyleData;

namespace NodeMarkup.Utilities.API
{
    public struct CrosswalkDataProvider : ICrosswalkData
    {
        private MarkupCrosswalk Crosswalk { get; }
        public ICrosswalkLineData Line => new CrosswalkLineDataProvider(Crosswalk.CrosswalkLine);

        public ushort MarkingId => Crosswalk.Markup.Id;

        public CrosswalkDataProvider(MarkupCrosswalk crosswalk)
        {
            Crosswalk = crosswalk;
        }

        public override string ToString() => Crosswalk.ToString();
    }

    public struct FillerDataProvider : IFillerData
    {
        private MarkupFiller Filler { get;}
        public int Id => Filler.Id;

        public ushort MarkingId => Filler.Markup.Id;

        public FillerDataProvider(MarkupFiller filler)
        {
            Filler = filler;
        }

        public override string ToString() => Filler.ToString();
    }

    public struct StyleDataProvider : IStyleData, IRegularLineStyleData, INormalLineStyleData, IStopLineStyleData, ILaneLineStyleData, ICrosswalkStyleData, IFillerStyleData
    {
        private Style Style { get; }
        public string Name { get; }
        private Dictionary<string, IStyleOptionData> OptionList { get; }
        private Dictionary<string, object> Values { get; }
        public IEnumerable<IStyleOptionData> Options => OptionList.Values;

        public StyleDataProvider(Style style, string name)
        {
            Style = style;
            Name = name;
            OptionList = new Dictionary<string, IStyleOptionData>();
            Values = new Dictionary<string, object>();
        }

        public object GetValue(IStyleOptionData option)
        {
            if (option == null)
                throw new ArgumentNullException(nameof(option));

            if (!Values.TryGetValue(option.Name, out var value))
                throw new IntersectionMarkingToolException($"No option with name {option.Name}");

            return value;
        }

        public void SetValue(IStyleOptionData option, object value)
        {
            if (option == null)
                throw new ArgumentNullException(nameof(option));

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (!OptionList.TryGetValue(option.Name, out var styleOption))
                throw new IntersectionMarkingToolException($"No option with name {option.Name}");

            if(value.GetType() != styleOption.Type)
                throw new IntersectionMarkingToolException($"Wrong type of option {option.Name} value");

            Values[styleOption.Name] = value;
        }
    }
}
