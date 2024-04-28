using ColossalFramework.UI;
using IMT.Manager;
using IMT.Tools;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IMT.UI.Editors
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
    public abstract class ItemsPanel<ItemType, ObjectType> : CustomUIScrollablePanel, IItemPanel<ObjectType>, IComparer<ObjectType>
        where ItemType : EditItem<ObjectType>
        where ObjectType : class, IDeletable
    {
        #region EVENTS

        public event Action<ObjectType> OnSelectClick;
        public event Action<ObjectType> OnDeleteClick;

        #endregion

        #region PROPERTIES

        protected IntersectionMarkingTool Tool => SingletonTool<IntersectionMarkingTool>.Instance;
        protected Editor Editor { get; private set; }

        private ItemType selectItem;
        protected ItemType SelectItem
        {
            get => selectItem;
            set
            {
                if (selectItem != null)
                    selectItem.IsSelected = false;

                selectItem = value;

                if (selectItem != null)
                    selectItem.IsSelected = true;

                OnSelectClick?.Invoke(selectItem?.EditObject);
            }
        }
        public ObjectType SelectedObject => SelectItem?.EditObject;

        protected ItemType HoverItem { get; set; }
        public ObjectType HoverObject => HoverItem?.EditObject;
        public virtual bool IsEmpty => !components.Any(c => c is ItemType);

        #endregion

        public ItemsPanel()
        {
            autoLayout = AutoLayout.Vertical;
            autoChildrenHorizontally = AutoLayoutChildren.Fill;
            scrollOrientation = UIOrientation.Vertical;

            Scrollbar.DefaultStyle();
            scrollbarSize = 12f;
        }
        public void Init(Editor editor)
        {
            Editor = editor;
        }

        #region ADD OBJECT

        public void SetObjects(IEnumerable<ObjectType> editObjects)
        {
            PauseLayout(() =>
            {
                Clear();

                var objects = editObjects.OrderBy(o => o, this).ToArray();
                foreach (var editObject in objects)
                    AddObjectImpl(editObject, -1);
            });
        }
        public void AddObject(ObjectType editObject) => AddObjectImpl(editObject);
        protected virtual ItemType AddObjectImpl(ObjectType editObject)
        {
            var index = FindIndex(editObject);
            return AddObjectImpl(editObject, index >= 0 ? index : ~index);
        }
        protected virtual ItemType AddObjectImpl(ObjectType editObject, int zOrder) => GetItem(editObject, false, this, zOrder);
        protected ItemType GetItem<ParentType>(ObjectType editObject, bool inGroup, ParentType container, int zOrder = -1)
            where ParentType : UIComponent, IAutoLayoutPanel
        {
            var item = ComponentPool.Get<ItemType>(container, zOrder: zOrder);
            item.Init(Editor, editObject, inGroup);
            item.width = container.ItemSize.x;
            item.eventClick += ItemClick;
            item.OnDelete += ItemDelete;
            item.eventMouseEnter += ItemHover;
            item.eventMouseLeave += ItemLeave;

            return item;
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
            var index = Math.Min(components.IndexOf(item), components.Count - 2);
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

            var components = this.components.ToArray();
            foreach (var component in components)
            {
                if (component == Scrollbar)
                    continue;
                else if (component is ItemType item)
                    DeleteItem(item);
                else
                    ComponentPool.Free(component);
            }
        }

        #endregion

        #region EDIT UPDATE SELECT

        public void EditObject(ObjectType editObject)
        {
            ItemType item = null;
            if (editObject != null)
            {
                item = FindItem(editObject);
                if (item == null)
                    PauseLayout(() => item = AddObjectImpl(editObject));
            }
            else
                item = FindItem(0);

            Select(item);
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
            ScrollIntoView(item);
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
        private void ItemDelete(EditItem<ObjectType> item) => OnDeleteClick?.Invoke(item.EditObject);
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

        protected virtual ItemType FindItem(ObjectType editObject) => components.OfType<ItemType>().FirstOrDefault(c => ReferenceEquals(c.EditObject, editObject));
        protected virtual ItemType FindItem(int index) => FindItem<ItemType>(index, this);
        protected T FindItem<T>(int index, UIComponent parent) where T : UIComponent => index >= 0 ? parent.components.OfType<T>().Skip(index).FirstOrDefault() : null;

        protected virtual int FindIndex(ObjectType editObject) => FindIndex(editObject, this);
        protected int FindIndex(ObjectType editObject, UIComponent parent) => Array.BinarySearch(parent.components.OfType<ItemType>().Select(i => i.EditObject).ToArray(), editObject, this);

        public void RefreshSelectedItem() => SelectItem?.Refresh();
        public virtual void RefreshItems()
        {
            foreach (var item in components.OfType<ItemType>())
                item.Refresh();
        }
        public abstract int Compare(ObjectType x, ObjectType y);

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            ShowScroll = width >= 100f;
        }

        #endregion
    }
}
