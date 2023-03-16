using ColossalFramework.UI;
using IMT.Manager;
using IMT.Utilities;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System.Linq;
using UnityEngine;

namespace IMT.UI.Editors
{
    public class EditGroup<GroupType, ItemType, ObjectType> : UIAutoLayoutPanel, IReusable
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
                Item.IsExpand = isExpand;

                StopLayout();
                {
                    foreach (var item in components.Where(i => i != Item))
                        item.isVisible = isExpand;
                }
                StartLayout();
            }
        }

        public GroupItem Item { get; private set; }
        public GroupType Selector { get; private set; }
        public bool IsEmpty => components.Count <= 1;

        public EditGroup()
        {
            StopLayout();
            {
                autoLayoutDirection = LayoutDirection.Vertical;
                autoLayoutPadding = new RectOffset(0, 0, 0, 0);
                autoFitChildrenVertically = true;

                atlas = CommonTextures.Atlas;
                //foregroundSprite = CommonTextures.PanelBig;
                //normalFgColor = new Color32(29, 77, 109, 255);
                //spritePadding = new RectOffset(2, 2, 2, 2);

                AddGroupItem();
            }
            StartLayout();
        }
        private void AddGroupItem()
        {
            Item = AddUIComponent<GroupItem>();
            Item.Init();
            Item.eventClick += ItemClick;
        }

        private void ItemClick(UIComponent component, UIMouseEventParameter eventParam) => IsExpand = !IsExpand;

        public void Init(GroupType selector, string groupName)
        {
            Selector = selector;
            Item.text = groupName;
            IsExpand = false;
        }
        public virtual void Refresh()
        {
            foreach (var item in components.OfType<ItemType>())
                item.Refresh();
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            foreach (var item in components)
                item.width = width - autoLayoutPadding.horizontal - padding.horizontal;
        }

        public void DeInit()
        {
            StopLayout();

            var components = this.components.OfType<ItemType>().ToArray();
            foreach (var component in components)
                ComponentPool.Free(component);

            StartLayout(false);
        }
    }

    public class GroupItem : EditItemBase
    {
        public override ModsCommon.UI.SpriteSet BackgroundSprites => new ModsCommon.UI.SpriteSet(string.Empty);
        public override ModsCommon.UI.SpriteSet ForegroundSprites => new ModsCommon.UI.SpriteSet()
        {
            normal = CommonTextures.PanelBig,
            hovered = CommonTextures.PanelBig,
            pressed = CommonTextures.PanelBig,
            focused = CommonTextures.PanelBig,
            disabled = string.Empty,
        };
        public override ColorSet ForegroundColors
        {
            get
            {
                var colors = base.ForegroundColors;
                colors.normal = colors.focused = new Color32(29, 75, 106, 255);
                return colors;
            }
        }
        public override ColorSet TextColor => new ColorSet(Color.white);
        protected override float TextScale => 0.8f;
        protected override float DefaultHeight => 48f;

        public bool IsExpand { set => ExpandIcon.spriteName = value ? CommonTextures.ArrowDown : CommonTextures.ArrowRight; }

        private CustomUISprite ExpandIcon { get; set; }

        public GroupItem() : base()
        {
            textPadding.right = 30;
            textPadding.top = 5;
            textPadding.left = 8;

            normalBgSprite = CommonTextures.PanelSmall;

            AddExpandIcon();
        }

        public void Init()
        {
            Refresh();
        }
        private void AddExpandIcon()
        {
            ExpandIcon = AddUIComponent<CustomUISprite>();
            ExpandIcon.atlas = CommonTextures.Atlas;
            ExpandIcon.color = Color.white;
            ExpandIcon.size = new Vector2(16, 16);
            IsExpand = true;
        }
        private void Refresh()
        {
            SetStyle();

            if (ExpandIcon != null)
            {
                ExpandIcon.isVisible = width >= 100f;
                var offset = (height - ExpandIcon.height) * 0.5f;
                ExpandIcon.relativePosition = new Vector2(width - ExpandIcon.width - offset, offset);

                textPadding.right = ExpandIcon.isVisible ? 30 : 5;
            }
        }
        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            Refresh();
        }
    }
}
