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
                foreach (var item in components.Where(i => i != Item))
                    item.isVisible = isExpand;
            }
        }

        public GroupItem Item { get; private set; }
        public GroupType Selector { get; private set; }
        public bool IsEmpty => components.Count <= 1;

        public EditGroup()
        {
            autoLayoutDirection = LayoutDirection.Vertical;
            autoLayoutPadding = new RectOffset(0, 0, 0, 0);
            autoFitChildrenVertically = true;

            atlas = CommonTextures.Atlas;
            backgroundSprite = CommonTextures.PanelSmall;
            color = disabledColor = new Color32(29, 58, 77, 255);

            AddGroupItem();
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
                item.width = width - autoLayoutPadding.horizontal;
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
        public override Color32 NormalColor => new Color32(114, 197, 255, 255);
        public override Color32 HoveredColor => new Color32(97, 180, 239, 255);
        public override Color32 PressedColor => new Color32(86, 167, 225, 255);
        public override Color32 FocusColor => NormalColor;
        protected override float TextScale => 0.65f;
        protected override float DefaultHeight => 36f;

        public bool IsExpand { set => ExpandIcon.spriteName = value ? CommonTextures.ArrowDown : CommonTextures.ArrowRight; }

        private CustomUISprite ExpandIcon { get; set; }

        public GroupItem() : base()
        {
            textPadding.right = 30;
            textPadding.top = 5;
            textPadding.left = 5;

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
            SetColors();

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
