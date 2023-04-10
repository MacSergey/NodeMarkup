using ColossalFramework.UI;
using IMT.Manager;
using IMT.Utilities;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Linq;
using UnityEngine;

namespace IMT.UI.Editors
{
    public abstract class EditGroup<GroupType, ItemType, ObjectType> : CustomUIPanel, IReusable
        where ItemType : EditItem<ObjectType>
        where ObjectType : class, IDeletable
    {
        bool IReusable.InCache { get; set; }
        Transform IReusable.CachedTransform { get => m_CachedTransform; set => m_CachedTransform = value; }


        public event Action<bool> OnExpandChanged;

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
        private void ItemClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (Utility.ShiftIsPressed)
                OnExpandChanged?.Invoke(!IsExpand);
            else
                IsExpand = !IsExpand;
        }

        public virtual void Init(GroupType selector)
        {
            Selector = selector;
            Header.text = GetName(selector);
            Header.IconAtlas = IMTTextures.Atlas;
            Header.IconSprites = ShowIcon ? GetSprite(selector) : string.Empty;
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
                    Header.TextPadding.left = Header.DefaultBackgroundPadding.left + 5 + (int)Header.IconSize.x + 10;
                }
                else
                {
                    Header.TextPadding.left = 100;
                }

                if (width >= (Header.DefaultBackgroundPadding.left + 5) * 2)
                {
                    Header.IconPadding.left = Header.DefaultBackgroundPadding.left + 5;
                    Header.HorizontalAlignment = UIHorizontalAlignment.Left;
                }
                else
                {
                    Header.IconPadding.left = 0;
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

        public bool IsExpand { set => ExpandButton.spriteName = value ? CommonTextures.ArrowDown : CommonTextures.ArrowRight; }

        private CustomUISprite ExpandButton { get; set; }
        private static int ExpandPadding => 7;

        public GroupItem() : base()
        {
            TextPadding.top = 5;
            NormalBgSprite = CommonTextures.PanelSmall;

            IconMode = SpriteMode.FixedSize;
            IconSize = new Vector2(20, 20f);

            WordWrap = false;

            ExpandButton = AddUIComponent<CustomUISprite>();
            ExpandButton.atlas = CommonTextures.Atlas;
            ExpandButton.color = Color.white;
            ExpandButton.size = new Vector2(16, 16);
            ExpandButton.tooltip = string.Format(IMT.Localize.Header_ExpandGroupTooltip, LocalizeExtension.Shift);
            IsExpand = true;
        }

        public void Init()
        {
            Refresh();
        }
        private void Refresh()
        {
            SetStyle();

            if (ExpandButton != null)
            {
                ExpandButton.isVisible = width >= 100f;
                var offset = (height - ExpandButton.height) * 0.5f;
                ExpandButton.relativePosition = new Vector2(width - DefaultBackgroundPadding.right - ExpandButton.width - ExpandPadding, offset);

                TextPadding.right = (ExpandButton.isVisible ? (int)ExpandButton.width + ExpandPadding : 0) + DefaultBackgroundPadding.right + DefaultTextPadding;
            }
        }
        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            Refresh();
        }
    }
}
