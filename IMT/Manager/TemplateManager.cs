using ColossalFramework;
using ColossalFramework.Importers;
using ColossalFramework.PlatformServices;
using IMT.Utilities;
using ModsCommon;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace IMT.Manager
{
    public abstract class DataManager : IManager
    {
        public static ulong UserId { get; } = PlatformService.active ? PlatformService.user.userID.AsUInt64 : 0;
        protected static Dictionary<ulong, string> Authors { get; } = new Dictionary<ulong, string>();
        public static string GetAuthor(ulong steamId)
        {
            if (PlatformService.active)
            {
                try
                {
                    if (!Authors.TryGetValue(steamId, out string author))
                    {
                        author = new Friend(new UserID(steamId)).personaName;
                        Authors[steamId] = author;
                    }
                    return author;
                }
                catch (Exception error)
                {
                    SingletonMod<Mod>.Logger.Error("Could not get author name", error);
                }
            }
            return Localize.Template_UnknownAuthor;
        }

        public abstract SavedString Saved { get; }
        protected abstract void LoadData();
        protected abstract void SaveData();
        protected abstract void ClearData();

        public static void Reload()
        {
            SingletonMod<Mod>.Logger.Debug($"{nameof(TemplateManager)} {nameof(Reload)}");

            SingletonManager<StyleTemplateManager>.Instance.LoadData();
            SingletonManager<IntersectionTemplateManager>.Instance.LoadData();
            SingletonManager<RoadTemplateManager>.Instance.LoadData();
        }
        public static void Clear()
        {
            SingletonMod<Mod>.Logger.Debug($"{nameof(TemplateManager)} {nameof(Clear)}");

            SingletonManager<StyleTemplateManager>.Instance.ClearData();
            SingletonManager<IntersectionTemplateManager>.Instance.ClearData();
            SingletonManager<RoadTemplateManager>.Instance.ClearData();
            Authors.Clear();
        }
    }

    public abstract class TemplateManager : DataManager
    {
        public abstract void AddTemplate(Template template);
        public void Load() => LoadData();
    }
    public abstract class TemplateManager<TemplateType> : TemplateManager
        where TemplateType : Template<TemplateType>
    {
        protected abstract string DefaultName { get; }
        protected Dictionary<Guid, TemplateType> TemplatesDictionary { get; } = new Dictionary<Guid, TemplateType>();
        public IEnumerable<TemplateType> Templates => TemplatesDictionary.Values;

        public bool TryGetTemplate(Guid templateId, out TemplateType template) => TemplatesDictionary.TryGetValue(templateId, out template);

        #region SAVE&LOAD

        public void TemplateChanged(TemplateType template)
        {
            if (template.IsAsset)
                Loader.SaveTemplateAsset(template.Asset);
            else
                SaveData();
        }

        protected override void LoadData()
        {
            try
            {
                Clear();
                var xml = Saved.value;
                if (!string.IsNullOrEmpty(xml))
                {
                    var config = XmlExtension.Parse(xml);
                    FromXml(config);
                }

                SingletonMod<Mod>.Logger.Debug($"{typeof(TemplateType).Name} was loaded: {TemplatesDictionary.Count} items");
            }
            catch (Exception error)
            {
                SingletonMod<Mod>.Logger.Error($"Could not load {typeof(TemplateType).Name}", error);
            }
        }
        protected override void SaveData()
        {
            try
            {
                var config = Loader.GetString(ToXml());
                Saved.value = config;

                SingletonMod<Mod>.Logger.Debug($"{typeof(TemplateType).Name} was saved: {TemplatesDictionary.Count} items");
            }
            catch (Exception error)
            {
                SingletonMod<Mod>.Logger.Error($"Could not save {typeof(TemplateType).Name}", error);
            }
        }
        protected override void ClearData() => Clear(true);

        protected virtual void Clear(bool clearAssets = false)
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
            SaveData();
        }

        public bool MakeAsset(TemplateType template)
        {
            if (template.IsAsset)
                return true;

            var asset = new TemplateAsset(template);
            var saved = Loader.SaveTemplateAsset(asset);
            if (saved)
                SaveData();

            return saved;
        }
        #endregion

        #region ADD&DELETE
        public override void AddTemplate(Template template)
        {
            if (template is TemplateType templateType)
                AddTemplate(templateType);
        }
        public void AddTemplate(TemplateType template)
        {
            if (NeedAdd(template))
                TemplatesDictionary[template.Id] = template;
        }
        private bool NeedAdd(TemplateType template)
        {
            if (TemplatesDictionary.TryGetValue(template.Id, out TemplateType existTemplate))
            {
                if (!template.IsAsset)
                    return false;

                if (existTemplate.IsAsset)
                {
                    if (!template.Asset.IsWorkshop)
                        return false;

                    if (existTemplate.Asset.IsWorkshop)
                    {
                        if (!template.Asset.IsLocalFolder)
                            return false;

                        if (existTemplate.Asset.IsLocalFolder)
                            return false;
                    }
                }
            }

            return true;
        }

        public void DeleteTemplate(TemplateType template)
        {
            TemplatesDictionary.Remove(template.Id);
            OnDeleteTemplate(template);

            SaveData();
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
        }

        #endregion
    }

    public abstract class TemplateManager<TemplateType, Item> : TemplateManager<TemplateType>
        where TemplateType : Template<TemplateType>
    {
        public bool AddTemplate(Item item, out TemplateType template) => AddTemplate(GetNewName(), item, out template);
        protected bool AddTemplate(string name, Item item, out TemplateType template)
        {
            template = CreateInstance(name, item);
            AddTemplate(template);
            SaveData();
            return true;
        }
        protected abstract TemplateType CreateInstance(string name, Item item);
    }
    public class StyleTemplateManager : TemplateManager<StyleTemplate, Style>
    {
        protected override string DefaultName => Localize.Template_NewTemplate;
        public override SavedString Saved => Settings.Templates;

        private Dictionary<Style.StyleType, Guid> DefaultTemplates { get; } = new Dictionary<Style.StyleType, Guid>();
        public bool IsDefault(StyleTemplate template) => DefaultTemplates.TryGetValue(template.Style.Type, out Guid id) && template.Id == id;
        public IEnumerable<StyleTemplate> GetTemplates(Style.StyleType group) => Templates.Where(t => t.Style.Type.GetGroup() == group);

        protected override StyleTemplate CreateInstance(string name, Style style) => new StyleTemplate(name, style);

        protected override void Clear(bool clearAssets = false)
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

            SaveData();
        }

        public T GetDefault<T>(Style.StyleType type) where T : Style
        {
            if (DefaultTemplates.TryGetValue(type, out Guid id) && TemplatesDictionary.TryGetValue(id, out StyleTemplate template) && template.Style is T style)
                return (T)style.Copy();
            else
                return Style.GetDefault<T>(type);
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();

            foreach (var def in DefaultTemplates)
            {
                var defaultConfig = new XElement("D");
                defaultConfig.AddAttr("T", (int)def.Key);
                defaultConfig.AddAttr("Id", def.Value);

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
    public class IntersectionTemplateManager : TemplateManager<IntersectionTemplate, Marking>
    {
        protected override string DefaultName => Localize.Preset_NewPreset;
        public override SavedString Saved => Settings.Intersections;

        protected override IntersectionTemplate CreateInstance(string name, Marking marking) => new IntersectionTemplate(name, marking);

        public bool AddTemplate(Marking marking, Image image, out IntersectionTemplate template)
        {
            if (AddTemplate(GetNewName(), marking, out template))
            {
                if (Loader.SaveScreenshot(template, image))
                    template.Preview = image.CreateTexture();
                return true;
            }
            else
                return false;
        }
    }

    public class RoadTemplateManager : DataManager
    {
        public override SavedString Saved => Settings.Roads;
        private Dictionary<string, Data> Templates { get; set; } = new Dictionary<string, Data>();

        public bool TryGetOffsets(string name, out float[] offsets)
        {
            if (Templates.TryGetValue(name, out var data))
            {
                offsets = data.offsets;
                return offsets != null;
            }
            else
            {
                offsets = null;
                return false;
            }
        }
        public void SaveOffsets(string name, float[] offsets)
        {
            Templates.TryGetValue(name, out var data);
            data.offsets = offsets;
            Templates[name] = data;
            SaveData();
        }
        public void RevertOffsets(string name)
        {
            Templates.TryGetValue(name, out var data);

            if (data.preset == Guid.Empty)
                Templates.Remove(name);
            else
            {
                data.offsets = null;
                Templates[name] = data;
            }

            SaveData();
        }
        public bool TryGetPreset(string name, out Guid preset)
        {
            if (Templates.TryGetValue(name, out var data))
            {
                preset = data.preset;
                return preset != Guid.Empty;
            }
            else
            {
                preset = Guid.Empty;
                return false;
            }
        }
        public bool TryGetPreset(string name, out Guid preset, out bool flip, out bool invert)
        {
            if (Templates.TryGetValue(name, out var data))
            {
                preset = data.preset;
                invert = data.invert;
                flip = data.flip;
                return preset != Guid.Empty;
            }
            else
            {
                preset = Guid.Empty;
                invert = false;
                flip = false;
                return false;
            }
        }
        public void SavePreset(string name, Guid preset, bool flip, bool invert)
        {
            Templates.TryGetValue(name, out var data);
            data.preset = preset;
            data.flip = flip;
            data.invert = invert;
            Templates[name] = data;
            SaveData();
        }
        public void RevertPreset(string name)
        {
            Templates.TryGetValue(name, out var data);

            if (data.offsets == null)
                Templates.Remove(name);
            else
            {
                data.preset = Guid.Empty;
                Templates[name] = data;
            }

            SaveData();
        }

        public bool ContainsOffset(string name) => Templates.TryGetValue(name, out var data) && data.offsets != null;
        public bool ContainsPreset(string name) => Templates.TryGetValue(name, out var data) && data.preset != Guid.Empty;

        protected override void LoadData()
        {
            try
            {
                ClearData();
                var xml = Saved.value;
                if (!string.IsNullOrEmpty(xml))
                {
                    var config = XmlExtension.Parse(xml);
                    FromXml(config);
                }

                SingletonMod<Mod>.Logger.Debug($"Road templates was loaded: {Templates.Count} items");
            }
            catch (Exception error)
            {
                SingletonMod<Mod>.Logger.Error($"Could not load road templates", error);
            }
        }

        protected override void SaveData()
        {
            try
            {
                var config = Loader.GetString(ToXml());
                Saved.value = config;

                SingletonMod<Mod>.Logger.Debug($"Road templates was saved: {Templates.Count} items");
            }
            catch (Exception error)
            {
                SingletonMod<Mod>.Logger.Error($"Could not save road templates", error);
            }
        }
        protected override void ClearData()
        {
            Templates.Clear();
        }

        #region XML

        private XElement ToXml()
        {
            var config = new XElement("C");

            foreach (var template in Templates)
            {
                var roadConfig = new XElement("R");
                roadConfig.AddAttr("N", template.Key);
                if (template.Value.offsets != null)
                    roadConfig.AddAttr("O", string.Join("|", template.Value.offsets.Select(v => v.ToString("0.###")).ToArray()));
                if (template.Value.preset != Guid.Empty)
                {
                    roadConfig.AddAttr("P", template.Value.preset);
                    roadConfig.AddAttr("F", template.Value.flip ? 1 : 0);
                    roadConfig.AddAttr("I", template.Value.invert ? 1 : 0);
                }
                config.Add(roadConfig);
            }

            return config;
        }

        private void FromXml(XElement config)
        {
            foreach (var templateConfig in config.Elements("R"))
            {
                var name = templateConfig.GetAttrValue("N", string.Empty);
                if (string.IsNullOrEmpty(name))
                    continue;

                var offsets = default(float[]);
                var offsetStr = templateConfig.GetAttrValue("O", string.Empty).Split('|');
                if (offsetStr.Length > 0)
                {
                    offsets = new float[offsetStr.Length];
                    for (int i = 0; i < offsetStr.Length; i += 1)
                    {
                        if (float.TryParse(offsetStr[i], out var value))
                            offsets[i] = value;
                    }
                }

                var preset = new Guid(templateConfig.GetAttrValue("P", string.Empty));
                var flip = templateConfig.GetAttrValue("F", 0) == 1;
                var invert = templateConfig.GetAttrValue("I", 0) == 1;

                if (offsets != null || preset != Guid.Empty)
                {
                    Templates[name] = new Data()
                    {
                        preset = preset,
                        offsets = offsets,
                        flip = flip,
                        invert = invert,
                    };
                }
            }
        }

        #endregion

        private struct Data
        {
            public float[] offsets;
            public Guid preset;
            public bool flip;
            public bool invert;
        }
    }
}
