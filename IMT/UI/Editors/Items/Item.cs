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
        public virtual SpriteSet BackgroundSprites => new SpriteSet()
        {
            normal = string.Empty,
            hovered = CommonTextures.PanelSmall,
            pressed = CommonTextures.PanelSmall,
            focused = string.Empty,
            disabled = CommonTextures.PanelSmall,
        };
        public virtual SpriteSet BackgroundSelectedSprites => new SpriteSet(CommonTextures.PanelSmall);

        public virtual ColorSet BackgroundColors => new ColorSet()
        {
            normal = default,
            hovered = UIStyle.ItemHovered,
            pressed = UIStyle.ItemPressed,
            focused = default,
            disabled = default,
        };
        public virtual ColorSet BackgroundSelectedColors => new ColorSet(UIStyle.ItemFocused);

        public virtual ColorSet DefaultTextColor => Color.white;
        public virtual ColorSet DefaultSelTextColor => Color.black;
        public virtual RectOffset DefaultBackgroundPadding => width >= 150f ? new RectOffset(ExpandedPadding, ExpandedPadding, 2, 2) : new RectOffset(CollapsedPadding, CollapsedPadding, 2, 2);
        protected static int DefaultTextPadding => 5;
        public static int ExpandedPadding => 8;
        public static int CollapsedPadding => 4;

        protected virtual float DefaultTextScale => 0.65f;
        protected virtual float DefaultHeight => 32f;

        public EditItemBase()
        {
            m_Size = new Vector2(100f, DefaultHeight);
            clipChildren = true;
            Atlas = CommonTextures.Atlas;

            VerticalAlignment = UIVerticalAlignment.Middle;
            HorizontalAlignment = UIHorizontalAlignment.Center;

            TextHorizontalAlignment = UIHorizontalAlignment.Left;
            TextVerticalAlignment = UIVerticalAlignment.Middle;
            WordWrap = true;
        }

        public virtual void DeInit()
        {
            IsSelected = false;
            isVisible = true;
        }

        protected virtual void SetStyle()
        {
            bgSprites = BackgroundSprites;
            bgColors = BackgroundColors;
            selBgSprites = BackgroundSelectedSprites;
            selBgColors = BackgroundSelectedColors;

            textColors = DefaultTextColor;
            selTextColors = DefaultSelTextColor;

            textScale = DefaultTextScale;
            TextPadding.top = 5;
            BackgroundPadding = DefaultBackgroundPadding;
        }
    }
    public abstract class EditItem<ObjectType> : EditItemBase, IReusable
        where ObjectType : class, IDeletable
    {
        public event Action<EditItem<ObjectType>> OnDelete;

        bool IReusable.InCache { get; set; }
        Transform IReusable.CachedTransform { get => m_CachedTransform; set => m_CachedTransform = value; }

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
            DeleteButton = AddUIComponent<CustomUIButton>();
            DeleteButton.name = nameof(DeleteButton);
            DeleteButton.Atlas = CommonTextures.Atlas;
            DeleteButton.IconSprites = new SpriteSet(CommonTextures.CloseButtonNormal, CommonTextures.CloseButtonHovered, CommonTextures.CloseButtonPressed, string.Empty, string.Empty);
            DeleteButton.IconMode = SpriteMode.Stretch;
            DeleteButton.size = new Vector2(20, 20);
            DeleteButton.eventClick += DeleteClick;
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
                m_Size.y = DefaultHeight + DefaultBackgroundPadding.vertical;
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

                var centerOffset = Mathf.FloorToInt(width - DefaultBackgroundPadding.vertical - iconSize) / 2;
                var fixOffset = DefaultTextPadding;
                var widthOffset = DefaultBackgroundPadding.left + Math.Min(fixOffset, centerOffset);
                var heightOffset = DefaultBackgroundPadding.top + (height - DefaultBackgroundPadding.vertical - iconSize) * 0.5f;
                Icon.relativePosition = new Vector3(widthOffset, heightOffset);
                TextPadding.left = centerOffset <= 1.5f * fixOffset ? 50 : DefaultBackgroundPadding.left + fixOffset + iconSize + DefaultTextPadding;
            }
            else
                TextPadding.left = DefaultTextPadding + DefaultBackgroundPadding.left;

            if (ShowDelete && width >= 120f)
            {
                var buttonSize = 15;
                DeleteButton.isVisible = true;
                DeleteButton.size = new Vector2(buttonSize, buttonSize);
                DeleteButton.relativePosition = new Vector2(size.x - buttonSize - 7 - DefaultBackgroundPadding.right, DefaultBackgroundPadding.top + (height - DefaultBackgroundPadding.vertical - buttonSize) * 0.5f);
                TextPadding.right = 19 + DefaultBackgroundPadding.right;
            }
            else
            {
                DeleteButton.isVisible = false;
                TextPadding.right = DefaultTextPadding + DefaultBackgroundPadding.right;
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
            InnerCircule.name = nameof(InnerCircule);
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
            Thumbnail.name = nameof(Thumbnail);
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
            CountLabel.name = nameof(CountLabel);
            CountLabel.textColor = Color.white;
            CountLabel.textScale = 0.7f;
            CountLabel.relativePosition = new Vector3(0, 0);
            CountLabel.AutoSize = AutoSize.None;
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
