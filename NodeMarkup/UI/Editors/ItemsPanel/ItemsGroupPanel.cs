using ColossalFramework.UI;
using ModsCommon.UI;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NodeMarkup.UI.Editors
{
    public abstract class ItemsGroupPanel<ItemType, ObjectType, GroupItemType, GroupType> : ItemsPanel<ItemType, ObjectType>, IComparer<GroupType>
        where ItemType : EditItem<ObjectType>
        where ObjectType : class, IDeletable
        where GroupItemType : EditGroup<GroupType, ItemType, ObjectType>
    {
        public override bool IsEmpty => GroupingEnable ? !Content.components.Any(c => c is GroupItemType) : base.IsEmpty;
        public abstract bool GroupingEnable { get; }
        private Dictionary<GroupType, GroupItemType> Groups { get; } = new Dictionary<GroupType, GroupItemType>();
        protected GroupItemType HoverGroup { get; set; }
        public GroupType HoverGroupObject => HoverGroup != null ? HoverGroup.Selector : default;

        protected override ItemType AddObjectImpl(ObjectType editObject)
        {
            var group = GetGroup(editObject);
            var index = FindIndex(editObject, group);
            return AddObjectImpl(editObject, (index >= 0 ? index : ~index) + 1);
        }
        protected override ItemType AddObjectImpl(ObjectType editObject, int zOrder)
        {
            if (GroupingEnable)
            {
                var group = GetGroup(editObject);
                var item = GetItem(group, zOrder);
                InitItem(item, editObject);
                item.isVisible = group.IsExpand;
                return item;
            }
            else
                return base.AddObjectImpl(editObject, zOrder);
        }
        protected override void DeleteSelectedItem(ItemType item)
        {
            var group = SelectGroup(item.Object);
            var index = Math.Min(item.parent.components.IndexOf(item), item.parent.components.Count - 2);
            DeleteItem(item);
            Select(FindItem(index, group));
        }
        protected override void DeleteItem(ItemType item)
        {
            var group = GetGroup(item.Object, false);

            base.DeleteItem(item);

            if (group?.IsEmpty == true)
            {
                group.Item.eventMouseEnter -= GroupHover;
                group.Item.eventMouseLeave -= GroupLeave;
                Groups.Remove(group.Selector);
                ComponentPool.Free(group);
            }
        }
        public override void Select(ItemType item)
        {
            if (GroupingEnable)
            {
                if (item == null)
                    return;

                var group = SelectGroup(item.Object);
                Groups[group].IsExpand = true;
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
        protected abstract string GroupName(GroupType group);

        private GroupItemType GetGroup(ObjectType editObject, bool add = true)
        {
            var groupType = SelectGroup(editObject);
            if (!Groups.TryGetValue(groupType, out GroupItemType group) && add)
            {
                var index = FindIndex(groupType, Content);
                group = ComponentPool.Get<GroupItemType>(Content, zOrder: index >= 0 ? index : ~index);
                group.Init(groupType, GroupName(groupType));
                group.width = Content.width;
                group.Item.eventMouseEnter += GroupHover;
                group.Item.eventMouseLeave += GroupLeave;
                Groups[groupType] = group;
            }
            return group;
        }

        private void GroupHover(UIComponent component, UIMouseEventParameter eventParam) => HoverGroup = component.parent as GroupItemType;
        private void GroupLeave(UIComponent component, UIMouseEventParameter eventParam) => HoverGroup = null;

        #endregion

        #region ADDITIONAL

        protected override ItemType FindItem(ObjectType editObject)
        {
            if (GroupingEnable)
            {
                var groupKey = SelectGroup(editObject);
                if (Groups.TryGetValue(groupKey, out GroupItemType group))
                    return group.components.OfType<ItemType>().FirstOrDefault(c => ReferenceEquals(c.Object, editObject));
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
                foreach (var group in Content.components.OfType<GroupItemType>())
                    group.Refresh();
            }
            else
                base.RefreshItems();
        }

        public abstract int Compare(GroupType x, GroupType y);

        #endregion
    }
}
