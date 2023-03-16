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
        public virtual ModsCommon.UI.SpriteSet BackgroundSprites => new ModsCommon.UI.SpriteSet(CommonTextures.BorderBottom);
        public virtual ColorSet BackgroundColors => new ColorSet(new Color32(39, 44, 47, 255));
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
            hovered = new Color32(38, 100, 142, 255),
            pressed = new Color32(79, 143, 192, 255),
            focused = null,
            disabled = null,
        };
        public virtual ColorSet ForegroundSelectedColors => new ColorSet(new Color32(139, 181, 213, 255));

        public virtual ColorSet TextColor => new ColorSet(Color.white);
        public virtual ColorSet TextSelectedColor => new ColorSet(Color.black);

        protected virtual float TextScale => 0.65f;
        protected virtual int TextPaddingTop => 5;
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
            textPadding.top = TextPaddingTop;
            spritePadding = new RectOffset(2, 2, 2, 3);
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
                var iconSize = 19f;
                Icon.size = new Vector2(iconSize, iconSize);
                var fixOffset = 5 + spritePadding.left;
                var offset = (width - iconSize) * 0.5f;
                if (offset <= 1.5f * fixOffset)
                {
                    Icon.relativePosition = new Vector2(offset, spritePadding.top + (height - spritePadding.vertical - iconSize) * 0.5f);
                    textPadding.left = 50;
                }
                else
                {
                    Icon.relativePosition = new Vector2(fixOffset, spritePadding.top + (height - spritePadding.vertical - iconSize) * 0.5f);
                    textPadding.left = fixOffset + (int)iconSize + 5;
                }
            }
            else
                textPadding.left = 5 + spritePadding.left;

            if (ShowDelete && width >= 120f)
            {
                var buttonSize = 15f;
                DeleteButton.isVisible = true;
                DeleteButton.size = new Vector2(buttonSize, buttonSize);
                DeleteButton.relativePosition = new Vector2(size.x - buttonSize - 7 - spritePadding.right, spritePadding.top + (height - spritePadding.vertical - buttonSize) * 0.5f);
                textPadding.right = 19 + spritePadding.right;
            }
            else
            {
                DeleteButton.isVisible = false;
                textPadding.right = 7 + spritePadding.right;
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
