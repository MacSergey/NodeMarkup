using ColossalFramework.UI;
using ModsCommon.UI;
using NodeMarkup.Manager;
using NodeMarkup.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup.UI.Editors
{
    public interface IItemPanel<ObjectType>
    {
        public event Action<ObjectType> OnSelect;
        public event Action<ObjectType> OnDelete;

        public ObjectType SelectObject { get; }
        public ObjectType HoverObject { get; }
        public bool IsEmpty { get; }

        public void AddObject(ObjectType editObject);
        public void DeleteObject(ObjectType editObject);
        public void EditObject(ObjectType editObject);
        public void Clear();
        public void RefreshSelectedItem();
    }
    public class ItemsPanel<ItemType, ObjectType, ItemIcon> : AdvancedScrollablePanel, IItemPanel<ObjectType>
        where ItemType : EditItem<ObjectType, ItemIcon>
        where ItemIcon : UIComponent
        where ObjectType : class, IDeletable
    {
        protected NodeMarkupTool Tool => NodeMarkupTool.Instance;

        public event Action<ObjectType> OnSelect;
        public event Action<ObjectType> OnDelete;

        ItemType _selectItem;
        private ItemType SelectItem
        {
            get => _selectItem;
            set
            {
                if (_selectItem != null)
                    _selectItem.IsSelect = false;

                _selectItem = value;

                if (_selectItem != null)
                    _selectItem.IsSelect = true;

                OnSelect?.Invoke(_selectItem?.Object);
            }
        }
        public ObjectType SelectObject => SelectItem?.Object;

        private ItemType HoverItem { get; set; }
        public ObjectType HoverObject => HoverItem?.Object;
        public virtual bool IsEmpty => !Content.components.Any(c => c is ItemType);

        public void AddObject(ObjectType editObject) => AddObjectImpl(editObject);
        private ItemType AddObjectImpl(ObjectType editObject)
        {
            var item = GetItem(Content);
            InitItem(item, editObject);
            return item;
        }
        protected virtual ItemType GetItem(UIComponent parent)
        {
            var newItem = ComponentPool.Get<ItemType>(parent);
            newItem.width = parent.width;
            return newItem;
        }

        protected void InitItem(ItemType item, ObjectType editObject)
        {
            item.Init(editObject);
            item.eventClick += ItemClick;
            item.OnDelete += ItemDelete;
            item.eventMouseEnter += ItemHover;
            item.eventMouseLeave += ItemLeave;
        }

        public void DeleteObject(ObjectType editObject)
        {
            if (!(FindItem(editObject) is ItemType item))
                return;

            if (HoverItem == item)
                HoverItem = null;

            if (SelectItem == item)
            {
                var index = Math.Min(item.parent.components.IndexOf(item), item.parent.components.Count - 2);
                SelectItem = null;
                Select(FindItem(index));
            }

            DeleteItem(item);
        }
        private void DeleteItem(ItemType item)
        {
            item.eventClick -= ItemClick;
            item.eventMouseEnter -= ItemHover;
            item.eventMouseLeave -= ItemLeave;
            item.OnDelete -= ItemDelete;
            ComponentPool.Free(item);
        }
        private void DeleteUIComponent(UIComponent component)
        {
            Content.RemoveUIComponent(component);
            Destroy(component);
        }

        public void EditObject(ObjectType editObject)
        {
            var item = FindItem(editObject) ?? AddObjectImpl(editObject);
            Select(item);
        }

        private ItemType FindItem(ObjectType editObject) => Content.components.OfType<ItemType>().FirstOrDefault(c => ReferenceEquals(c.Object, editObject));
        private ItemType FindItem(int index) => index >= 0 && parent.components.Count > index ? Content.components[index] as ItemType : null;
        public virtual void Select(ItemType item)
        {
            if (item == null)
                return;

            SelectItem = item;
            ScrollTo(item);
        }
        public virtual void ScrollTo(ItemType item)
        {
            Content.ScrollToBottom();
            Content.ScrollIntoView(item);
        }

        public void Clear()
        {
            HoverItem = null;
            SelectItem = null;

            var components = Content.components.ToArray();
            foreach (var component in components)
            {
                if (component is ItemType item)
                    DeleteItem(item);
                else
                    DeleteUIComponent(component);
            }
        }

        private void ItemClick(UIComponent component, UIMouseEventParameter eventParam) => SelectItem = component as ItemType;
        private void ItemDelete(EditItem<ObjectType, ItemIcon> item) => OnDelete?.Invoke(item.Object);
        private void ItemHover(UIComponent component, UIMouseEventParameter eventParam) => HoverItem = component as ItemType;
        private void ItemLeave(UIComponent component, UIMouseEventParameter eventParam) => HoverItem = null;

        public override void Update()
        {
            //base.Update();

            if (SelectItem is ItemType item)
                item.IsSelect = true;
        }
        public void RefreshSelectedItem() => SelectItem?.Refresh();
    }
}
