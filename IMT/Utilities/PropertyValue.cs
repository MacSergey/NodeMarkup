using ModsCommon.Utilities;
using System;
using System.Xml.Linq;

namespace IMT.Utilities
{
    public class PropertyThemeValue : PropertyClassValue<ThemeHelper.IThemeData>
    {
        public string RawName { get; private set; } = string.Empty;
        public bool HasName => !string.IsNullOrEmpty(RawName);

        public override ThemeHelper.IThemeData Value
        {
            set
            {
                base.Value = value;
                RawName = value?.Id ?? string.Empty;
            }
        }

        public PropertyThemeValue(Action onChanged, ThemeHelper.IThemeData value = default) : base(onChanged, value) { }
        public PropertyThemeValue(string label, Action onChanged, ThemeHelper.IThemeData value = default) : base(label, onChanged, value) { }

        public PropertyThemeValue(Action onChanged, string name) : this(onChanged, ThemeHelper.TryGetTheme(name, out var theme) ? theme : null) { }
        public PropertyThemeValue(string label, Action onChanged, string name) : this(label, onChanged, ThemeHelper.TryGetTheme(name, out var theme) ? theme : null) { }

        protected override void ToXml(out string label, out object value)
        {
            label = Label;
            value = Value?.Id ?? RawName;
        }
        public override void FromXml(XElement config, ThemeHelper.IThemeData defaultValue)
        {
            RawName = config.GetAttrValue(Label, string.Empty);
            Value = ThemeHelper.TryGetTheme(RawName, out var theme) ? theme : defaultValue;
        }
    }
}
