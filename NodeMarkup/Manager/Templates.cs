using ColossalFramework.PlatformServices;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace NodeMarkup.Manager
{
    public static class TemplateManager
    {
        static string DefaultName => Localize.Template_NewTemplate;

        static Dictionary<Guid, StyleTemplate> TemplatesDictionary { get; } = new Dictionary<Guid, StyleTemplate>();
        static Dictionary<Style.StyleType, StyleTemplate> DefaultTemplates { get; } = new Dictionary<Style.StyleType, StyleTemplate>();

        public static IEnumerable<StyleTemplate> Templates => TemplatesDictionary.Values;
        public static IEnumerable<StyleTemplate> GetTemplates(Style.StyleType groupType) => Templates.Where(t => (t.Style.Type & groupType & Style.StyleType.GroupMask) != 0).OrderBy(t => !t.IsDefault());

        static TemplateManager()
        {
            Load();
        }
        private static void InitTempalte(StyleTemplate template)
        {
            template.OnTemplateChanged = OnTemplateChanged;
            template.OnStyleChanged = OnTemplateStyleChanged;
        }
        public static bool AddTemplate(Style style, out StyleTemplate template) => AddTemplate(GetNewName(), style, out template);
        public static bool DuplicateTemplate(StyleTemplate template, out StyleTemplate duplicate) 
            => AddTemplate(GetNewName($"{template.Name} {Localize.Template_DuplicateTemplateSuffix}"), template.Style, out duplicate);
        private static bool AddTemplate(string name, Style style, out StyleTemplate template)
        {
            template = new StyleTemplate(name, style);
            InitTempalte(template);
            TemplatesDictionary[template.Id] = template;

            Save();

            return true;
        }
        public static void DeleteTemplate(StyleTemplate template)
        {
            TemplatesDictionary.Remove(template.Id);
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
        private static string GetNewName(string newName = null)
        {
            if (string.IsNullOrEmpty(newName))
                newName = DefaultName;

            var i = 0;
            foreach (var template in Templates.Where(t => t.Name.StartsWith(newName)))
            {
                if (template.Name.Length == newName.Length && i == 0)
                    i = 1;
                else if (int.TryParse(template.Name.Substring(newName.Length), out int num) && num >= i)
                    i = num + 1;
            }
            return i == 0 ? newName : $"{newName} {i}";
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

        static void Load()
        {
            try
            {
                var xml = Settings.Templates.value;
                if (!string.IsNullOrEmpty(xml))
                {
                    var config = Loader.Parse(xml);
                    FromXml(config);
                }

                Logger.LogDebug($"Templates was loaded: {TemplatesDictionary.Count} items");
            }
            catch (Exception error)
            {
                Logger.LogError(() => "Could load templates", error);
            }
        }
        static void Save()
        {
            try
            {
                var config = ToXml();
                var xml = config.ToString(SaveOptions.DisableFormatting);
                Settings.Templates.value = xml;

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

        public static bool ContainsName(string name, StyleTemplate ignore) => TemplatesDictionary.Values.Any(t => t != ignore && t.Name == name);
        private static void Clear()
        {
            TemplatesDictionary.Clear();
            DefaultTemplates.Clear();
        }
        public static void DeleteAll()
        {
            Clear();
            Save();
        }
        public static void Import(XElement config)
        {
            FromXml(config);
            Save();
        }
        public static void FromXml(XElement config)
        {
            Clear();

            foreach (var templateConfig in config.Elements(StyleTemplate.XmlName))
            {
                if (StyleTemplate.FromXml(templateConfig, out StyleTemplate template) && !TemplatesDictionary.ContainsKey(template.Id))
                {
                    InitTempalte(template);
                    TemplatesDictionary[template.Id] = template;
                }
            }

            foreach (var defaultConfig in config.Elements("D"))
            {
                var styleType = (Style.StyleType)defaultConfig.GetAttrValue<int>("T");
                var templateId = defaultConfig.GetAttrValue<Guid>("Id");

                if (TemplatesDictionary.TryGetValue(templateId, out StyleTemplate template))
                    DefaultTemplates[styleType] = template;
            }
        }
        public static XElement ToXml()
        {
            var config = new XElement("C");

            foreach (var template in Templates)
                config.Add(template.ToXml());

            foreach (var def in DefaultTemplates)
            {
                var defaultConfig = new XElement("D");
                defaultConfig.Add(new XAttribute("T", (int)def.Key));
                defaultConfig.Add(new XAttribute("Id", def.Value.Id));
                    
                config.Add(defaultConfig);
            }

            return config;
        }
    }
}
