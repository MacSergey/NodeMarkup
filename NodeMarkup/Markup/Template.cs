using ColossalFramework.IO;
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
using UnityEngine;

namespace NodeMarkup.Manager
{
    public abstract class Template : IDeletable, IToXml
    {
        public static string XmlName { get; } = "T";

        public string XmlSection => XmlName;
        public abstract TemplateType Type { get; }
        public abstract TemplateManager Manager { get; }

        public Action OnTemplateChanged { private get; set; }

        public TemplateAsset Asset { get; set; }
        public bool IsAsset => Asset != null;

        public Guid Id { get; private set; }
        public abstract string Description { get; }

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

        protected Template() { }
        protected Template(string name) : this(Guid.NewGuid(), name) { }
        protected Template(Guid id, string name)
        {
            Id = id;
            Name = name;
        }

        protected void TemplateChanged() => OnTemplateChanged?.Invoke();

        public Dependences GetDependences() => new Dependences();
        public static bool FromXml<RequestType>(XElement config, out RequestType template)
            where RequestType : Template
        {
            var type = (TemplateType)config.GetAttrValue("T", (int)TemplateType.Style);
            switch (type)
            {
                case TemplateType.Style when StyleTemplate.FromXml(config, out StyleTemplate styleTemplate):
                    template = styleTemplate as RequestType;
                    return template != null;
                case TemplateType.Intersection when IntersectionTemplate.FromXml(config, out IntersectionTemplate intersectionTemplate):
                    template = intersectionTemplate as RequestType;
                    return template != null;
                default:
                    template = default;
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
        public virtual bool FromXml(XElement config)
        {
            Name = config.GetAttrValue("N", string.Empty);
            Id = config.GetAttrValue(nameof(Id), Guid.NewGuid());
            return true;
        }

        public override string ToString()
        {
            var name = HasName? Name : Localize.TemplateEditor_UnnamedTemplate;
            return IsAsset && Asset.HasAuthor ? string.Format(Localize.TemplateEditor_TemplateByAuthor, name, Asset.Author) : name;
        }
    }
    public enum TemplateType
    {
        Style = 1,
        Intersection = 2
    }
    public class StyleTemplate : Template
    {
        public override TemplateType Type => TemplateType.Style;
        public override TemplateManager Manager => TemplateManager.StyleManager;

        public override string DeleteCaptionDescription => Localize.TemplateEditor_DeleteCaptionDescription;
        public override string DeleteMessageDescription => Localize.TemplateEditor_DeleteMessageDescription;
        public override string Description => Style.Type.ToString();

        public Style Style { get; private set; }
        public bool IsDefault => TemplateManager.StyleManager.IsDefault(this);

        private StyleTemplate() : base() { }
        public StyleTemplate(string name, Style style) : base(name) => Init(style);
        private void Init(Style style)
        {
            Style = style.Copy();
            Style.OnStyleChanged = TemplateChanged;
        }

        public static bool FromXml(XElement config, out StyleTemplate template)
        {
            template = new StyleTemplate();
            return template.FromXml(config);
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(Style.ToXml());
            return config;
        }
        public override bool FromXml(XElement config)
        {
            if (base.FromXml(config) && config.Element(Style.XmlName) is XElement styleConfig && Style.FromXml(styleConfig, new ObjectsMap(), false, out Style style))
            {
                Style = style;
                return true;
            }
            else
                return false;
        }
    }
    public class IntersectionTemplate : Template
    {
        public override TemplateType Type => TemplateType.Intersection;
        public override TemplateManager Manager => TemplateManager.IntersectionManager;

        public override string DeleteCaptionDescription => Localize.TemplateEditor_DeleteCaptionDescription;
        public override string DeleteMessageDescription => Localize.TemplateEditor_DeleteMessageDescription;
        public override string Description => "Intersection";

        public XElement Data { get; private set; }
        public EnterData[] Enters { get; private set; }
        public ObjectsMap Map { get; } = new ObjectsMap();

        public int EntersCount => Enters.Length;

        public Texture2D Texture { get; set; }
        public bool HasScreenshot => Texture != null;

        private IntersectionTemplate() : base() { }

        public IntersectionTemplate(Markup markup) : this($"Intersection #{markup.Id}", markup) { }
        public IntersectionTemplate(string name, Markup markup) : base(name) 
        {
            Data = markup.ToXml();
            Enters = markup.Enters.Select(e => e.Data).ToArray();
        }

        public static bool FromXml(XElement config, out IntersectionTemplate template)
        {
            template = new IntersectionTemplate();
            return template.FromXml(config);
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(Data);

            foreach (var enter in Enters)
                config.Add(enter.ToXml());

            return config;
        }

        public override bool FromXml(XElement config)
        {
            if (base.FromXml(config) && config.Element(Markup.XmlName) is XElement data)
            {
                Data = data;
                Enters = config.Elements(Enter.XmlName).Select(c => EnterData.FromXml(c)).ToArray();
                return true;
            }
            else
                return false;
        }
    }

    public class TemplateAsset
    {
        public Template Template { get; protected set; }

        public ulong AuthorId { get; set; }
        public string Author => TemplateManager.GetAuthor(AuthorId);
        public bool HasAuthor => !string.IsNullOrEmpty(Author);
        public bool IsWorkshop { get; set; }
        public string FileName { get; set; }

        public TemplateAsset(Template template, Package.Asset asset = null)
        {
            Template = template;
            Template.Asset = this;

            if (asset != null)
            {
                AuthorId = ulong.TryParse(asset.package.packageAuthor.Substring("steamid:".Length), out ulong steamId) ? steamId : 0;
                IsWorkshop = asset.isWorkshopAsset;
                FileName = Path.GetFileNameWithoutExtension(asset.package.packagePath);
            }
            else
            {
                AuthorId = PlatformService.active ? PlatformService.user.userID.AsUInt64 : 0;
                IsWorkshop = false;
                FileName = $"IMT_{Template.Description}_{Template.Name.Replace(' ', '_').Replace('.', '_')}_{Template.Id.ToString().Substring(0, 8)}";
            }
        }

        public static bool FromPackage(XElement config, Package.Asset asset, out TemplateAsset templateAsset)
        {
            if (Template.FromXml(config, out Template template))
            {
                templateAsset = new TemplateAsset(template, asset);
                return true;
            }
            else
            {
                templateAsset = default;
                return false;
            }
        }
    }


    [Serializable]
    public class MarkingInfo : PrefabInfo
    {
        public string data;
    }
}
