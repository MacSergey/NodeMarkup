using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.UI;
using NodeMarkup.Manager;
using NodeMarkup.Tools;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NodeMarkup.UI.Editors
{
    public interface IItemPanel<ObjectType>
    {
        public event Action<ObjectType> OnSelectClick;
        public event Action<ObjectType> OnDeleteClick;

        public ObjectType SelectedObject { get; }
        public ObjectType HoverObject { get; }
        public bool IsEmpty { get; }

        public void Init(Editor editor);

        public void SetObjects(IEnumerable<ObjectType> editObjects);
        public void AddObject(ObjectType editObject);
        public void DeleteObject(ObjectType editObject);
        public void EditObject(ObjectType editObject);
        public void SelectObject(ObjectType editObject);
        public void RefreshSelectedItem();
    }
    public abstract class ItemsPanel<ItemType, ObjectType> : AdvancedScrollablePanel, IItemPanel<ObjectType>, IComparer<ObjectType>
        where ItemType : EditItem<ObjectType>
        where ObjectType : class, IDeletable
    {
        #region EVENTS

        public event Action<ObjectType> OnSelectClick;
        public event Action<ObjectType> OnDeleteClick;

        #endregion

        #region PROPERTIES

        protected NodeMarkupTool Tool => SingletonTool<NodeMarkupTool>.Instance;
        protected Editor Editor { get; private set; }

        private ItemType _selectItem;
        protected ItemType SelectItem
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
        public ObjectType SelectedObject => SelectItem?.Object;

        protected ItemType HoverItem { get; set; }
        public ObjectType HoverObject => HoverItem?.Object;
        public virtual bool IsEmpty => !Content.components.Any(c => c is ItemType);

        #endregion

        public void Init(Editor editor)
        {
            Editor = editor;
        }

        #region ADD OBJECT

        public void SetObjects(IEnumerable<ObjectType> editObjects)
        {
            StopLayout();

            Clear();

            var objects = editObjects.OrderBy(o => o, this).ToArray();
            foreach (var editObject in objects)
                AddObjectImpl(editObject, -1);

            StartLayout();
        }
        public void AddObject(ObjectType editObject) => AddObjectImpl(editObject);
        protected virtual ItemType AddObjectImpl(ObjectType editObject)
        {
            var index = FindIndex(editObject);
            return AddObjectImpl(editObject, index >= 0 ? index : ~index);
        }
        protected virtual ItemType AddObjectImpl(ObjectType editObject, int zOrder)
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

        public virtual void DeleteObject(ObjectType editObject)
        {
            if (FindItem(editObject) is not ItemType item)
                return;

            if (HoverItem == item)
                HoverItem = null;

            if (SelectItem == item)
            {
                SelectItem = null;
                DeleteSelectedItem(item);
            }
            else
                DeleteItem(item);
        }
        protected virtual void DeleteSelectedItem(ItemType item)
        {
            var index = Math.Min(Content.components.IndexOf(item), Content.components.Count - 2);
            DeleteItem(item);
            Select(FindItem(index));
        }

        protected virtual void DeleteItem(ItemType item)
        {
            item.eventClick -= ItemClick;
            item.eventMouseEnter -= ItemHover;
            item.eventMouseLeave -= ItemLeave;
            item.OnDelete -= ItemDelete;
            ComponentPool.Free(item);
        }
        public virtual void Clear()
        {
            HoverItem = null;
            SelectItem = null;

            var components = Content.components.ToArray();
            foreach (var component in components)
            {
                if (component is ItemType item)
                    DeleteItem(item);
                else
                    ComponentPool.Free(component);
            }
        }

        #endregion

        #region EDIT UPDATE SELECT

        public void EditObject(ObjectType editObject)
        {
            if (editObject != null)
                Select(FindItem(editObject) ?? AddObjectImpl(editObject));
            else
                Select(FindItem(0));
        }
        public void SelectObject(ObjectType editObject)
        {
            if (editObject != null && FindItem(editObject) is ItemType item)
                Select(item);
            else
                Select(FindItem(0));
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
            //WAIT FPS BOOSTER FIX 
            //base.Update();

            if (SelectItem is ItemType item)
                item.IsSelect = true;
        }

        #endregion

        #region HANDLERS

        private void ItemClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (component is ItemType item)
            {
                SelectItem = item;
                ItemClick(item);
            }
        }
        private void ItemDelete(EditItem<ObjectType> item) => OnDeleteClick?.Invoke(item.Object);
        private void ItemHover(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (component is ItemType item)
            {
                HoverItem = item;
                ItemHover(item);
            }
        }
        private void ItemLeave(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (component is ItemType item)
            {
                HoverItem = null;
                ItemLeave(item);
            }
        }

        protected virtual void ItemClick(ItemType item) { }
        protected virtual void ItemHover(ItemType item) { }
        protected virtual void ItemLeave(ItemType item) { }

        #endregion

        #region ADDITIONAL

        protected virtual ItemType FindItem(ObjectType editObject) => Content.components.OfType<ItemType>().FirstOrDefault(c => ReferenceEquals(c.Object, editObject));
        protected virtual ItemType FindItem(int index) => FindItem<ItemType>(index, Content);
        protected T FindItem<T>(int index, UIComponent parent) where T : UIComponent => index >= 0 && parent.components.Count > index ? parent.components[index] as T : null;

        protected virtual int FindIndex(ObjectType editObject) => FindIndex(editObject, Content);
        protected int FindIndex(ObjectType editObject, UIComponent parent) => Array.BinarySearch(parent.components.OfType<ItemType>().Select(i => i.Object).ToArray(), editObject, this);

        public virtual void ScrollTo(ItemType item)
        {
            Content.ScrollToBottom();
            Content.ScrollIntoViewRecursive(item);
        }
        public void RefreshSelectedItem() => SelectItem?.Refresh();
        public virtual void RefreshItems()
        {
            foreach (var item in Content.components.OfType<ItemType>())
                item.Refresh();
        }
        public abstract int Compare(ObjectType x, ObjectType y);

        #endregion
    }
}
