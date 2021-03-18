using ColossalFramework.UI;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup.UI.Editors.Base
{
    public abstract class ItemsGroupPanel<ItemType, ObjectType, ItemIcon, GroupItemType, GroupType> : ItemsPanel<ItemType, ObjectType, ItemIcon>
        where ItemType : EditItem<ObjectType, ItemIcon>
        where ItemIcon : UIComponent
        where ObjectType : class, IDeletable
    {
        public bool GroupingEnabled { get; }
        private Dictionary<GroupType, GroupItemType> Groups { get; } = new Dictionary<GroupType, GroupItemType>();

        protected override ItemType AddObjectImpl(ObjectType editObject, int zOrder)
        {
            return base.AddObjectImpl(editObject, zOrder);
        }
    }
}
