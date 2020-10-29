using ColossalFramework.IO;
using ColossalFramework.Packaging;
using ColossalFramework.PlatformServices;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public static class TemplateManager
    {
        private static string DefaultName => Localize.Template_NewTemplate;

        private static Dictionary<Guid, StyleTemplate> TemplatesDictionary { get; } = new Dictionary<Guid, StyleTemplate>();
        private static Dictionary<Style.StyleType, Guid> DefaultTemplates { get; } = new Dictionary<Style.StyleType, Guid>();
        private static Dictionary<Guid, StyleTemplate> AssetTemplates { get; } = new Dictionary<Guid, StyleTemplate>();
        private static Dictionary<ulong, string> Authors { get; } = new Dictionary<ulong, string>();

        public static IEnumerable<StyleTemplate> Templates => TemplatesDictionary.Values;
        public static IEnumerable<StyleTemplate> GetTemplates(Style.StyleType groupType) => Templates.Where(t => (t.Style.Type & groupType & Style.StyleType.GroupMask) != 0).OrderBy(t => !t.IsDefault);

        private static void InitTempalte(StyleTemplate template) => template.OnTemplateChanged = OnTemplateChanged;
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
            if (template.IsDefault)
                DefaultTemplates.Remove(template.Style.Type);

            Save();
        }
        public static void ToggleAsDefaultTemplate(StyleTemplate template)
        {
            if (template.IsDefault)
                DefaultTemplates.Remove(template.Style.Type);
            else
                DefaultTemplates[template.Style.Type] = template.Id;

            Save();
        }
        public static string GetNewName(string newName = null)
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
            if (DefaultTemplates.TryGetValue(type, out Guid id) && TemplatesDictionary.TryGetValue(id, out StyleTemplate template) && template.Style.Copy() is T tStyle)
                return tStyle;
            else
                return Style.GetDefault<T>(type);
        }
        public static bool IsDefault(StyleTemplate template) => DefaultTemplates.TryGetValue(template.Style.Type, out Guid id) && template.Id == id;

        public static void Load()
        {
            try
            {
                Clear();
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

        public static bool ContainsName(string name, StyleTemplate ignore) => TemplatesDictionary.Values.Any(t => t != ignore && t.Name == name);
        public static void Clear(bool clearAssets = false)
        {
            TemplatesDictionary.Clear();

            if (clearAssets)
            {
                AssetTemplates.Clear();
                Authors.Clear();
            }

            var keys = DefaultTemplates.Keys.ToArray();
            foreach (var key in keys)
            {
                if (!AssetTemplates.ContainsKey(DefaultTemplates[key]))
                    DefaultTemplates.Remove(key);
            }
        }
        public static void DeleteAll()
        {
            Clear();
            Save();
            Load();
        }
        public static void FromXml(XElement config)
        {
            Clear();

            foreach (var templateConfig in config.Elements(StyleTemplate.XmlName))
            {
                if (StyleTemplate.FromXml(templateConfig, out StyleTemplate template) && !TemplatesDictionary.ContainsKey(template.Id))
                    TemplatesDictionary[template.Id] = template;
            }

            foreach (var template in AssetTemplates)
                TemplatesDictionary[template.Key] = template.Value;

            foreach (var template in TemplatesDictionary.Values)
                InitTempalte(template);

            foreach (var defaultConfig in config.Elements("D"))
            {
                var styleType = (Style.StyleType)defaultConfig.GetAttrValue<int>("T");
                var templateId = defaultConfig.GetAttrValue<Guid>("Id");

                if (TemplatesDictionary.ContainsKey(templateId))
                    DefaultTemplates[styleType] = templateId;
            }
        }
        public static XElement ToXml()
        {
            var config = new XElement("C");

            foreach (var template in Templates)
            {
                if (!template.IsAsset)
                    config.Add(template.ToXml());
            }

            foreach (var def in DefaultTemplates)
            {
                var defaultConfig = new XElement("D");
                defaultConfig.Add(new XAttribute("T", (int)def.Key));
                defaultConfig.Add(new XAttribute("Id", def.Value));

                config.Add(defaultConfig);
            }

            return config;
        }
        public static bool MakeAsset(StyleTemplate template, out AssetStyleTemplate assetTemplate)
        {
            if (!template.IsAsset)
            {
                try
                {
                    assetTemplate = new AssetStyleTemplate(template.Name, template.Style);
                    assetTemplate.SetDefault();

                    SaveAsset(assetTemplate);

                    AddAssetTemplate(assetTemplate);

                    TemplatesDictionary.Remove(template.Id);
                    TemplatesDictionary[assetTemplate.Id] = assetTemplate;
                    return true;
                }
                catch (Exception error)
                {
                    Logger.LogError(() => $"Could make template asset", error);
                }
            }

            assetTemplate = default;
            return false;
        }
        public static void LoadAsset(GameObject gameObject, Package.Asset asset)
        {
            if (!(gameObject.GetComponent<MarkingInfo>() is MarkingInfo markingInfo))
                return;

            var templateConfig = Loader.Parse(markingInfo.data);
            if (!AssetStyleTemplate.FromXml(templateConfig, asset, out AssetStyleTemplate template))
                return;

            AddAssetTemplate(template);

            Logger.LogDebug($"{nameof(TemplateManager)}.{nameof(LoadAsset)}: {template.Name}");
        }
        public static void AddAssetTemplate(AssetStyleTemplate template)
        {
            AssetTemplates[template.Id] = template;

            if (template.AuthorId != 0 && !Authors.ContainsKey(template.AuthorId))
                Authors[template.AuthorId] = new Friend(new UserID(template.AuthorId)).personaName;
        }
        public static void SaveAsset(AssetStyleTemplate template)
        {
            var package = new Package(template.Name);
            package.packageMainAsset = template.Name;
            package.packageAuthor = $"steamid:{template.AuthorId}";

            var gameObject = new GameObject(typeof(MarkingInfo).Name);
            var markingInfo = gameObject.AddComponent<MarkingInfo>();
            markingInfo.data = template.ToXml().ToString(SaveOptions.DisableFormatting);

            var asset = package.AddAsset($"{template.Name}_Data", markingInfo.gameObject);

            var meta = new CustomAssetMetaData()
            {
                name = template.Name,
                timeStamp = DateTime.Now,
                type = CustomAssetMetaData.Type.Unknown,
                dlcMask = SteamHelper.DLC_BitMask.None,
                steamTags = new string[] { "Marking" },
                guid = template.Id.ToString(),
                assetRef = asset,
            };
            package.AddAsset(template.Name, meta, UserAssetType.CustomAssetMetaData);

            var path = GetSavePathName(template.FileName);
            package.Save(path);
        }
        public static string GetSavePathName(string saveName)
        {
            string path = PathUtils.AddExtension(PathEscaper.Escape(saveName), PackageManager.packageExtension);
            return Path.Combine(DataLocation.assetsPath, path);
        }
        public static string GetAuthor(ulong steamId) => Authors.TryGetValue(steamId, out string author) ? author : null;
    }
}
