using ColossalFramework.UI;
using ModsCommon.UI;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup.UI.Editors
{
    public abstract class GroupedEditor<EditableItemType, EditableObject, ItemIcon, EditableGroupType, GroupType> : Editor<EditableItemType, EditableObject, ItemIcon>
        where EditableItemType : EditableItem<EditableObject, ItemIcon>
        where ItemIcon : UIComponent
        where EditableObject : class, IDeletable
        where EditableGroupType : EditableGroup<GroupType, EditableItemType, EditableObject, ItemIcon>
    {
        private Dictionary<GroupType, EditableGroupType> Groups { get; } = new Dictionary<GroupType, EditableGroupType>();
        protected abstract bool GroupingEnabled { get; }

        public override EditableItemType AddItem(EditableObject editableObject)
        {
            EditableItemType item;

            if (GroupingEnabled)
            {
                var group = GetGroup(editableObject);
                item = GetItem(group);
                item.isVisible = group.IsExpand;
            }
            else
                item = GetItem(ItemsPanel);

            InitItem(item, editableObject);

            SwitchEmpty();

            return item;
        }
        protected override void DeleteItem(EditableItemType item)
        {
            var group = GetGroup(item.Object, false);

            base.DeleteItem(item);

            if (group?.IsEmpty == true)
                DeleteGroup(group);
        }
        protected override void OnDeleteItem(EditableItemType item)
        {
            OnObjectDelete(item.Object);
            var isSelect = item == SelectItem;
            var index = Math.Min(item.parent.components.IndexOf(item), item.parent.components.Count - 2);
            var group = SelectGroup(item.Object);
            DeleteItem(item);

            if (!isSelect)
                return;

            SelectItem = null;
            ClearSettings();

            if (GroupingEnabled)
                Select(group, index);
            else
                Select(index);
        }
        private void DeleteGroup(EditableGroupType group)
        {
            Groups.Remove(group.Selector);
            DeleteUIComponent(group);
        }
        private EditableGroupType AddGroup(GroupType groupType)
        {
            var group = ItemsPanel.AddUIComponent<EditableGroupType>();
            group.Init(groupType, GroupName(groupType));
            group.width = ItemsPanel.width;
            Groups[groupType] = group;
            return group;
        }

        private EditableGroupType GetGroup(EditableObject editableObject, bool add = true)
        {
            var groupType = SelectGroup(editableObject);
            if (!Groups.TryGetValue(groupType, out EditableGroupType group) && add)
                group = AddGroup(groupType);
            return group;
        }
        protected override void ClearItems()
        {
            var components = ItemsPanel.components.ToArray();
            foreach (var component in components)
            {
                if (component is EditableItemType item)
                    DeleteItem(item);
                else if (component is EditableGroupType group)
                    ClearItems(group);
                else
                    DeleteUIComponent(component);
            }

            Groups.Clear();
        }

        protected abstract GroupType SelectGroup(EditableObject editableItem);
        protected abstract string GroupName(GroupType group);

        protected override void RefreshItems()
        {
            if (GroupingEnabled)
            {
                foreach (var group in ItemsPanel.components.OfType<EditableGroupType>())
                    group.Refresh();
            }
            else
                base.RefreshItems();
        }
        protected override EditableItemType GetItem(EditableObject editObject)
        {
            if (GroupingEnabled)
            {
                var groupKey = SelectGroup(editObject);
                if (Groups.TryGetValue(groupKey, out EditableGroupType group))
                    return group.components.OfType<EditableItemType>().FirstOrDefault(c => ReferenceEquals(c.Object, editObject));
                else
                    return null;
            }
            else
                return base.GetItem(editObject);
        }
        public override void Edit(EditableObject selectObject = null)
        {
            var expandedGroups = Groups.Where(i => i.Value.IsExpand).Select(i => i.Key).ToArray();

            base.Edit(selectObject);

            foreach (var expandedGroup in expandedGroups)
            {
                if (Groups.TryGetValue(expandedGroup, out EditableGroupType group))
                    group.IsExpand = true;
            }

            if (selectObject != null && GetItem(selectObject) is EditableItemType item)
                ScrollTo(item);
        }
        private void Select(GroupType groupType, int index)
        {
            if (Groups.TryGetValue(groupType, out EditableGroupType group))
                Select(group, index);
        }
        public override void Select(EditableItemType item)
        {
            var groupKey = SelectGroup(item.Object);
            if (Groups.TryGetValue(groupKey, out EditableGroupType group))
                group.IsExpand = true;

            base.Select(item);
        }

        public override void ScrollTo(EditableItemType item)
        {
            ItemsPanel.ScrollToBottom();
            ItemsPanel.ScrollIntoViewRecursive(item);
        }
    }
}
