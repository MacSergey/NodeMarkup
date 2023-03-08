using ColossalFramework.UI;
using IMT.Manager;
using IMT.Utilities;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using UnityEngine;

namespace IMT.UI.Editors
{
    public abstract class EditItemBase : CustomUIButton
    {
        public virtual Color32 BackgroundColor => NormalColor;
        public virtual Color32 NormalColor => new Color32(29, 58, 77, 255);
        public virtual Color32 HoveredColor => new Color32(44, 87, 112, 255);
        public virtual Color32 PressedColor => new Color32(51, 100, 132, 255);
        public virtual Color32 FocusColor => new Color32(171, 185, 196, 255);
        public virtual Color32 TextColor => Color.white;
        protected virtual float TextScale => 0.55f;
        protected virtual float DefaultHeight => 25f;

        public bool IsSelect
        {
            get => state == ButtonState.Focused;
            set
            {
                if (IsSelect != value)
                {
                    state = value ? ButtonState.Focused : ButtonState.Normal;
                    SetColors();
                }
            }
        }

        public EditItemBase()
        {
            m_Size = new Vector2(100f, DefaultHeight);
            clipChildren = true;
            atlas = CommonTextures.Atlas;
            normalBgSprite = CommonTextures.PanelSmall;
            normalFgSprite = CommonTextures.PanelSmall;

            textHorizontalAlignment = UIHorizontalAlignment.Left;
            textVerticalAlignment = UIVerticalAlignment.Middle;
            textScale = TextScale;
            textPadding.top = 5;
            wordWrap = true;
        }

        public virtual void DeInit()
        {
            IsSelect = false;
            isVisible = true;
        }

        protected virtual void SetColors()
        {
            normalBgColor = hoveredBgColor = pressedBgColor = focusedBgColor = BackgroundColor;

            normalFgColor = NormalColor;
            hoveredFgColor = HoveredColor;
            pressedFgColor = PressedColor;
            focusedFgColor = FocusColor;

            disabledBgColor = disabledFgColor = NormalColor;

            textColor = TextColor;
        }
    }
    public abstract class EditItem<ObjectType> : EditItemBase, IReusable
        where ObjectType : class, IDeletable
    {
        public event Action<EditItem<ObjectType>> OnDelete;

        bool IReusable.InCache { get; set; }

        protected Editor Editor { get; private set; }
        private ObjectType _object;
        public ObjectType Object
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
        protected CustomUIButton DeleteButton { get; set; }
        public virtual bool ShowDelete => true;

        public EditItem() : base()
        {
            AddDeleteButton();
        }

        public virtual void Init(Editor editor, ObjectType editObject, bool inGroup)
        {
            Editor = editor;
            Object = editObject;

            Refresh();
        }
        public override void DeInit()
        {
            base.DeInit();

            text = string.Empty;
            Editor = null;
            Object = null;
            OnDelete = null;
        }

        private void AddDeleteButton()
        {
            DeleteButton = AddUIComponent<CustomUIButton>();
            DeleteButton.atlas = CommonTextures.Atlas;
            DeleteButton.normalFgSprite = CommonTextures.CloseButtonNormal;
            DeleteButton.hoveredFgSprite = CommonTextures.CloseButtonHovered;
            DeleteButton.pressedFgSprite = CommonTextures.CloseButtonPressed;
            DeleteButton.foregroundSpriteMode = UIForegroundSpriteMode.Stretch;
            DeleteButton.size = new Vector2(20, 20);
            DeleteButton.eventClick += DeleteClick;
        }
        private void DeleteClick(UIComponent component, UIMouseEventParameter eventParam) => OnDelete?.Invoke(this);

        public virtual void Refresh()
        {
            DeleteButton.isVisible = ShowDelete && width >= 120f;
            text = Object.ToString();
            SetColors();
        }
    }
    public abstract class EditItem<ObjectType, IconType> : EditItem<ObjectType>
        where ObjectType : class, IDeletable
        where IconType : UIComponent
    {
        public virtual bool ShowIcon => true;
        protected IconType Icon { get; set; }

        public EditItem() : base()
        {
            Icon = AddUIComponent<IconType>();
        }
        public override void Init(Editor editor, ObjectType editObject, bool inGroup)
        {
            Icon.isVisible = ShowIcon;
            if(inGroup)
            {
                spritePadding = new RectOffset(3, 3, 2, 2);
                m_Size.y = DefaultHeight + spritePadding.vertical;
            }
            else
            {
                spritePadding = new RectOffset(0, 0, 0, 0);
                m_Size.y = DefaultHeight;
            }
            base.Init(editor, editObject, inGroup);
        }
        public override void Refresh()
        {
            base.Refresh();

            if (ShowIcon)
            {
                var iconSize = size.y - 6 - spritePadding.vertical;
                Icon.size = new Vector2(iconSize, iconSize);
                var fixOffset = 3 + spritePadding.left;
                var offset = (width - iconSize) * 0.5f;
                if (offset <= 1.5f * fixOffset)
                {
                    Icon.relativePosition = new Vector2(offset, (height - iconSize) * 0.5f);
                    textPadding.left = 50;
                }
                else
                {
                    Icon.relativePosition = new Vector2(fixOffset, (height - iconSize) * 0.5f);
                    textPadding.left = 25 + spritePadding.left;
                }
            }
            else
                textPadding.left = 5 + spritePadding.left;

            if (ShowDelete && width >= 120f)
            {
                var buttonSize = size.y - 10 - spritePadding.vertical;
                DeleteButton.isVisible = true;
                DeleteButton.size = new Vector2(buttonSize, buttonSize);
                DeleteButton.relativePosition = new Vector2(size.x - buttonSize - 5 - spritePadding.right, (height - buttonSize) * 0.5f);
                textPadding.right = 19 + spritePadding.right;
            }
            else
            {
                DeleteButton.isVisible = false;
                textPadding.right = 5 + spritePadding.right;
            }
        }
        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            Refresh();
        }
    }

    public class ColorIcon : CustomUISprite
    {
        private static float Border => 1f;
        protected CustomUISprite InnerCircule { get; set; }
        public Color32 InnerColor { set => InnerCircule.color = value; }
        public Color32 BorderColor { set => color = value; }
        public ColorIcon()
        {
            atlas = TextureHelper.InGameAtlas;
            spriteName = "PieChartWhiteBg";
            isInteractive = false;
            color = Color.white;

            InnerCircule = AddUIComponent<CustomUISprite>();
            InnerCircule.atlas = TextureHelper.InGameAtlas;
            InnerCircule.spriteName = "PieChartWhiteBg";
            InnerCircule.relativePosition = new Vector3(Border, Border);
            InnerCircule.color = InnerCircule.disabledColor = Color.black;

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
        protected CustomUISprite Thumbnail { get; set; }

        public Color32 StyleColor { set => Thumbnail.color = Thumbnail.disabledColor = value.GetStyleIconColor(); }
        public Style.StyleType Type { set => Thumbnail.spriteName = value.ToString(); }

        public StyleIcon()
        {
            Thumbnail = AddUIComponent<CustomUISprite>();
            Thumbnail.atlas = IMTTextures.Atlas;
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
        protected CustomUILabel CountLabel { get; }
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
            CountLabel = AddUIComponent<CustomUILabel>();
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
