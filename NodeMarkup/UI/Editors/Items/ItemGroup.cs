using ColossalFramework.UI;
using ModsCommon.UI;
using NodeMarkup.Manager;
using NodeMarkup.Utilities;
using System.Linq;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public class EditGroup<GroupType, ItemType, ObjectType> : UIAutoLayoutPanel, IReusable
        where ItemType : EditItem<ObjectType>
        where ObjectType : class, IDeletable
    {
        bool IReusable.InCache { get; set; }

        private bool _isExpand = true;
        public bool IsExpand
        {
            get => _isExpand;
            set
            {
                if (_isExpand == value)
                    return;

                _isExpand = value;
                Item.IsExpand = _isExpand;
                foreach (var item in components.Where(i => i != Item))
                    item.isVisible = _isExpand;
            }
        }

        public GroupItem Item { get; private set; }
        public GroupType Selector { get; private set; }
        public bool IsEmpty => components.Count <= 1;

        public EditGroup()
        {
            autoLayout = true;
            autoLayoutDirection = LayoutDirection.Vertical;
            autoLayoutPadding = new RectOffset(0, 0, 0, 0);
            autoFitChildrenVertically = true;

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
            Item.Text = groupName;
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
                item.width = width;
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

        public bool IsExpand { set => ExpandIcon.backgroundSprite = value ? NodeMarkupTextures.ArrowDown : NodeMarkupTextures.ArrowRight; }

        private CustomUIPanel ExpandIcon { get; set; }

        public GroupItem()
        {
            height = 35;
            AddExpandIcon();
        }

        public void Init()
        {
            SetColors();
            OnSizeChanged();
        }
        private void AddExpandIcon()
        {
            ExpandIcon = AddUIComponent<CustomUIPanel>();
            ExpandIcon.atlas = NodeMarkupTextures.Atlas;
            ExpandIcon.size = new Vector2(20, 20);
            IsExpand = true;
        }
        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            if (ExpandIcon != null)
            {
                ExpandIcon.size = new Vector2(size.y - 11, size.y - 11);
                ExpandIcon.relativePosition = new Vector2(size.x - (size.y - 3), 3);
            }

            Label.size = new Vector2(size.x - 6, size.y);
        }
        protected override void LabelSizeChanged() => Label.relativePosition = new Vector3(3, (height - Label.height) / 2);
    }
}
