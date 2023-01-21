﻿using ColossalFramework.Importers;
using ColossalFramework.IO;
using ColossalFramework.Packaging;
using ColossalFramework.PlatformServices;
using IMT.Utilities;
using ModsCommon;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UnityEngine;

namespace IMT.Manager
{
    public abstract class Template : IDeletable, ISupport, IToXml
    {
        public static string XmlName { get; } = "T";

        public string XmlSection => XmlName;
        public abstract TemplateType Type { get; }
        public abstract TemplateManager Manager { get; }
        public abstract Marking.SupportType Support { get; }

        public TemplateAsset Asset { get; set; }
        public bool IsAsset => Asset != null;

        public Guid Id { get; private set; }
        public abstract string Description { get; }

        public string Name { get; set; }
        public bool HasName => !string.IsNullOrEmpty(Name);

        public virtual Texture2D Preview { get; set; }
        public virtual bool HasPreview => Preview != null;
        public virtual Texture2D SteamPreview { get; set; }
        public virtual bool HasSteamPreview => Preview != null;
        public virtual bool SeparatePreview => true;
        public virtual bool NeedLoadPreview => true;

        public abstract string DeleteCaptionDescription { get; }
        public abstract string DeleteMessageDescription { get; }

        protected Template() { }
        protected Template(string name) : this(Guid.NewGuid(), name) { }
        protected Template(Guid id, string name)
        {
            Id = id;
            Name = name;
        }
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
            config.AddAttr("T", (int)Type);
            config.AddAttr(nameof(Id), Id);
            config.AddAttr("N", Name);
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
            var name = HasName ? Name : Localize.TemplateEditor_UnnamedTemplate;
            return IsAsset && Asset.HasAuthor ? string.Format(Localize.TemplateEditor_TemplateByAuthor, name, Asset.Author) : name;
        }
    }
    public enum TemplateType
    {
        Style = 1,
        Intersection = 2
    }
    public abstract class Template<TemplateType> : Template
        where TemplateType : Template<TemplateType>
    {
        protected Template() : base() { }
        protected Template(string name) : base(name) { }
    }
    public class StyleTemplate : Template<StyleTemplate>
    {
        public override TemplateType Type => TemplateType.Style;
        public override Marking.SupportType Support => Marking.SupportType.StyleTemplates;
        public override TemplateManager Manager => SingletonManager<StyleTemplateManager>.Instance;

        public override bool HasPreview => true;
        public override Texture2D Preview => GetTexture("StylesPreviewBackground");
        public override bool HasSteamPreview => true;
        public override Texture2D SteamPreview => GetTexture("StylesSteamBackground");
        public override bool NeedLoadPreview => false;

        private Texture2D GetTexture(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var background = assembly.LoadTextureFromAssembly(name);
            var logo = assembly.LoadTextureFromAssembly("StylesPreview" + Style.Type.ToString());

            if (background == null)
                return null;

            else if (logo != null)
            {
                var widthShift = (background.width - logo.width) / 2;
                var heightShift = (background.height - logo.height) / 2;

                var styleColor = (Color)Style.Color.Value.GetStyleIconColor();
                var isColor = Style is IColorStyle;

                for (var i = 0; i < logo.width; i += 1)
                {
                    for (var j = 0; j < logo.height; j += 1)
                    {
                        var logoColor = logo.GetPixel(i, j);
                        var backColor = background.GetPixel(i + widthShift, j + heightShift);
                        if (logoColor.a != 0)
                        {
                            if (isColor)
                            {
                                var color = new Color(Blend(backColor.r, styleColor.r, logoColor.a), Blend(backColor.g, styleColor.g, logoColor.a), Blend(backColor.b, styleColor.b, logoColor.a), 1);
                                background.SetPixel(i + widthShift, j + heightShift, color);
                            }
                            else
                            {
                                var color = new Color(Blend(backColor.r, logoColor.r, logoColor.a), Blend(backColor.g, logoColor.g, logoColor.a), Blend(backColor.b, logoColor.b, logoColor.a), 1);
                                background.SetPixel(i + widthShift, j + heightShift, color);
                            }
                        }
                    }
                }
            }

            static float Blend(float background, float color, float opacity) => background * (1 - opacity) + color * opacity;

            return background;
        }

        public override string DeleteCaptionDescription => Localize.TemplateEditor_DeleteCaptionDescription;
        public override string DeleteMessageDescription => Localize.TemplateEditor_DeleteMessageDescription;
        public override string Description => Style.Type.ToString();

        public Style Style { get; set; }
        public bool IsDefault => SingletonManager<StyleTemplateManager>.Instance.IsDefault(this);

        private StyleTemplate() : base() { }
        public StyleTemplate(string name, Style style) : base(name) => Style = style.Copy();

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
            if (base.FromXml(config) && config.Element(Style.XmlName) is XElement styleConfig && Style.FromXml(styleConfig, new ObjectsMap(), false, false, out Style style))
            {
                Style = style;
                return true;
            }
            else
                return false;
        }
    }
    public class IntersectionTemplate : Template<IntersectionTemplate>
    {
        public override TemplateType Type => TemplateType.Intersection;
        public override Marking.SupportType Support => Marking.SupportType.IntersectionTemplates;
        public override TemplateManager Manager => SingletonManager<IntersectionTemplateManager>.Instance;

        public override string DeleteCaptionDescription => Localize.PresetEditor_DeleteCaptionDescription;
        public override string DeleteMessageDescription => Localize.PresetEditor_DeleteMessageDescription;
        public override string Description => "Intersection";

        public XElement Data { get; private set; }
        public EntranceData[] Enters { get; private set; }
        public ObjectsMap Map { get; } = new ObjectsMap();

        public int Roads => Enters.Length;
        public int Lines { get; private set; }
        public int Crosswalks { get; private set; }
        public int Fillers { get; private set; }

        private Texture2D Screenshot { get; set; }
        public override Texture2D Preview
        {
            get => Screenshot;
            set => Screenshot = value;
        }
        public override Texture2D SteamPreview
        {
            get => Screenshot;
            set => Screenshot = value;
        }
        public override bool SeparatePreview => false;

        private IntersectionTemplate() : base() { }

        public IntersectionTemplate(Marking marking) : this($"Intersection #{marking.Id}", marking) { }
        public IntersectionTemplate(string name, Marking marking) : base(name)
        {
            Data = marking.ToXml();
            Enters = marking.Enters.Select(e => e.Data).ToArray();
            Lines = marking.LinesCount;
            Crosswalks = marking.CrosswalksCount;
            Fillers = marking.FillersCount;
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

            config.AddAttr("LC", Lines);
            config.AddAttr("CC", Crosswalks);
            config.AddAttr("FC", Fillers);

            return config;
        }

        public override bool FromXml(XElement config)
        {
            if (base.FromXml(config) && config.Elements().FirstOrDefault() is XElement data)
            {
                Data = data;
                Enters = config.Elements(Entrance.XmlName).Select(c => EntranceData.FromXml(c)).ToArray();

                Lines = config.GetAttrValue<int>("LC");
                Crosswalks = config.GetAttrValue<int>("CC");
                Fillers = config.GetAttrValue<int>("FC");

                if (Loader.LoadScreenshot(this, out var image))
                    Preview = image.CreateTexture();

                return true;
            }
            else
                return false;
        }
    }

    public class TemplateAsset
    {
        private static string LocalFolder { get; } = DataLocation.assetsPath;
        public Template Template { get; private set; }

        public ulong AuthorId { get; private set; } = TemplateManager.UserId;
        public bool AuthorIsUser => AuthorId != 0 && AuthorId == TemplateManager.UserId;
        public string Author => TemplateManager.GetAuthor(AuthorId);
        public bool HasAuthor => !string.IsNullOrEmpty(Author);
        public PublishedFileId WorkshopId { get; private set; } = PublishedFileId.invalid;
        public bool IsWorkshop => WorkshopId != PublishedFileId.invalid;
        public bool IsLocalFolder { get; private set; }
        public bool CanEdit => !IsWorkshop || AuthorIsUser;

        private string DefineFileName { get; set; }
        public string FileName
        {
            get
            {
                if (DefineFileName is string fileName)
                    return fileName;
                else
                {
                    var name = Replacer.Replace(Template.Name, "_").Trim('_');
                    return $"IMT_{Template.Description}_{name}_{Template.Id.Unique()}";
                }
            }
        }
        public string Flags => $"{(HasAuthor ? "A" : string.Empty)}{(AuthorIsUser ? "U" : string.Empty)}{(IsWorkshop ? "W" : string.Empty)}{(IsLocalFolder ? "L" : string.Empty)}";

        public Image Preview => Template.HasPreview ? GetPreview(Template.Preview) : null;
        public Image SteamPreview => Template.HasSteamPreview ? GetPreview(Template.SteamPreview) : null;
        public bool SeparatePreview => Template.SeparatePreview;
        public bool NeedLoadPreview => Template.NeedLoadPreview;

        private Image GetPreview(Texture2D texture)
        {
            try
            {
                return new Image(texture.width, texture.height, TextureFormat.RGB24, texture.GetPixels32());
            }
            catch (Exception error)
            {
                SingletonMod<Mod>.Logger.Error("Could not get template screenshot", error);
                return null;
            }
        }

        public string MetaPreview => $"{Template.Name}_Preview";
        public string MetaSteamPreview => $"{Template.Name}_SteamPreview";

        public override string ToString() => $"[{Template.Type}] \"{Template.Name}\" - {Template.Id}";
        private static Regex Replacer { get; } = new Regex(@$"[{new string(GetInvalidChars().ToArray())}]+");
        private static IEnumerable<char> GetInvalidChars()
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                yield return c;

            yield return '.';
            yield return ' ';
        }

        public TemplateAsset(Template template, Package.Asset asset = null)
        {
            Template = template;
            Template.Asset = this;

            if (asset == null)
                return;

            AuthorId = ulong.TryParse(asset.package.packageAuthor.Substring("steamid:".Length), out ulong steamId) ? steamId : 0;
            WorkshopId = asset.package.GetPublishedFileID();
            IsLocalFolder = asset.package.packagePath.StartsWith(LocalFolder);
            if (IsLocalFolder)
                DefineFileName = Path.GetFileNameWithoutExtension(asset.package.packagePath);

            if (NeedLoadPreview && asset.package.Find(MetaPreview) is Package.Asset assetPreview && assetPreview.Instantiate<Texture>() is Texture2D preview)
                Template.Preview = preview;
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


}
namespace IMT.Manager
{
    [Serializable]
    public class MarkingInfo : PrefabInfo
    {
        public string data;
    }
}
