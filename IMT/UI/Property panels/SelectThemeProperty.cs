using ColossalFramework.UI;
using IMT.Utilities;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace IMT.UI
{
    public class SelectThemeProperty : EditorPropertyPanel, IReusable
    {
        public event Action<ThemeHelper.IThemeData> OnValueChanged;

        private ThemeDropDown DropDown { get; set; }

        public ThemeHelper.IThemeData Theme
        {
            get => DropDown.SelectedObject;
            set => DropDown.SelectedObject = value;
        }
        public string RawName
        {
            get => DropDown.RawName;
            set => DropDown.RawName = value;
        }
        public ThemeHelper.TextureType TextureType
        {
            get => DropDown.TextureType;
            set => DropDown.TextureType = value;
        }

        protected override void FillContent()
        {
            DropDown = Content.AddUIComponent<ThemeDropDown>();
            DropDown.name = nameof(DropDown);
            DropDown.DropDownDefaultStyle();
            DropDown.size = new Vector2(230f, 50f);
            DropDown.ScaleFactor = 20f / DropDown.height;
            DropDown.OnSelectObject += ValueChanged;
        }

        private void ValueChanged(ThemeHelper.IThemeData theme) => OnValueChanged?.Invoke(Theme);

        public override void Init()
        {
            DropDown.Clear();
            DropDown.AddItem(ThemeHelper.DefaultTheme);
            foreach (var theme in ThemeHelper.ThemeDatas)
                DropDown.AddItem(theme);
        }
        public override void DeInit()
        {
            base.DeInit();
            OnValueChanged = null;
            DropDown.Clear();
            TextureType = default;
        }

        public override void SetStyle(ControlStyle style)
        {
            DropDown.DropDownStyle = style.DropDown;
        }
    }

    public class ThemeDropDown : SelectItemDropDown<ThemeHelper.IThemeData, ThemeEntity, ThemePopup>
    {
        protected override Func<ThemeHelper.IThemeData, bool> Selector => null;
        protected override Func<ThemeHelper.IThemeData, ThemeHelper.IThemeData, int> Sorter => null;

        private ThemeHelper.TextureType textureType;
        public ThemeHelper.TextureType TextureType
        {
            get => textureType;
            set
            {
                if (value != textureType)
                {
                    textureType = value;
                    Entity.TextureType = value;
                }
            }
        }
        public string RawName
        {
            get => Entity.RawName;
            set => Entity.RawName = value;
        }

        protected override void SetPopupStyle()
        {
            Popup.PopupDefaultStyle(50f);
            if (DropDownStyle != null)
                Popup.PopupStyle = DropDownStyle;
        }

        protected override void InitPopup()
        {
            Popup.MaximumSize = new Vector2(width, 700f);
            Popup.width = width;
            Popup.MaxVisibleItems = 10;
            Popup.TextureType = TextureType;
            base.InitPopup();
        }
    }

    public class ThemePopup : SearchPopup<ThemeHelper.IThemeData, ThemeEntity>
    {
        protected override string EmptyText => IMT.Localize.AssetPopup_NothingFound;
        private static string SearchCache { get; set; } = string.Empty;
        public ThemeHelper.TextureType TextureType { get; set; }

        public override void Init(IEnumerable<ThemeHelper.IThemeData> values, Func<ThemeHelper.IThemeData, bool> selector, Func<ThemeHelper.IThemeData, ThemeHelper.IThemeData, int> sorter)
        {
            Search.text = SearchCache;
            base.Init(values, selector, sorter);
        }
        public override void DeInit()
        {
            TextureType = default;
            SearchCache = Search.text;
            base.DeInit();
        }
        protected override void SetEntityValue(ThemeEntity entity, int index, ThemeHelper.IThemeData value, bool selected)
        {
            entity.TextureType = TextureType;
            base.SetEntityValue(entity, index, value, selected);
        }

        protected override string GetName(ThemeHelper.IThemeData value) => value.Name;
        protected override void SetEntityStyle(ThemeEntity entity)
        {
            entity.EntityDefaultStyle<ThemeHelper.IThemeData, ThemeEntity>();
            if (PopupStyle != null)
                entity.EntityStyle = PopupStyle;
        }
    }

    public class ThemeEntity : PopupEntity<ThemeHelper.IThemeData>
    {
        private ThemeHelper.TextureType textureType;
        private string rawName;

        public string RawName
        {
            get => rawName;
            set
            {
                rawName = value;
                Set();
            }
        }
        public ThemeHelper.TextureType TextureType
        {
            get => textureType;
            set
            {
                if (value != textureType)
                {
                    textureType = value;
                    Set();
                }
            }
        }

        private CustomUITextureSprite Screenshot { get; set; }

        public ThemeEntity()
        {
            Screenshot = AddUIComponent<CustomUITextureSprite>();
            Screenshot.material = RenderHelper.ThemeTexture;
            Screenshot.size = new Vector2(90f, 90f);

            WordWrap = true;
            textScale = 0.7f;
            TextVerticalAlignment = UIVerticalAlignment.Middle;
            TextHorizontalAlignment = UIHorizontalAlignment.Left;

            Set();
        }
        public override void DeInit()
        {
            base.DeInit();

            Screenshot.texture = null;
            textureType = default;
            rawName = string.Empty;
        }

        public override void SetObject(int index, ThemeHelper.IThemeData theme, bool selected)
        {
            base.SetObject(index, theme, selected);
            rawName = theme?.Id ?? string.Empty;
            Set();
        }
        private void Set()
        {
            if (EditObject is ThemeHelper.IThemeData theme)
            {
                Screenshot.texture = theme.GetTexture(TextureType).texture;
                Screenshot.isVisible = true;
                text = theme.Name;
            }
            else
            {
                Screenshot.texture = null;
                Screenshot.isVisible = false;

                if (string.IsNullOrEmpty(RawName))
                    text = IMT.Localize.StyleOption_AssetNotSet;
                else
                    text = string.Format(IMT.Localize.StyleOption_ThemeMissed, RawName);
            }

            SetPosition();
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            SetPosition();
        }
        private void SetPosition()
        {
            if (Screenshot != null)
            {
                Screenshot.size = new Vector2(height - 10f, height - 10f);
                Screenshot.relativePosition = new Vector2(5f, 5f);

                var left = Screenshot.isVisible ? Mathf.CeilToInt(Screenshot.relativePosition.x + Screenshot.width) + 5 : 8;
                var right = Math.Max(Padding.right, 8);
                TextPadding = new RectOffset(left, right, 5, 5);
            }
        }
    }
}
