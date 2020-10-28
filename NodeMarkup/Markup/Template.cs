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
    public class StyleTemplate : IDeletable, IToXml
    {
        public static string XmlName { get; } = "T";

        public virtual bool IsAsset => false;

        public string DeleteCaptionDescription => Localize.TemplateEditor_DeleteCaptionDescription;
        public string DeleteMessageDescription => Localize.TemplateEditor_DeleteMessageDescription;

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
        public Guid Id { get; private set; }
        public Style Style { get; private set; }
        public bool IsDefault => TemplateManager.IsDefault(this);
        public bool HasName => !string.IsNullOrEmpty(Name);

        public Action OnTemplateChanged { private get; set; }

        public string XmlSection => XmlName;

        public StyleTemplate(string name, Style style) : this(Guid.NewGuid(), name, style) { }
        private StyleTemplate(Guid id, string name, Style style)
        {
            Id = id;
            Name = name;
            Style = style.Copy();
            Style.OnStyleChanged = TemplateChanged;
        }
        private void TemplateChanged() => OnTemplateChanged?.Invoke();
        public Dependences GetDependences() => new Dependences();

        public override string ToString() => HasName ? Name : Localize.TemplateEditor_UnnamedTemplate;

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

        public XElement ToXml()
        {
            var config = new XElement(XmlName);
            config.Add(new XAttribute(nameof(Id), Id));
            config.Add(new XAttribute("N", Name));
            config.Add(Style.ToXml());
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
