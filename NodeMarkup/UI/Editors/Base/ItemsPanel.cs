using ColossalFramework.UI;
using ModsCommon.UI;
using NodeMarkup.Manager;
using NodeMarkup.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup.UI.Editors
{
    public interface IItemPanel<ObjectType>
    {
        public event Action<ObjectType> OnSelectClick;
        public event Action<ObjectType> OnDeleteClick;

        public ObjectType SelectObject { get; }
        public ObjectType HoverObject { get; }
        public bool IsEmpty { get; }

        public void Init(IEnumerable<ObjectType> editObjects);
        public void AddObject(ObjectType editObject);
        public void DeleteObject(ObjectType editObject);
        public void EditObject(ObjectType editObject);
        public void Clear();
        public void RefreshSelectedItem();
    }
    public abstract class ItemsPanel<ItemType, ObjectType, ItemIcon> : AdvancedScrollablePanel, IItemPanel<ObjectType>, IComparer<ObjectType>
        where ItemType : EditItem<ObjectType, ItemIcon>
        where ItemIcon : UIComponent
        where ObjectType : class, IDeletable
    {
        #region EVENTS

        public event Action<ObjectType> OnSelectClick;
        public event Action<ObjectType> OnDeleteClick;

        #endregion

        #region PROPERTIES

        protected NodeMarkupTool Tool => NodeMarkupTool.Instance;
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

                OnSelectClick?.Invoke(_selectItem?.Object);
            }
        }
        public ObjectType SelectObject => SelectItem?.Object;

        private ItemType HoverItem { get; set; }
        public ObjectType HoverObject => HoverItem?.Object;
        public virtual bool IsEmpty => !Content.components.Any(c => c is ItemType);

        #endregion

        #region ADD OBJECT

        public void Init(IEnumerable<ObjectType> editObjects)
        {
            Clear();
            var objects = editObjects.OrderBy(o => o, this).ToArray();

            StopLayout();
            foreach (var editObject in objects)
                AddObjectImpl(editObject, -1);
            StartLayout();
        }
        public void AddObject(ObjectType editObject) => AddObjectImpl(editObject);
        private ItemType AddObjectImpl(ObjectType editObject)
        {
            var index = FindIndex(editObject);
            return AddObjectImpl(editObject, index >= 0 ? index : ~index);
        }
        private ItemType AddObjectImpl(ObjectType editObject, int zOrder)
        {
            var item = GetItem(Content, zOrder);
            InitItem(item, editObject);
            return item;
        }
        protected virtual ItemType GetItem(UIComponent parent, int zOrder = -1)
        {
            var newItem = ComponentPool.Get<ItemType>(parent, zOrder: zOrder);
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

        #endregion

        #region DELETE OBJECT

        public void DeleteObject(ObjectType editObject)
        {
            if (!(FindItem(editObject) is ItemType item))
                return;

            if (HoverItem == item)
                HoverItem = null;

            if (SelectItem == item)
            {
                var index = Math.Min(Content.components.IndexOf(item), Content.components.Count - 2);
                SelectItem = null;
                DeleteItem(item);
                Select(FindItem(index));
            }
            else
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

        #endregion

        #region EDIT UPDATE SELECT

        public void EditObject(ObjectType editObject)
        {
            if (editObject != null)
            {
                var item = FindItem(editObject) ?? AddObjectImpl(editObject);
                Select(item);
            }
            else
            {
                var item = FindItem(0);
                Select(item);
            }
        }

        public virtual void Select(ItemType item)
        {
            if (item == null)
                return;

            SelectItem = item;
            ScrollTo(item);
        }
        public override void Update()
        {
            base.Update();

            if (SelectItem is ItemType item)
                item.IsSelect = true;
        }

        #endregion

        #region HANDLERS

        private void ItemClick(UIComponent component, UIMouseEventParameter eventParam) => SelectItem = component as ItemType;
        private void ItemDelete(EditItem<ObjectType, ItemIcon> item) => OnDeleteClick?.Invoke(item.Object);
        private void ItemHover(UIComponent component, UIMouseEventParameter eventParam) => HoverItem = component as ItemType;
        private void ItemLeave(UIComponent component, UIMouseEventParameter eventParam) => HoverItem = null;

        #endregion

        #region ADDITIONAL

        private ItemType FindItem(ObjectType editObject) => Content.components.OfType<ItemType>().FirstOrDefault(c => ReferenceEquals(c.Object, editObject));
        private ItemType FindItem(int index) => index >= 0 && Content.components.Count > index ? Content.components[index] as ItemType : null;
        private int FindIndex(ObjectType editObject) => Array.BinarySearch(Content.components.OfType<ItemType>().Select(i => i.Object).ToArray(), editObject, this);
        public virtual void ScrollTo(ItemType item)
        {
            Content.ScrollToBottom();
            Content.ScrollIntoView(item);
        }
        public void RefreshSelectedItem() => SelectItem?.Refresh();
        public abstract int Compare(ObjectType x, ObjectType y);

        #endregion
    }
}
