using ModsCommon.Utilities;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using IStyleData = NodeMarkup.API.IStyleData;

namespace NodeMarkup.API
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
        private MarkupFiller Filler { get; }
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

        private Dictionary<string, IStylePropertyData> _properties;
        private Dictionary<string, IStylePropertyData> PropertiesDic
        {
            get
            {
                //if (_properties == null)
                //    _properties = Style.Properties.ToDictionary(i => i.Name, i => i);

                return _properties;
            }
        }
        public IEnumerable<IStylePropertyData> Properties => PropertiesDic.Values;

        public StyleDataProvider(Style style, string name)
        {
            Style = style;
            Name = name;
            _properties = null;
        }

        public object GetValue(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (!PropertiesDic.TryGetValue(name, out var property))
                throw new IntersectionMarkingToolException($"No option with name {name}");

            return property.Value;
        }

        public void SetValue(string name, object value)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (!PropertiesDic.TryGetValue(name, out var property))
                throw new IntersectionMarkingToolException($"No option with name {name}");

            property.Value = value;
        }

        public override string ToString() => Name;
    }
    public struct StylePropertyDataProvider<T> : IStylePropertyData
    {
        public Type Type => typeof(T).IsEnum ? Enum.GetUnderlyingType(typeof(T)) : typeof(T);
        public string Name { get; }
        BasePropertyValue<T> Property { get; }

        public object Value
        {
            get
            {
                if (Property.Value.GetType() == Type)
                    return Property.Value;
                else
                    return Convert.ChangeType(Property.Value, Type);
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (value.GetType() != Type)
                    throw new IntersectionMarkingToolException($"Wrong type of option {Name} value");

                Property.Value = (T)value;
            }
        }
        public StylePropertyDataProvider(string name, BasePropertyValue<T> property)
        {
            Name = name;
            Property = property;
        }

        public override string ToString() => $"{Name} ({Type}): {Value}";
    }
    //public struct StyleEnumPropertyDataProvider<T> : IStylePropertyData
    //    where T : struct, Enum
    //{
    //    public Type Type => Enum.GetUnderlyingType(typeof(T));
    //    public string Name { get; }
    //    PropertyEnumValue<T> Property { get; }

    //    public object Value
    //    {
    //        get => Property.Value;
    //        set
    //        {
    //            if (value == null)
    //                throw new ArgumentNullException(nameof(value));

    //            var valueType = value.GetType();
    //            var type = Type;
    //            if (valueType != type || !(type.IsEnum && valueType != Enum.GetUnderlyingType(type)))
    //                throw new IntersectionMarkingToolException($"Wrong type of option {Name} value");

    //            Property.Value = (T)value;
    //        }
    //    }
    //    public StyleEnumPropertyDataProvider(string name, PropertyEnumValue<T> property)
    //    {
    //        Name = name;
    //        Property = property;
    //    }

    //    public override string ToString() => $"{Name} ({Type}): {Value}";
    //}
}
