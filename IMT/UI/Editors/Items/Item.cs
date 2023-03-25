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
        public virtual ModsCommon.UI.SpriteSet BackgroundSprites => new ModsCommon.UI.SpriteSet(string.Empty);
        public virtual ColorSet BackgroundColors => new ColorSet();

        public virtual ModsCommon.UI.SpriteSet ForegroundSprites => new ModsCommon.UI.SpriteSet()
        {
            normal = string.Empty,
            hovered = CommonTextures.PanelSmall,
            pressed = CommonTextures.PanelSmall,
            focused = string.Empty,
            disabled = CommonTextures.PanelSmall,
        };
        public virtual ModsCommon.UI.SpriteSet ForegroundSelectedSprites => new ModsCommon.UI.SpriteSet(CommonTextures.PanelSmall);

        public virtual ColorSet ForegroundColors => new ColorSet()
        {
            normal = null,
            hovered = IMTColors.ItemHovered,
            pressed = IMTColors.ItemPressed,
            focused = null,
            disabled = null,
        };
        public virtual ColorSet ForegroundSelectedColors => new ColorSet(IMTColors.ItemFocused);

        public virtual ColorSet TextColor => new ColorSet(Color.white);
        public virtual ColorSet TextSelectedColor => new ColorSet(Color.black);
        public virtual RectOffset SpritePadding => width >= 150f ? new RectOffset(ExpandedPadding, ExpandedPadding, 2, 2) : new RectOffset(CollapsedPadding, CollapsedPadding, 2, 2);
        protected static int TextPadding => 5;
        public static int ExpandedPadding => 8;
        public static int CollapsedPadding => 4;

        protected virtual float TextScale => 0.65f;
        protected virtual float DefaultHeight => 32f;

        public EditItemBase()
        {
            m_Size = new Vector2(100f, DefaultHeight);
            clipChildren = true;
            atlas = CommonTextures.Atlas;

            textHorizontalAlignment = UIHorizontalAlignment.Left;
            textVerticalAlignment = UIVerticalAlignment.Middle;
            wordWrap = true;
        }

        public virtual void DeInit()
        {
            isSelected = false;
            isVisible = true;
        }

        protected virtual void SetStyle()
        {
            SetBgSprite(BackgroundSprites);
            SetBgColor(BackgroundColors);

            SetFgSprite(ForegroundSprites);
            SetFgColor(ForegroundColors);
            SetTextColor(TextColor);

            SetSelectedFgSprite(ForegroundSelectedSprites);
            SetSelectedFgColor(ForegroundSelectedColors);
            SetSelectedTextColor(TextSelectedColor);

            textScale = TextScale;
            textPadding.top = 5;
            spritePadding = SpritePadding;
        }
    }
    public abstract class EditItem<ObjectType> : EditItemBase, IReusable
        where ObjectType : class, IDeletable
    {
        public event Action<EditItem<ObjectType>> OnDelete;

        bool IReusable.InCache { get; set; }

        protected Editor Editor { get; private set; }
        private ObjectType editObject;
        public ObjectType EditObject
        {
            get => editObject;
            private set
            {
                editObject = value;
                if (editObject != null)
                    Refresh();
            }
        }
        protected bool Inited => EditObject != null;
        protected CustomUIButton DeleteButton { get; set; }
        public virtual bool ShowDelete => true;

        public EditItem() : base()
        {
            AddDeleteButton();
        }

        public virtual void Init(Editor editor, ObjectType editObject, bool inGroup)
        {
            Editor = editor;
            EditObject = editObject;

            Refresh();
        }
        public override void DeInit()
        {
            base.DeInit();

            text = string.Empty;
            Editor = null;
            EditObject = null;
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
            text = EditObject.ToString();
            SetStyle();
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
            if (inGroup)
            {
                m_Size.y = DefaultHeight + SpritePadding.vertical;
            }
            else
            {
                m_Size.y = DefaultHeight;
            }
            base.Init(editor, editObject, inGroup);
        }
        public override void Refresh()
        {
            base.Refresh();

            if (ShowIcon)
            {
                var iconSize = 19;
                Icon.size = new Vector2(iconSize, iconSize);

                var centerOffset = Mathf.FloorToInt(width - spritePadding.vertical - iconSize) / 2;
                var fixOffset = TextPadding;
                var widthOffset = spritePadding.left + Math.Min(fixOffset, centerOffset);
                var heightOffset = spritePadding.top + (height - spritePadding.vertical - iconSize) * 0.5f;
                Icon.relativePosition = new Vector3(widthOffset, heightOffset);
                textPadding.left = centerOffset <= 1.5f * fixOffset ? 50 : spritePadding.left + fixOffset + iconSize + TextPadding;
            }
            else
                textPadding.left = TextPadding + spritePadding.left;

            if (ShowDelete && width >= 120f)
            {
                var buttonSize = 15;
                DeleteButton.isVisible = true;
                DeleteButton.size = new Vector2(buttonSize, buttonSize);
                DeleteButton.relativePosition = new Vector2(size.x - buttonSize - 7 - spritePadding.right, spritePadding.top + (height - spritePadding.vertical - buttonSize) * 0.5f);
                textPadding.right = 19 + spritePadding.right;
            }
            else
            {
                DeleteButton.isVisible = false;
                textPadding.right = TextPadding + spritePadding.right;
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
            CountLabel.HorizontalAlignment = UIHorizontalAlignment.Center;
            CountLabel.VerticalAlignment = UIVerticalAlignment.Middle;
            CountLabel.Padding = new RectOffset(0, 0, 5, 0);
        }
        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            if (CountLabel != null)
                CountLabel.size = size;
        }
    }
}
