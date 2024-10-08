﻿using ColossalFramework.UI;
using IMT.Manager;
using ModsCommon.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IMT.UI.Editors
{
    public interface IGroupItemPanel
    {
        bool GroupingEnable { get; }
    }
    public abstract class ItemsGroupPanel<ItemType, ObjectType, GroupItemType, GroupType> : ItemsPanel<ItemType, ObjectType>, IGroupItemPanel, IComparer<GroupType>
        where ItemType : EditItem<ObjectType>
        where ObjectType : class, IDeletable
        where GroupItemType : EditGroup<GroupType, ItemType, ObjectType>
    {
        public override bool IsEmpty => GroupingEnable ? !components.Any(c => c is GroupItemType) : base.IsEmpty;
        public abstract bool GroupingEnable { get; }
        private Dictionary<GroupType, GroupItemType> Groups { get; } = new Dictionary<GroupType, GroupItemType>();
        protected GroupItemType HoverGroup { get; set; }
        public GroupType HoverGroupObject => HoverGroup != null ? HoverGroup.Selector : default;

        protected override ItemType AddObjectImpl(ObjectType editObject)
        {
            if (GroupingEnable)
            {
                var group = GetGroup(editObject);
                var index = FindIndex(editObject, group);
                return AddObjectImpl(editObject, (index >= 0 ? index : ~index) + 1);
            }
            else
                return base.AddObjectImpl(editObject);
        }
        protected override ItemType AddObjectImpl(ObjectType editObject, int zOrder)
        {
            if (GroupingEnable)
            {
                var group = GetGroup(editObject);
                var item = GetItem(editObject, true, group, zOrder);
                item.isVisible = group.IsExpand;
                return item;
            }
            else
                return base.AddObjectImpl(editObject, zOrder);
        }
        protected override void DeleteSelectedItem(ItemType item)
        {
            var group = SelectGroup(item.EditObject);
            var index = Math.Min(item.parent.components.IndexOf(item), item.parent.components.Count - 2);
            DeleteItem(item);
            Select(FindItem(index, group));
        }
        protected override void DeleteItem(ItemType item)
        {
            var group = GetGroup(item.EditObject, false);

            base.DeleteItem(item);

            if (group?.IsEmpty == true)
            {
                group.Header.eventMouseEnter -= GroupHover;
                group.Header.eventMouseLeave -= GroupLeave;
                Groups.Remove(group.Selector);
                ComponentPool.Free(group);
            }
        }
        public override void Select(ItemType item)
        {
            if (item == null)
                return;

            if (GroupingEnable)
            {
                var group = SelectGroup(item.EditObject);
                var groupItem = Groups[group];
                groupItem.IsExpand = true;
                ScrollIntoView(groupItem);
            }

            base.Select(item);
        }

        public override void Clear()
        {
            base.Clear();
            Groups.Clear();
        }

        #region GROUP

        protected abstract GroupType SelectGroup(ObjectType editObject);
        private GroupItemType GetGroup(ObjectType editObject, bool add = true)
        {
            var groupType = SelectGroup(editObject);
            if (!Groups.TryGetValue(groupType, out GroupItemType group) && add)
            {
                var index = FindIndex(groupType, this);
                group = ComponentPool.Get<GroupItemType>(this, zOrder: index >= 0 ? index : ~index);
                group.Init(groupType);
                group.width = this.width;
                if (Editor is IItemsGroupingEditor<GroupType> groupingEditor && groupingEditor.GroupExpandList.TryGetValue(groupType, out var isExpand))
                    group.IsExpand = isExpand;
                group.Header.eventMouseEnter += GroupHover;
                group.Header.eventMouseLeave += GroupLeave;
                group.OnExpandChanged += GroupExpandChanged;

                if (IsLayoutSuspended)
                    group.StopLayout();

                Groups[groupType] = group;
            }
            return group;
        }

        private void GroupHover(UIComponent component, UIMouseEventParameter eventParam) => HoverGroup = component.parent as GroupItemType;
        private void GroupLeave(UIComponent component, UIMouseEventParameter eventParam) => HoverGroup = null;
        private void GroupExpandChanged(GroupType groupType, bool isExpand, bool applyToAll)
        {
            PauseLayout(() =>
            {
                if (applyToAll)
                {
                    foreach (var group in Groups.Values)
                        Set(groupType, isExpand);
                }
                else
                    Set(groupType, isExpand);
            });

            void Set(GroupType groupType, bool isExpand)
            {
                Groups[groupType].IsExpand = isExpand;
                if (Editor is IItemsGroupingEditor<GroupType> groupingEditor)
                    groupingEditor.GroupExpandList[groupType] = isExpand;
            }
        }

        #endregion

        #region ADDITIONAL

        protected override ItemType FindItem(int index)
        {
            if (GroupingEnable)
                return index >= 0 ? components.OfType<GroupItemType>().SelectMany(g => g.components).OfType<ItemType>().Skip(index).FirstOrDefault() : null;
            else
                return base.FindItem(index);
        }
        protected override ItemType FindItem(ObjectType editObject)
        {
            if (GroupingEnable)
            {
                var groupKey = SelectGroup(editObject);
                if (Groups.TryGetValue(groupKey, out GroupItemType group))
                    return group.components.OfType<ItemType>().FirstOrDefault(c => ReferenceEquals(c.EditObject, editObject));
                else
                    return null;
            }
            else
                return base.FindItem(editObject);
        }
        protected ItemType FindItem(int index, GroupType group)
        {
            if (Groups.TryGetValue(group, out GroupItemType groupItem))
                return FindItem<ItemType>(index, groupItem);
            else
                return null;
        }
        protected int FindIndex(GroupType group, UIComponent parent) => Array.BinarySearch(parent.components.OfType<GroupItemType>().Select(i => i.Selector).ToArray(), group, this);
        public override void RefreshItems()
        {
            if (GroupingEnable)
            {
                foreach (var group in components.OfType<GroupItemType>())
                    group.Refresh();
            }
            else
                base.RefreshItems();
        }

        public abstract int Compare(GroupType x, GroupType y);

        public override void PauseLayout(Action action, bool layoutNow = true, bool force = false)
        {
            foreach (var group in Groups.Values)
                group.StopLayout();

            base.PauseLayout(action, layoutNow, force);

            foreach (var group in Groups.Values)
                group.StartLayout(layoutNow, force);
        }
        public override void StopLayout()
        {
            foreach (var group in Groups.Values)
                group.StopLayout();

            base.StopLayout();
        }
        public override void StartLayout(bool layoutNow = true, bool force = false)
        {
            foreach (var group in Groups.Values)
                group.StartLayout(layoutNow, force);

            base.StartLayout(layoutNow, force);
        }

        #endregion
    }
}
