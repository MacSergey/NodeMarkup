using ColossalFramework.UI;
using System.Collections.Generic;
using System.Linq;

namespace NodeMarkup.UI.Editors
{
    public abstract class GroupedEditor<EditableItemType, EditableObject, ItemIcon, EditableGroupType, GroupType> : Editor<EditableItemType, EditableObject, ItemIcon>
        where EditableItemType : EditableItem<EditableObject, ItemIcon>
        where ItemIcon : UIComponent
        where EditableObject : class
        where EditableGroupType : EditableGroup<GroupType, EditableItemType, EditableObject, ItemIcon>
    {
        private Dictionary<GroupType, EditableGroupType> Groups { get; } = new Dictionary<GroupType, EditableGroupType>();
        protected abstract bool GroupingEnabled { get; }

        public override EditableItemType AddItem(EditableObject editableObject)
        {
            var item = GroupingEnabled ? GetGroup(editableObject).NewItem() : NewItem();
            InitItem(item, editableObject);

            SwitchEmpty();

            return item;
        }
        protected override void DeleteItem(EditableItemType item)
        {
            DeInitItem(item);

            if (GroupingEnabled)
            {
                var group = GetGroup(item.Object);
                group.DeleteItem(item);

                if (group.IsEmpty)
                    DeleteGroup(group);
            }
            else
                DeleteUIComponent(item);

            SwitchEmpty();
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

        private EditableGroupType GetGroup(EditableObject editableObject)
        {
            var groupType = SelectGroup(editableObject);
            if (!Groups.TryGetValue(groupType, out EditableGroupType group))
                group = AddGroup(groupType);
            return group;
        }
        protected override void ClearItems()
        {
            base.ClearItems();
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
        public override void UpdateEditor(EditableObject selectObject = null)
        {
            var expandedGroups = Groups.Where(i => i.Value.IsExpand).Select(i => i.Key).ToArray();

            base.UpdateEditor(selectObject);

            foreach (var expandedGroup in expandedGroups)
            {
                if (Groups.TryGetValue(expandedGroup, out EditableGroupType group))
                    group.IsExpand = true;
            }

            if (selectObject != null && GetItem(selectObject) is EditableItemType item)
                ScrollTo(item);
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
