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
        static string DefaultName { get; } = "New template";

        static Dictionary<string, LineStyleTemplate> TemplatesDictionary { get; } = new Dictionary<string, LineStyleTemplate>();
        static Dictionary<LineStyle.LineType, LineStyleTemplate> DefaultTemplates { get; } = new Dictionary<LineStyle.LineType, LineStyleTemplate>();

        public static IEnumerable<LineStyleTemplate> Templates => TemplatesDictionary.Values;

        static TemplateManager()
        {
            try
            {
                var xml = UI.Settings.Templates.value;
                var config = Serializer.Parse(xml);
                FromXml(config);

                Logger.LogDebug($"Templates was loaded: {TemplatesDictionary.Count} items");
            }
            catch (Exception error)
            {
                Logger.LogError(() => "Could load templates", error);
            }
        }

        public static bool AddTemplate(LineStyle style, out LineStyleTemplate template, string name = null)
        {
            var templateName = name ?? GetNewName();
            if (TemplatesDictionary.ContainsKey(templateName))
            {
                template = default;
                return false;
            }

            template = new LineStyleTemplate(templateName, style)
            {
                OnTemplateChanged = OnTemplateChanged,
                OnStyleChanged = OnTemplateStyleChanged,
                OnNameChanged = OnTemplateNameChanged
            };
            TemplatesDictionary.Add(template.Name, template);

            Save();

            return true;
        }
        public static void DeleteTemplate(LineStyleTemplate template)
        {
            TemplatesDictionary.Remove(template.Name);
            if (template.IsDefault())
                DefaultTemplates.Remove(template.Style.Type);

            Save();
        }
        public static void ToggleAsDefaultTemplate(LineStyleTemplate template)
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
            foreach(var template in Templates.Where(t => t.Name.StartsWith(DefaultName)))
            {
                if (int.TryParse(template.Name.Substring(DefaultName.Length), out int num) && num >= i)
                    i = num + 1;
            }
            return $"{DefaultName} {i}";
        }
        public static LineStyle GetDefault(LineStyle.LineType type)
        {
            if (DefaultTemplates.TryGetValue(type, out LineStyleTemplate template))
                return template.Style.Copy();
            else
                return LineStyle.GetDefault(type);
        }
        public static bool IsDefault(this LineStyleTemplate template) =>
            DefaultTemplates.TryGetValue(template.Style.Type, out LineStyleTemplate defaultTemplate) && template == defaultTemplate;
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
        static void OnTemplateStyleChanged(LineStyleTemplate template, LineStyle newStyle)
        {
            if (template.IsDefault())
            {
                DefaultTemplates.Remove(template.Style.Type);

                if (!DefaultTemplates.ContainsKey(newStyle.Type))
                    DefaultTemplates[newStyle.Type] = template;
            }
        }
        static bool OnTemplateNameChanged(LineStyleTemplate template, string newName)
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
            foreach (var templateConfig in config.Elements(LineStyleTemplate.XmlName))
            {
                if (LineStyleTemplate.FromXml(templateConfig, out LineStyleTemplate template) && !TemplatesDictionary.ContainsKey(template.Name))
                {
                    TemplatesDictionary[template.Name] = template;
                }
            }

            foreach (var defaultConfig in config.Elements("D"))
            {
                var styleType = (LineStyle.LineType)defaultConfig.GetAttrValue<int>("T");
                var templateName = defaultConfig.GetAttrValue<string>("N");

                if (TemplatesDictionary.TryGetValue(templateName, out LineStyleTemplate template))
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
