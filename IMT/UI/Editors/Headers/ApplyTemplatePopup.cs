using ColossalFramework.UI;
using IMT.Manager;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IMT.UI.Editors
{
    public class ApplyTemplateHeaderButton : ObjectDropDown<StyleTemplate, TemplateEntity, TemplatePopup>, IHeaderButton, IReusable
    {
        bool IReusable.InCache { get; set; }
        Transform IReusable.CachedTransform { get => m_CachedTransform; set => m_CachedTransform = value; }

        public Style.StyleType StyleGroup { get; set; }

        protected override IEnumerable<StyleTemplate> Objects => SingletonManager<StyleTemplateManager>.Instance.GetTemplates(StyleGroup);
        protected override Func<StyleTemplate, bool> Selector => null;
        protected override Func<StyleTemplate, StyleTemplate, int> Sorter => (x, y) =>
        {
            var result = Settings.DefaultTemlatesFirst ? SortByDefault(x, y) : 0;

            if (result == 0)
            {
                if (Settings.SortApplyType == 0)
                {
                    if ((result = SortByAuthor(x, y)) == 0)
                        if ((result = SortByType(x, y)) == 0)
                            result = SortByName(x, y);
                }
                else if (Settings.SortApplyType == 1)
                {
                    if ((result = SortByType(x, y)) == 0)
                        result = SortByName(x, y);
                }
                else if (Settings.SortApplyType == 2)
                {
                    if ((result = SortByName(x, y)) == 0)
                        result = SortByType(x, y);
                }
            }

            return result;

            static int SortByDefault(StyleTemplate x, StyleTemplate y) => -x.IsDefault.CompareTo(y.IsDefault);
            static int SortByAuthor(StyleTemplate x, StyleTemplate y) => (x.Asset?.Author ?? string.Empty).CompareTo(y.Asset?.Author ?? string.Empty);
            static int SortByType(StyleTemplate x, StyleTemplate y) => x.Style.Type.CompareTo(y.Style.Type);
            static int SortByName(StyleTemplate x, StyleTemplate y) => x.Name.CompareTo(y.Name);
        };

        public ApplyTemplateHeaderButton()
        {
            BgAtlas = CommonTextures.Atlas;
            BgSprites = new SpriteSet(string.Empty, CommonTextures.HeaderHover, CommonTextures.HeaderHover, CommonTextures.HeaderHover, string.Empty);

            clipChildren = true;
            textScale = 0.8f;
            TextHorizontalAlignment = UIHorizontalAlignment.Left;
            IconMode = SpriteMode.Fill;
        }
        protected override void SetPopupStyle()
        {
            Popup.PopupDefaultStyle(50f);
            Popup.Atlas = CommonTextures.Atlas;
            Popup.BackgroundSprite = CommonTextures.PanelBig;
            Popup.BgColors = UIStyle.PopupBackground;
        }
        protected override void InitPopup()
        {
            Popup.MaximumSize = new Vector2(width, 700f);
            Popup.width = 300f;
            Popup.MaxVisibleItems = 15;
            Popup.EntityHeight = 36f;
            Popup.ItemsPadding = new RectOffset(0, 0, 4, 6);
            base.InitPopup();
        }
        public void SetSize(int buttonSize, int iconSize)
        {
            size = new Vector2(buttonSize, buttonSize);
            minimumSize = size;
            TextPadding = new RectOffset(iconSize + 5, 5, 5, 0);
        }
        public void SetIcon(UITextureAtlas atlas, string sprite)
        {
            IconAtlas = atlas ?? TextureHelper.InGameAtlas;
            IconSprites = sprite;
        }
        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (State == UIButton.ButtonState.Focused)
                State = UIButton.ButtonState.Normal;
        }
        public virtual void DeInit()
        {
            SetIcon(null, string.Empty);
        }
        protected override void OnClick(UIMouseEventParameter p)
        {
            p.Use();
            base.OnClick(p);
        }
    }

    public class TemplatePopup : SearchPopup<StyleTemplate, TemplateEntity>
    {
        protected override string EmptyText => IMT.Localize.HeaderPanel_NoTemplates;
        protected override string GetName(StyleTemplate value) => value.Name;
        protected override void SetEntityStyle(TemplateEntity entity) => entity.EntityDefaultStyle<StyleTemplate, TemplateEntity>();
        protected override bool FilterSearch(StyleTemplate value) => base.FilterSearch(value) || (value.IsAsset && value.Asset.Author.ToLower().Contains(SearchText));
    }
    public class TemplateEntity : StyleTemplateItem, IPopupEntity<StyleTemplate>
    {
        public event Action<int, StyleTemplate> OnSelected;

        public int Index { get; set; }
        public RectOffset Padding { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override bool ShowDelete => false;

        public void SetObject(int index, StyleTemplate template, bool selected)
        {
            Init(null, template, false);
        }

        protected void Select() => OnSelected?.Invoke(Index, EditObject);
        protected override void OnClick(UIMouseEventParameter p)
        {
            base.OnClick(p);
            if (!p.used)
                Select();
        }

        public DropDownStyle EntityStyle
        {
            set
            {
                bgAtlas = value.EntityAtlas;

                bgSprites = value.EntitySprites;
                selBgSprites = value.EntitySelSprites;

                bgColors = value.EntityColors;
                selBgColors = value.EntitySelColors;

                Invalidate();
            }
        }
    }
}
