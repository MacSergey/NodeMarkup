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
        bool IReusable.InCache { get; set; }
        public override bool SupportEven => true;

        private ThemeDropDown DropDown { get; }

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

        public SelectThemeProperty()
        {
            DropDown = Content.AddUIComponent<ThemeDropDown>();
            DropDown.DefaultStyle();
            DropDown.OnValueChanged += ValueChanged;
        }

        private void ValueChanged(ThemeHelper.IThemeData theme) => OnValueChanged?.Invoke(Theme);

        public override void Init() => Init(null);
        public new void Init(float? height)
        {
            base.Init(height);

            DropDown.Clear();
            DropDown.AddItem(ThemeHelper.DefaultTheme);
            foreach (var theme in ThemeHelper.ThemeDatas)
                DropDown.AddItem(theme);
        }
        public override void DeInit()
        {
            base.DeInit();
            Theme = null;
            TextureType = default;
            OnValueChanged = null;
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            if (DropDown != null)
            {
                DropDown.size = new Vector2(230f, height - 10f);
                DropDown.scaleFactor = 20f / DropDown.height;
            }
        }
    }

    public class ThemeDropDown : AdvancedDropDown<ThemeHelper.IThemeData, ThemePopup, ThemeEntity>
    {
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

        protected override void SetPopupStyle() => Popup.DefaultStyle(50f);
        protected override void InitPopup()
        {           
            Popup.MaximumSize = new Vector2(width, 700f);
            Popup.width = width;
            Popup.MaxVisibleItems = 10;
            Popup.TextureType = TextureType;
            Popup.Init(Objects, null, null);
        }
    }

    public class ThemePopup : SearchPopup<ThemeHelper.IThemeData, ThemeEntity>
    {
        protected override string NotFoundText => IMT.Localize.AssetPopup_NothingFound;
        private static string SearchText { get; set; } = string.Empty;
        public ThemeHelper.TextureType TextureType { get; set; }

        public override void Init(IEnumerable<ThemeHelper.IThemeData> values, Func<ThemeHelper.IThemeData, bool> selector, Func<ThemeHelper.IThemeData, ThemeHelper.IThemeData, int> sorter)
        {
            Search.text = SearchText;
            base.Init(values, selector, sorter);
        }
        public override void DeInit()
        {
            TextureType = default;
            SearchText = Search.text;
            base.DeInit();
        }
        protected override void SetEntityValue(ThemeEntity entity, int index, ThemeHelper.IThemeData value, bool selected)
        {
            entity.TextureType = TextureType;
            base.SetEntityValue(entity, index, value, selected);
        }

        protected override string GetName(ThemeHelper.IThemeData value) => value.Name;
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
        private CustomUILabel Title { get; set; }

        public ThemeEntity()
        {
            Screenshot = AddUIComponent<CustomUITextureSprite>();
            Screenshot.material = RenderHelper.ThemeTexture;
            Screenshot.size = new Vector2(90f, 90f);

            Title = AddUIComponent<CustomUILabel>();
            Title.autoSize = false;
            Title.wordWrap = true;
            Title.textScale = 0.7f;
            Title.verticalAlignment = UIVerticalAlignment.Middle;

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
            if (Object is ThemeHelper.IThemeData theme)
            {
                Screenshot.texture = theme.GetTexture(TextureType).texture;
                Screenshot.isVisible = true;
                Title.text = theme.Name;
            }
            else
            {
                Screenshot.texture = null;
                Screenshot.isVisible = false;

                if (string.IsNullOrEmpty(RawName))
                    Title.text = IMT.Localize.StyleOption_AssetNotSet;
                else
                    Title.text = string.Format(IMT.Localize.StyleOption_ThemeMissed, RawName);
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
            if (Screenshot != null && Title != null)
            {
                Screenshot.size = new Vector2(height - 10f, height - 10f);
                Screenshot.relativePosition = new Vector2(5f, 5f);
                Title.size = size;

                var left = Screenshot.isVisible ? Mathf.CeilToInt(Screenshot.relativePosition.x + Screenshot.width) + 5 : 8;
                var right = Math.Max(Padding.right, 8);
                Title.padding = new RectOffset(left, right, 5, 5);
            }
        }
    }
}
