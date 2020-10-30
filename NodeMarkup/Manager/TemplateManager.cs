using ColossalFramework;
using ColossalFramework.IO;
using ColossalFramework.Packaging;
using ColossalFramework.PlatformServices;
using NodeMarkup.Utils;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public abstract class TemplateManager
    {
        public static StyleTemplateManager StyleManager { get; } = new StyleTemplateManager();
        public static IntersectionTemplateManager IntersectionManager { get; } = new IntersectionTemplateManager();

        private static Dictionary<ulong, string> Authors { get; } = new Dictionary<ulong, string>();
        public static string GetAuthor(ulong steamId) => Authors.TryGetValue(steamId, out string author) ? author : null;

        public abstract SavedString Saved { get; }

        public abstract void Load();

        public static void Reload()
        {
            Logger.LogDebug($"{nameof(TemplateManager)}.{nameof(Clear)}");

            StyleManager.Load();
            IntersectionManager.Load();
        }
        public static void Clear()
        {
            Logger.LogDebug($"{nameof(TemplateManager)}.{nameof(Clear)}");

            StyleManager.Clear(true);
            IntersectionManager.Clear(true);
            Authors.Clear();
        }
        public static void LoadAsset(GameObject gameObject, Package.Asset asset)
        {
            if (!(gameObject.GetComponent<MarkingInfo>() is MarkingInfo markingInfo))
                return;

            var templateConfig = Loader.Parse(markingInfo.data);
            if (TemplateAsset.FromPackage(templateConfig, asset, out TemplateAsset templateAsset))
                AddAssetTemplate(templateAsset);

            Logger.LogDebug($"{nameof(TemplateManager)}.{nameof(LoadAsset)}: {templateAsset.Template.Name}");
        }

        public static void AddAssetTemplate(TemplateAsset templateAsset)
        {
            switch(templateAsset.Template)
            {
                case StyleTemplate styleTemplate:
                    StyleManager.AddTemplate(styleTemplate);
                    break;
                case IntersectionTemplate intersectionTemplate:
                    IntersectionManager.AddTemplate(intersectionTemplate);
                    break;
                default:
                    return;
            }

            var authorId = templateAsset.AuthorId;
            if (authorId != 0 && !Authors.ContainsKey(authorId))
                Authors[authorId] = new Friend(new UserID(authorId)).personaName;
        }

        public static bool Save(TemplateAsset templateAsset)
        {
            try
            {
                var package = new Package(templateAsset.Template.Name)
                {
                    packageMainAsset = templateAsset.Template.Name,
                    packageAuthor = $"steamid:{templateAsset.AuthorId}"
                };

                var gameObject = new GameObject(typeof(MarkingInfo).Name);
                var markingInfo = gameObject.AddComponent<MarkingInfo>();
                markingInfo.data = Loader.GetString(templateAsset.Template.ToXml());

                var asset = package.AddAsset($"{templateAsset.Template.Name}_Data", markingInfo.gameObject);

                var meta = new CustomAssetMetaData()
                {
                    name = templateAsset.Template.Name,
                    timeStamp = DateTime.Now,
                    type = CustomAssetMetaData.Type.Unknown,
                    dlcMask = SteamHelper.DLC_BitMask.None,
                    steamTags = new string[] { "Marking" },
                    guid = templateAsset.Template.Id.ToString(),
                    assetRef = asset,
                };
                package.AddAsset(templateAsset.Template.Name, meta, UserAssetType.CustomAssetMetaData);

                var path = GetSavePathName(templateAsset.FileName);
                package.Save(path);

                return true;
            }
            catch (Exception error)
            {
                Logger.LogError($"Could save template asset", error);
                return false;
            }
        }
        public static string GetSavePathName(string saveName)
        {
            string path = PathUtils.AddExtension(PathEscaper.Escape(saveName), PackageManager.packageExtension);
            return Path.Combine(DataLocation.assetsPath, path);
        }

        public static bool MakeAsset(Template template)
        {
            if (template.IsAsset)
                return true;

            var asset = new TemplateAsset(template);
            return Save(asset);
        }
    }
    public abstract class TemplateManager<TemplateType> : TemplateManager
        where TemplateType : Template
    {
        protected abstract string DefaultName { get; }
        protected Dictionary<Guid, TemplateType> TemplatesDictionary { get; } = new Dictionary<Guid, TemplateType>();
        public IEnumerable<TemplateType> Templates => TemplatesDictionary.Values;

        #region SAVE&LOAD

        private void InitTempalte(TemplateType template) => template.OnTemplateChanged = OnTemplateChanged;
        private void OnTemplateChanged() => Save();

        public override void Load()
        {
            try
            {
                Clear();
                var xml = Saved.value;
                if (!string.IsNullOrEmpty(xml))
                {
                    var config = Loader.Parse(xml);
                    FromXml(config);
                }

                Logger.LogDebug($"Templates was loaded: {TemplatesDictionary.Count} items");
            }
            catch (Exception error)
            {
                Logger.LogError("Could load templates", error);
            }
        }
        protected void Save()
        {
            try
            {
                var config = Loader.GetString(ToXml());
                Saved.value = config;

                Logger.LogDebug($"Templates was saved: {TemplatesDictionary.Count} items");
            }
            catch (Exception error)
            {
                Logger.LogError("Could save templates", error);
            }
        }

        public virtual void Clear(bool clearAssets = false)
        {
            if (clearAssets)
                TemplatesDictionary.Clear();
            else
            {
                var toDelete = Templates.Where(t => !t.IsAsset).Select(t => t.Id).ToArray();
                foreach (var id in toDelete)
                    TemplatesDictionary.Remove(id);
            }
        }
        public void DeleteAll()
        {
            Clear();
            Save();
        }
        #endregion

        #region ADD&DELETE

        public void AddTemplate(TemplateType template)
        {
            InitTempalte(template);
            TemplatesDictionary[template.Id] = template;
        }
        public void DeleteTemplate(TemplateType template)
        {
            TemplatesDictionary.Remove(template.Id);
            OnDeleteTemplate(template);

            Save();
        }
        protected virtual void OnDeleteTemplate(TemplateType template) { }

        #endregion


        #region NAME

        public string GetNewName(string newName = null)
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
        public bool ContainsName(string name, TemplateType ignore) => TemplatesDictionary.Values.Any(t => t != ignore && t.Name == name);

        #endregion

        #region XML

        public virtual XElement ToXml()
        {
            var config = new XElement("C");

            foreach (var template in Templates)
            {
                if (!template.IsAsset)
                    config.Add(template.ToXml());
            }

            return config;
        }

        protected virtual void FromXml(XElement config)
        {
            foreach (var templateConfig in config.Elements(Template.XmlName))
            {
                if (Template.FromXml(templateConfig, out TemplateType template) && !TemplatesDictionary.ContainsKey(template.Id))
                    TemplatesDictionary[template.Id] = template;
            }

            foreach (var template in TemplatesDictionary.Values)
                InitTempalte(template);
        }

        #endregion
    }

    public abstract class TemplateManager<TemplateType, Item> : TemplateManager<TemplateType>
        where TemplateType : Template
    {
        public bool AddTemplate(Item item, out TemplateType template) => AddTemplate(GetNewName(), item, out template);
        protected bool AddTemplate(string name, Item item, out TemplateType template)
        {
            template = GetInstance(name, item);
            AddTemplate(template);
            Save();
            return true;
        }
        protected abstract TemplateType GetInstance(string name, Item item);
    }
    public class StyleTemplateManager : TemplateManager<StyleTemplate, Style>
    {
        protected override string DefaultName => Localize.Template_NewTemplate;
        public override SavedString Saved => Settings.Templates;

        private Dictionary<Style.StyleType, Guid> DefaultTemplates { get; } = new Dictionary<Style.StyleType, Guid>();
        public bool IsDefault(StyleTemplate template) => DefaultTemplates.TryGetValue(template.Style.Type, out Guid id) && template.Id == id;

        protected override StyleTemplate GetInstance(string name, Style style) => new StyleTemplate(name, style);

        public override void Clear(bool clearAssets = false)
        {
            base.Clear(clearAssets);

            var pairs = DefaultTemplates.ToArray();
            foreach (var pair in pairs)
            {
                if (!TemplatesDictionary.ContainsKey(pair.Value))
                    DefaultTemplates.Remove(pair.Key);
            }
        }

        public bool DuplicateTemplate(StyleTemplate template, out StyleTemplate duplicate)
            => AddTemplate(GetNewName($"{template.Name} {Localize.Template_DuplicateTemplateSuffix}"), template.Style, out duplicate);

        protected override void OnDeleteTemplate(StyleTemplate template)
        {
            if (template.IsDefault)
                DefaultTemplates.Remove(template.Style.Type);
        }
        public void ToggleAsDefaultTemplate(StyleTemplate template)
        {
            if (template.IsDefault)
                DefaultTemplates.Remove(template.Style.Type);
            else
                DefaultTemplates[template.Style.Type] = template.Id;

            Save();
        }

        public T GetDefault<T>(Style.StyleType type) where T : Style
        {
            if (DefaultTemplates.TryGetValue(type, out Guid id) && TemplatesDictionary.TryGetValue(id, out StyleTemplate template) && template.Style.Copy() is T tStyle)
                return tStyle;
            else
                return Style.GetDefault<T>(type);
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();

            foreach (var def in DefaultTemplates)
            {
                var defaultConfig = new XElement("D");
                defaultConfig.Add(new XAttribute("T", (int)def.Key));
                defaultConfig.Add(new XAttribute("Id", def.Value));

                config.Add(defaultConfig);
            }

            return config;
        }

        protected override void FromXml(XElement config)
        {
            base.FromXml(config);

            foreach (var defaultConfig in config.Elements("D"))
            {
                var styleType = (Style.StyleType)defaultConfig.GetAttrValue<int>("T");
                var templateId = defaultConfig.GetAttrValue<Guid>("Id");

                if (TemplatesDictionary.ContainsKey(templateId))
                    DefaultTemplates[styleType] = templateId;
            }
        }
    }
    public class IntersectionTemplateManager : TemplateManager<IntersectionTemplate, Markup>
    {
        protected override string DefaultName => Localize.Preset_NewPreset;
        public override SavedString Saved => Settings.Intersections;

        protected override IntersectionTemplate GetInstance(string name, Markup markup) => new IntersectionTemplate(name, markup);
    }
}
