using ColossalFramework.UI;
using IMT.Manager;
using IMT.Utilities;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System.Linq;
using UnityEngine;

namespace IMT.UI.Editors
{
    public abstract class EditGroup<GroupType, ItemType, ObjectType> : CustomUIPanel, IReusable
        where ItemType : EditItem<ObjectType>
        where ObjectType : class, IDeletable
    {
        bool IReusable.InCache { get; set; }

        private bool isExpand = true;
        public bool IsExpand
        {
            get => isExpand;
            set
            {
                if (isExpand == value)
                    return;

                isExpand = value;
                Header.IsExpand = isExpand;

                PauseLayout(() =>
                {
                    PaddingBottom = isExpand ? 15 : 0;
                    SetStyle();

                    foreach (var item in components.Where(i => i != Header))
                        item.isVisible = isExpand;
                });
            }
        }

        public GroupItem Header { get; private set; }
        public GroupType Selector { get; private set; }
        public bool IsEmpty => components.Count <= 1;
        protected abstract bool ShowIcon { get; }

        public EditGroup() : base()
        {
            autoLayout = AutoLayout.Vertical;
            padding = new RectOffset(0, 0, 0, 0);
            autoChildrenVertically = AutoLayoutChildren.Fit;

            atlas = CommonTextures.Atlas;
            BgColors = UIStyle.ItemGroupBackground;
            BackgroundSprite = CommonTextures.PanelBig;

            PauseLayout(() =>
            {
                Header = AddUIComponent<GroupItem>();
                Header.Init();
                Header.eventClick += ItemClick;
            });
        }

        protected abstract string GetName(GroupType group);
        protected abstract string GetSprite(GroupType group);
        private void ItemClick(UIComponent component, UIMouseEventParameter eventParam) => IsExpand = !IsExpand;

        public virtual void Init(GroupType selector)
        {
            Selector = selector;
            Header.text = GetName(selector);
            Header.FgAtlas = IMTTextures.Atlas;
            Header.FgSprites = ShowIcon ? GetSprite(selector) : string.Empty;
            IsExpand = false;
        }
        protected virtual void SetStyle()
        {
            var padding = width >= 150 ? EditItemBase.ExpandedPadding : EditItemBase.CollapsedPadding;

            if (isExpand)
                BackgroundPadding = new RectOffset(padding, padding, 4, 15);
            else
                BackgroundPadding = new RectOffset(padding, padding, 4, 4);

            if (ShowIcon)
            {
                if (width >= 100)
                {
                    Header.TextPadding.left = Header.DefaultBackgroundPadding.left + 5 + (int)Header.SpriteSize.x + 10;
                }
                else
                {
                    Header.TextPadding.left = 100;
                }

                if (width >= (Header.DefaultBackgroundPadding.left + 5) * 2)
                {
                    Header.SpritePadding.left = Header.DefaultBackgroundPadding.left + 5;
                    Header.HorizontalAlignment = UIHorizontalAlignment.Left;
                }
                else
                {
                    Header.SpritePadding.left = 0;
                    Header.HorizontalAlignment = UIHorizontalAlignment.Center;
                }
            }
            else
                Header.TextPadding.left = Header.DefaultBackgroundPadding.left + 10;
        }

        public virtual void Refresh()
        {
            foreach (var item in components.OfType<ItemType>())
                item.Refresh();
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            SetStyle();

            foreach (var item in components)
                item.width = width - Padding.horizontal;
        }

        public void DeInit()
        {
            PauseLayout(() =>
            {
                var components = this.components.OfType<ItemType>().ToArray();
                foreach (var component in components)
                    ComponentPool.Free(component);
            }, false, true);
        }
    }

    public class GroupItem : EditItemBase
    {
        public override SpriteSet BackgroundSprites => new SpriteSet()
        {
            normal = string.Empty,
            hovered = CommonTextures.PanelBig,
            pressed = CommonTextures.PanelBig,
            focused = string.Empty,
            disabled = string.Empty,
        };
        public override ColorSet BackgroundColors
        {
            get
            {
                var colors = base.BackgroundColors;
                colors.normal = colors.focused = UIStyle.ItemGroup;
                return colors;
            }
        }
        public override ColorSet DefaultTextColor => new ColorSet(Color.white);
        protected override float DefaultTextScale => 0.9f;
        protected override float DefaultHeight => 48f;
        public override RectOffset DefaultBackgroundPadding => width >= 150f ? new RectOffset(ExpandedPadding, ExpandedPadding, 4, 4) : new RectOffset(CollapsedPadding, CollapsedPadding, 4, 4);

        public bool IsExpand { set => ExpandIcon.spriteName = value ? CommonTextures.ArrowDown : CommonTextures.ArrowRight; }

        private CustomUISprite ExpandIcon { get; set; }
        private static int ExpandPadding => 7;

        public GroupItem() : base()
        {
            TextPadding.top = 5;
            NormalBgSprite = CommonTextures.PanelSmall;

            ForegroundSpriteMode = SpriteMode.FixedSize;
            SpriteSize = new Vector2(20, 20f);

            WordWrap = false;

            ExpandIcon = AddUIComponent<CustomUISprite>();
            ExpandIcon.atlas = CommonTextures.Atlas;
            ExpandIcon.color = Color.white;
            ExpandIcon.size = new Vector2(16, 16);
            IsExpand = true;
        }

        public void Init()
        {
            Refresh();
        }
        private void Refresh()
        {
            SetStyle();

            if (ExpandIcon != null)
            {
                ExpandIcon.isVisible = width >= 100f;
                var offset = (height - ExpandIcon.height) * 0.5f;
                ExpandIcon.relativePosition = new Vector2(width - DefaultBackgroundPadding.right - ExpandIcon.width - ExpandPadding, offset);

                TextPadding.right = (ExpandIcon.isVisible ? (int)ExpandIcon.width + ExpandPadding : 0) + DefaultBackgroundPadding.right + DefaultTextPadding;
            }
        }
        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            Refresh();
        }
    }
}
