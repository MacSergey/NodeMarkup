using ColossalFramework.UI;
using NodeMarkup.Utils;
using System.Linq;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public class EditableGroup<GroupType, EditableItemType, EditableObject, ItemIcon> : UIPanel
        where EditableItemType : EditableItem<EditableObject, ItemIcon>
        where ItemIcon : UIComponent
        where EditableObject : class
    {
        bool _isExpand = true;
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

        private GroupItem Item { get; set; }
        public GroupType Selector { get; private set; }
        public bool IsEmpty => components.Count <= 1;

        public EditableGroup()
        {
            autoLayout = true;
            autoLayoutDirection = LayoutDirection.Vertical;
            autoLayoutPadding = new RectOffset(0, 0, 0, 0);
            autoFitChildrenVertically = true;

            AddGroupItem();
            IsExpand = false;
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
        }

        public EditableItemType NewItem()
        {
            var item = AddUIComponent<EditableItemType>();
            item.width = width;
            item.isVisible = _isExpand;

            return item;
        }
        public void DeleteItem(EditableItemType item)
        {
            RemoveUIComponent(item);
            Destroy(item.gameObject);
        }
        public virtual void Refresh()
        {
            foreach (var item in components.OfType<EditableItemType>())
                item.Refresh();
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            foreach (var item in components)
                item.width = width;
        }
    }

    public class GroupItem : EditableItemBase
    {
        public override Color32 NormalColor => new Color32(108, 169, 218, 255);
        public override Color32 HoveredColor => new Color32(93, 145, 213, 255);
        public override Color32 PressedColor => new Color32(75, 127, 192, 255);
        public override Color32 FocusColor => NormalColor;

        public bool IsExpand { set => ExpandIcon.backgroundSprite = value ? "PropertyGroupOpen" : "PropertyGroupClosed"; }

        private UIPanel ExpandIcon { get; set; }

        public GroupItem()
        {
            height = 35;
            AddExpandIcon();
        }

        public void Init() => OnSizeChanged();
        private void AddExpandIcon()
        {
            ExpandIcon = AddUIComponent<UIPanel>();
            ExpandIcon.atlas = TextureUtil.InMapEditorAtlas;
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
            Label.relativePosition = new Vector3(3, (height - Label.height) / 2);
        }
    }
}
