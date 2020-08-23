using ColossalFramework.UI;
using NodeMarkup.Manager;
using NodeMarkup.Utils;
using System;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public abstract class EditableItemBase : UIButton
    {
        public static UITextureAtlas ItemAtlas { get; } = GetStylesIcons();
        private static UITextureAtlas GetStylesIcons()
        {
            var spriteNames = new string[] { "Item"};

            var atlas = TextureUtil.GetAtlas(nameof(ItemAtlas));
            if (atlas == UIView.GetAView().defaultAtlas)
                atlas = TextureUtil.CreateTextureAtlas("ListItem.png", nameof(ItemAtlas), 21, 26, spriteNames, new RectOffset(1, 1, 1, 1));

            return atlas;
        }

        public virtual Color32 NormalColor => new Color32(29, 58, 77, 255);
        public virtual Color32 HoveredColor => new Color32(44, 87, 112, 255);
        public virtual Color32 PressedColor => new Color32(51, 100, 132, 255);
        public virtual Color32 FocusColor => new Color32(171, 185, 196, 255);
        public virtual Color32 TextColor => Color.white;

        private bool _isSelect;
        public bool IsSelect
        {
            get => _isSelect;
            set
            {
                if (_isSelect != value)
                {
                    _isSelect = value;
                    OnSelectChanged();
                }
            }
        }

        protected UILabel Label { get; set; }
        public string Text
        {
            get => Label.text;
            set => Label.text = value;
        }

        public EditableItemBase()
        {
            AddLable();

            atlas = ItemAtlas;
            normalBgSprite = "Item";
            height = 25;

            OnSelectChanged();
        }

        private void AddLable()
        {
            Label = AddUIComponent<UILabel>();
            Label.textAlignment = UIHorizontalAlignment.Left;
            Label.verticalAlignment = UIVerticalAlignment.Middle;
            Label.autoSize = false;
            Label.autoHeight = false;
            Label.textScale = 0.55f;
            Label.padding = new RectOffset(0, 0, 3, 0);
            Label.autoHeight = true;
            Label.wordWrap = true;
        }

        protected virtual void OnSelectChanged()
        {
            color = NormalColor;
            hoveredColor = HoveredColor;
            pressedColor = PressedColor;
            focusedColor = FocusColor;

            Label.textColor = TextColor;
        }
    }
    public abstract class EditableItem<EditableObject, IconType> : EditableItemBase
        where IconType : UIComponent
        where EditableObject : class
    {
        public event Action<EditableItem<EditableObject, IconType>> OnDelete;

        EditableObject _object;
        private bool Inited { get; set; } = false;
        public abstract string DeleteCaptionDescription { get; }
        public abstract string DeleteMessageDescription { get; }
        public EditableObject Object
        {
            get => _object;
            set
            {
                _object = value;
                Refresh();
                OnObjectSet();
            }
        }
        protected IconType Icon { get; set; }
        private UIButton DeleteButton { get; set; }

        public bool ShowIcon { get; set; }
        public bool ShowDelete { get; set; }

        public abstract void Init();
        public void Init(bool showIcon, bool showDelete)
        {
            if (Inited)
                return;

            ShowIcon = showIcon;
            ShowDelete = showDelete;

            if (ShowIcon)
                AddIcon();
            if (ShowDelete)
                AddDeleteButton();

            OnSizeChanged();

            Inited = true;
        }

        private void AddIcon() => Icon = AddUIComponent<IconType>();

        private void AddDeleteButton()
        {
            DeleteButton = AddUIComponent<UIButton>();
            DeleteButton.atlas = TextureUtil.InGameAtlas;
            DeleteButton.normalBgSprite = "buttonclose";
            DeleteButton.hoveredBgSprite = "buttonclosehover";
            DeleteButton.pressedBgSprite = "buttonclosepressed";
            DeleteButton.size = new Vector2(20, 20);
            DeleteButton.isEnabled = ShowDelete;
            DeleteButton.eventClick += DeleteClick;
        }
        protected override void OnSelectChanged()
        {
            if (IsSelect)
            {
                color = FocusColor;
                hoveredColor = FocusColor;
                pressedColor = FocusColor;

                Label.textColor = TextColor;
            }
            else
                base.OnSelectChanged();
        }
        private void DeleteClick(UIComponent component, UIMouseEventParameter eventParam) => OnDelete?.Invoke(this);
        protected virtual void OnObjectSet() { }
        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            var labelWidth = size.x;
            if (ShowIcon)
            {
                Icon.size = new Vector2(size.y - 6, size.y - 6);
                Icon.relativePosition = new Vector2(3, 3);
                labelWidth -= 25;
            }

            if (ShowDelete)
            {
                DeleteButton.size = new Vector2(size.y - 6, size.y - 6);
                DeleteButton.relativePosition = new Vector2(size.x - (size.y - 3), 3);
                labelWidth -= 19;
            }

            Label.size = new Vector2(ShowIcon ? labelWidth : labelWidth - 3, size.y);
            Label.relativePosition = new Vector3(ShowIcon ? size.y : 3, (size.y - Label.height) / 2);
        }

        public virtual void Refresh() => Text = Object.ToString();
    }

    public class ColorIcon : UIButton
    {
        private static float Border => 1f;
        protected UIButton InnerCircule { get; set; }
        public Color32 InnerColor { set => InnerCircule.color = value; }
        public Color32 BorderColor { set => color = value; }
        public ColorIcon()
        {
            atlas = TextureUtil.InGameAtlas;
            normalBgSprite = "PieChartWhiteBg";
            disabledBgSprite = "PieChartWhiteBg";
            isInteractive = false;
            color = Color.white;

            InnerCircule = AddUIComponent<UIButton>();
            InnerCircule.atlas = TextureUtil.InGameAtlas;
            InnerCircule.normalBgSprite = "PieChartWhiteBg";
            InnerCircule.normalFgSprite = "PieChartWhiteFg";
            InnerCircule.disabledBgSprite = "PieChartWhiteBg";
            InnerCircule.disabledFgSprite = "PieChartWhiteFg";
            InnerCircule.isInteractive = false;
            InnerCircule.relativePosition = new Vector3(Border, Border);
            InnerCircule.color = Color.black;
        }
        protected override void OnSizeChanged()
        {
            if (InnerCircule != null)
            {
                InnerCircule.height = height - (Border * 2);
                InnerCircule.width = width - (Border * 2);
            }
        }
    }
    public class StyleIcon : ColorIcon
    {
        protected static Color32 GetStyleColor(Color32 color)
        {
            var ratio = 255 / (float)Math.Max(Math.Max(color.r, color.g), color.b);
            var styleColor = new Color32((byte)(color.r * ratio), (byte)(color.g * ratio), (byte)(color.b * ratio), 255);
            return styleColor == Color.black ? (Color32)Color.white : styleColor;
        }
        protected UIButton Thumbnail { get; set; }

        public Color32 StyleColor { set => Thumbnail.color = GetStyleColor(value); }
        public Style.StyleType Type
        {
            set
            {
                if (!Editor.SpriteNames.TryGetValue(value, out string sprite))
                    sprite = string.Empty;

                Thumbnail.normalBgSprite = Thumbnail.normalFgSprite = sprite;
            }
        }

        public StyleIcon()
        {
            Thumbnail = AddUIComponent<UIButton>();
            Thumbnail.atlas = Editor.StylesAtlas;
            Thumbnail.relativePosition = new Vector3(0, 0);
            Thumbnail.isInteractive = false;
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            if (Thumbnail != null)
                Thumbnail.size = size;
        }
    }
    public class LineIcon : StyleIcon
    {
        protected UILabel CountLabel { get; }
        public int Count
        {
            set
            {
                CountLabel.isVisible = value > 1;
                Thumbnail.isVisible = value == 1;
                CountLabel.text = value.ToString();
            }
        }

        public LineIcon()
        {
            CountLabel = AddUIComponent<UILabel>();
            CountLabel.textColor = Color.white;
            CountLabel.textScale = 0.7f;
            CountLabel.relativePosition = new Vector3(0, 0);
            CountLabel.autoSize = false;
            CountLabel.textAlignment = UIHorizontalAlignment.Center;
            CountLabel.verticalAlignment = UIVerticalAlignment.Middle;
            CountLabel.padding = new RectOffset(0, 0, 5, 0);
        }
        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            if (CountLabel != null)
                CountLabel.size = size;
        }
    }
}
