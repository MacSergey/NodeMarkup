using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace NodeMarkup.Manager
{
    public static class TemplateManager
    {
        static string DefaultName => Localize.Template_NewTemplate;

        static Dictionary<string, StyleTemplate> TemplatesDictionary { get; } = new Dictionary<string, StyleTemplate>();
        static Dictionary<Style.StyleType, StyleTemplate> DefaultTemplates { get; } = new Dictionary<Style.StyleType, StyleTemplate>();

        public static IEnumerable<StyleTemplate> Templates => TemplatesDictionary.Values;
        public static IEnumerable<StyleTemplate> GetTemplates(Style.StyleType groupType) => Templates.Where(t => (t.Style.Type & groupType & Style.StyleType.GroupMask) != 0).OrderBy(t => !t.IsDefault());

        static TemplateManager()
        {
            try
            {
                var xml = UI.Settings.Templates.value;
                if (!string.IsNullOrEmpty(xml))
                {
                    var config = Serializer.Parse(xml);
                    FromXml(config);
                }

                Logger.LogDebug($"Templates was loaded: {TemplatesDictionary.Count} items");
            }
            catch (Exception error)
            {
                Logger.LogError(() => "Could load templates", error);
            }
        }
        private static void InitTempalte(StyleTemplate template)
        {
            template.OnTemplateChanged = OnTemplateChanged;
            template.OnStyleChanged = OnTemplateStyleChanged;
            template.OnNameChanged = OnTemplateNameChanged;
        }
        public static bool AddTemplate(Style style, out StyleTemplate template, string name = null)
        {
            var templateName = name ?? GetNewName();
            if (TemplatesDictionary.ContainsKey(templateName))
            {
                template = default;
                return false;
            }

            template = new StyleTemplate(templateName, style);
            InitTempalte(template);
            TemplatesDictionary[template.Name] = template;

            Save();

            return true;
        }
        public static void DeleteTemplate(StyleTemplate template)
        {
            TemplatesDictionary.Remove(template.Name);
            if (template.IsDefault())
                DefaultTemplates.Remove(template.Style.Type);

            Save();
        }
        public static void ToggleAsDefaultTemplate(StyleTemplate template)
        {
            if (template.IsDefault())
                DefaultTemplates.Remove(template.Style.Type);
            else
                DefaultTemplates[template.Style.Type] = template;

            Save();
        }
        private static string GetNewName()
        {
            var i = 1;
            foreach (var template in Templates.Where(t => t.Name.StartsWith(DefaultName)))
            {
                if (int.TryParse(template.Name.Substring(DefaultName.Length), out int num) && num >= i)
                    i = num + 1;
            }
            return $"{DefaultName} {i}";
        }
        public static T GetDefault<T>(Style.StyleType type) where T : Style
        {
            if (DefaultTemplates.TryGetValue(type, out StyleTemplate template) && template.Style.Copy() is T tStyle)
                return tStyle;
            else
                return Style.GetDefault<T>(type);
        }
        public static bool IsDefault(this StyleTemplate template) =>
            DefaultTemplates.TryGetValue(template.Style.Type, out StyleTemplate defaultTemplate) && template == defaultTemplate;
        static void Save()
        {
            try
            {
                var config = ToXml();
                var xml = config.ToString(SaveOptions.DisableFormatting);
                UI.Settings.Templates.value = xml;

                Logger.LogDebug($"Templates was saved: {TemplatesDictionary.Count} items");
            }
            catch (Exception error)
            {
                Logger.LogError(() => "Could save templates", error);
            }
        }
        static void OnTemplateChanged() => Save();
        static void OnTemplateStyleChanged(StyleTemplate template, Style newStyle)
        {
            if (template.IsDefault())
            {
                DefaultTemplates.Remove(template.Style.Type);

                if (!DefaultTemplates.ContainsKey(newStyle.Type))
                    DefaultTemplates[newStyle.Type] = template;
            }
        }
        static bool OnTemplateNameChanged(StyleTemplate template, string newName)
        {
            if (!string.IsNullOrEmpty(newName) && newName != template.Name && !TemplatesDictionary.ContainsKey(newName))
            {
                TemplatesDictionary.Remove(template.Name);
                TemplatesDictionary[newName] = template;

                return true;
            }
            else
                return false;
        }

        static void FromXml(XElement config)
        {
            foreach (var templateConfig in config.Elements(StyleTemplate.XmlName))
            {
                if (StyleTemplate.FromXml(templateConfig, out StyleTemplate template) && !TemplatesDictionary.ContainsKey(template.Name))
                {
                    InitTempalte(template);
                    TemplatesDictionary[template.Name] = template;
                }
            }

            foreach (var defaultConfig in config.Elements("D"))
            {
                var styleType = (Style.StyleType)defaultConfig.GetAttrValue<int>("T");
                var templateName = defaultConfig.GetAttrValue<string>("N");

                if (TemplatesDictionary.TryGetValue(templateName, out StyleTemplate template))
                {
                    DefaultTemplates[styleType] = template;
                }
            }
        }
        static XElement ToXml()
        {
            var config = new XElement("C");

            foreach (var template in Templates)
            {
                config.Add(template.ToXml());
            }
            foreach (var def in DefaultTemplates)
            {
                var defaultConfig = new XElement("D",
                    new XAttribute("T", (int)def.Key),
                    new XAttribute("N", def.Value.Name)
                    );
                config.Add(defaultConfig);
            }

            return config;
        }
    }
}
