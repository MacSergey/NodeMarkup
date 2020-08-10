using ColossalFramework.UI;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public EditableItemType AddItem(EditableObject editableObject)
        {
            var item = AddUIComponent<EditableItemType>();
            item.Init();
            item.name = editableObject.ToString();
            item.width = width;
            item.Object = editableObject;
            item.isVisible = _isExpand;

            return item;
        }
        public void DeleteItem(EditableItemType item)
        {
            RemoveUIComponent(item);
            Destroy(item.gameObject);
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            foreach (var item in components)
                item.width = width;
        }
    }

    public class GroupItem : UIButton
    {
        public bool IsExpand { set => ExpandIcon.backgroundSprite = value ? "PropertyGroupOpen" : "PropertyGroupClosed"; }
        protected UILabel Label { get; set; }

        public string Text
        {
            get => Label.text;
            set => Label.text = value;
        }
        private UIPanel ExpandIcon { get; set; }

        public GroupItem()
        {
            AddLable();
            AddExpandIcon();

            atlas = TextureUtil.InGameAtlas;

            normalBgSprite = "ButtonSmallPressed";
            colorizeSprites = true;

            focusedColor = color = new Color32(165, 255, 255, 255);
            hoveredColor = new Color32(145, 208, 208, 255);
            pressedColor = new Color32(125, 160, 160, 255);

            height = 25;
        }

        public void Init()
        {
            OnSizeChanged();
        }
        private void AddLable()
        {
            Label = AddUIComponent<UILabel>();
            Label.textAlignment = UIHorizontalAlignment.Left;
            Label.verticalAlignment = UIVerticalAlignment.Middle;
            Label.autoSize = false;
            Label.autoHeight = false;
            Label.textScale = 0.6f;
            Label.padding = new RectOffset(0, 0, 2, 0);
        }
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

            ExpandIcon.size = new Vector2(size.y - 6, size.y - 6);
            ExpandIcon.relativePosition = new Vector2(size.x - (size.y - 3), 3);

            Label.size = new Vector2(size.x - 28, size.y);
            Label.relativePosition = new Vector3(3, 0);
        }
    }
}
