using ColossalFramework.Packaging;
using ColossalFramework.PlatformServices;
using NodeMarkup.Manager;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace NodeMarkup.Manager
{
    public abstract class Template : IDeletable, IToXml
    {
        public static string XmlName { get; } = "T";

        public string XmlSection => XmlName;
        public abstract TemplateType Type { get; }

        public Action OnTemplateChanged { private get; set; }
        public virtual bool IsAsset => false;

        public Guid Id { get; private set; }

        string _name;
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                TemplateChanged();
            }
        }

        public bool HasName => !string.IsNullOrEmpty(Name);

        public abstract string DeleteCaptionDescription { get; }
        public abstract string DeleteMessageDescription { get; }

        protected Template() : this(TemplateManager.GetNewName()) { }
        protected Template(string name) : this(Guid.NewGuid(), name) { }
        protected Template(Guid id, string name)
        {
            Id = id;
            Name = name;
        }

        protected void TemplateChanged() => OnTemplateChanged?.Invoke();

        public Dependences GetDependences() => new Dependences();

        public static bool FromXml(XElement config, out Template template)
        {
            var type = (TemplateType)config.GetAttrValue<int>("T");
            switch (type)
            {
                case TemplateType.Style when StyleTemplate.FromXml(config, out StyleTemplate styleTemplate):
                    template = styleTemplate;
                    return true;
                case TemplateType.Intersection when IntersectionTemplate.FromXml(config, out IntersectionTemplate intersectionTemplate):
                    template = intersectionTemplate;
                    return true;
                default:
                    template = null;
                    return false;
            }
        }

        public virtual XElement ToXml()
        {
            var config = new XElement(XmlSection);
            config.Add(new XAttribute("T", (int)Type));
            config.Add(new XAttribute(nameof(Id), Id));
            config.Add(new XAttribute("N", Name));
            return config;
        }

        public override string ToString() => HasName ? Name : Localize.TemplateEditor_UnnamedTemplate;
    }
    public enum TemplateType
    {
        Style = 1,
        Intersection = 2
    }
    public class StyleTemplate : Template
    {
        public override TemplateType Type => TemplateType.Style;

        public override string DeleteCaptionDescription => Localize.TemplateEditor_DeleteCaptionDescription;
        public override string DeleteMessageDescription => Localize.TemplateEditor_DeleteMessageDescription;

        public Style Style { get; private set; }
        public bool IsDefault => TemplateManager.IsDefault(this);

        public StyleTemplate(string name, Style style) : base(name)
        {
            Init(style);
        }
        private StyleTemplate(Guid id, string name, Style style) : base(id, name)
        {
            Init(style);
        }
        private void Init(Style style)
        {
            Style = style.Copy();
            Style.OnStyleChanged = TemplateChanged;
        }

        public static bool FromXml(XElement config, out StyleTemplate template)
        {
            if (config.Element(Style.XmlName) is XElement styleConfig && Style.FromXml(styleConfig, new ObjectsMap(), false, out Style style))
            {
                var id = config.GetAttrValue(nameof(Id), Guid.Empty);
                var name = config.GetAttrValue<string>("N");
                template = id == Guid.Empty ? new StyleTemplate(name, style) : new StyleTemplate(id, name, style);
                return true;
            }
            else
            {
                template = default;
                return false;
            }
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(Style.ToXml());
            return config;
        }
    }
    public class IntersectionTemplate : Template
    {
        public override TemplateType Type => TemplateType.Intersection;

        public override string DeleteCaptionDescription => Localize.TemplateEditor_DeleteCaptionDescription;
        public override string DeleteMessageDescription => Localize.TemplateEditor_DeleteMessageDescription;

        public XElement Data { get; private set; }
        public EnterData[] Enters { get; private set; }
        public ObjectsMap Map { get; } = new ObjectsMap();

        public IntersectionTemplate(Markup markup) : this(markup.ToXml(), markup.Enters.Select(e => e.Data).ToArray()) { }
        public IntersectionTemplate(XElement data, EnterData[] enters) : base()
        {
            Init(data, enters);
        }
        public IntersectionTemplate(string name, XElement data, EnterData[] enters) : base(name)
        {
            Init(data, enters);
        }
        public IntersectionTemplate(Guid id, string name, XElement data, EnterData[] enters) : base(id, name)
        {
            Init(data, enters);
        }
        private void Init(XElement data, EnterData[] enters)
        {
            Data = data;
            Enters = enters;
        }

        public static bool FromXml(XElement config, out IntersectionTemplate template)
        {
            if (false)
            {

            }
            else
            {
                template = default;
                return false;
            }
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(Data);

            foreach (var enter in Enters)
                config.Add(enter.ToXml());

            return config;
        }
    }

    public class AssetStyleTemplate : StyleTemplate
    {
        public override bool IsAsset => true;
        public ulong AuthorId { get; set; }
        public string Author => TemplateManager.GetAuthor(AuthorId);
        public bool HasAuthor => !string.IsNullOrEmpty(Author);
        public bool IsWorkshop { get; set; }
        public string FileName { get; set; }

        public AssetStyleTemplate(string name, Style style) : base(name, style) { }

        public override string ToString() => $"{base.ToString()}\nby {Author}";

        public static bool FromXml(XElement config, Package.Asset asset, out AssetStyleTemplate assetTemplate)
        {
            var result = FromXml(config, out StyleTemplate template);
            if (result)
            {
                assetTemplate = new AssetStyleTemplate(template.Name, template.Style)
                {
                    AuthorId = ulong.TryParse(asset.package.packageAuthor.Substring("steamid:".Length), out ulong steamId) ? steamId : 0,
                    IsWorkshop = asset.isWorkshopAsset,
                    FileName = Path.GetFileNameWithoutExtension(asset.package.packagePath)
                };
            }
            else
                assetTemplate = default;

            return result;
        }
        public void SetDefault()
        {
            AuthorId = PlatformService.active ? PlatformService.user.userID.AsUInt64 : 0;
            IsWorkshop = false;
            FileName = $"IMT_{Style.Type}_{Name.Replace(' ', '_').Replace('.', '_')}_{Id.ToString().Substring(0, 8)}";
        }
    }


    [Serializable]
    public class MarkingInfo : PrefabInfo
    {
        public string data;
    }
}
