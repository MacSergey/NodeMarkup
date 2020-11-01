using ColossalFramework.UI;
using ICities;
using NodeMarkup.Manager;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public abstract class EditableItemBase : UIButton
    {
        private static string Item { get; } = nameof(Item);
        public static UITextureAtlas ItemAtlas { get; } = TextureUtil.CreateTextureAtlas("ListItem.png", nameof(ItemAtlas), 21, 26, new string[] { Item }, new RectOffset(1, 1, 1, 1));

        public virtual Color32 NormalColor => new Color32(29, 58, 77, 255);
        public virtual Color32 HoveredColor => new Color32(44, 87, 112, 255);
        public virtual Color32 PressedColor => new Color32(51, 100, 132, 255);
        public virtual Color32 FocusColor => new Color32(171, 185, 196, 255);
        public virtual Color32 TextColor => Color.white;

        protected bool _isSelect;
        public bool IsSelect
        {
            get => _isSelect;
            set
            {
                if (_isSelect != value)
                {
                    _isSelect = value;
                    SetColors();
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
            normalBgSprite = Item;
            height = 25;
        }

        private void AddLable()
        {
            Label = AddUIComponent<UILabel>();
            Label.textAlignment = UIHorizontalAlignment.Left;
            Label.verticalAlignment = UIVerticalAlignment.Middle;
            Label.autoSize = false;
            Label.textScale = 0.55f;
            Label.padding = new RectOffset(0, 0, 3, 0);
            Label.autoHeight = true;
            Label.wordWrap = true;
        }
        public virtual void DeInit()
        {
            _isSelect = false;
        }

        protected virtual void SetColors()
        {
            color = NormalColor;
            hoveredColor = HoveredColor;
            pressedColor = PressedColor;
            focusedColor = FocusColor;

            Label.textColor = TextColor;
        }
    }
    public abstract class EditableItem<EditableObject, IconType> : EditableItemBase, IReusable
        where IconType : UIComponent
        where EditableObject : class, IDeletable
    {
        public event Action<EditableItem<EditableObject, IconType>> OnDelete;

        EditableObject _object;
        public EditableObject Object
        {
            get => _object;
            private set
            {
                _object = value;
                if (_object != null)
                    Refresh();
            }
        }
        protected bool Inited => Object != null;

        protected IconType Icon { get; set; }
        private UIButton DeleteButton { get; set; }

        public virtual bool ShowIcon => true;
        public virtual bool ShowDelete => true;

        public EditableItem()
        {
            Label.eventSizeChanged += LabelSizeChanged;
            AddIcon();
            AddDeleteButton();
        }

        public virtual void Init(EditableObject editableObject)
        {
            Object = editableObject;

            Icon.isVisible = ShowIcon;
            DeleteButton.isVisible = ShowDelete;

            Refresh();
            OnSizeChanged();
        }
        public override void DeInit()
        {
            base.DeInit();

            Text = string.Empty;
            Object = null;
        }

        private void AddIcon() => Icon = AddUIComponent<IconType>();

        private void AddDeleteButton()
        {
            DeleteButton = AddUIComponent<UIButton>();
            DeleteButton.atlas = TextureUtil.AdditionalAtlas;
            DeleteButton.normalBgSprite = TextureUtil.DeleteNormal;
            DeleteButton.hoveredBgSprite = TextureUtil.DeleteHover;
            DeleteButton.pressedBgSprite = TextureUtil.DeletePressed;
            DeleteButton.size = new Vector2(20, 20);
            DeleteButton.eventClick += DeleteClick;
        }
        protected override void SetColors()
        {
            if (IsSelect)
            {
                color = FocusColor;
                hoveredColor = FocusColor;
                pressedColor = FocusColor;

                Label.textColor = TextColor;
            }
            else
                base.SetColors();
        }
        private void DeleteClick(UIComponent component, UIMouseEventParameter eventParam) => OnDelete?.Invoke(this);
        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            if (!Inited)
                return;

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
        }
        private void LabelSizeChanged(UIComponent component, Vector2 value) => Label.relativePosition = new Vector3(ShowIcon ? size.y : 3, (size.y - Label.height) / 2);

        public virtual void Refresh()
        {
            Text = Object.ToString();
            SetColors();
        }
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
