using ColossalFramework.UI;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NodeMarkup.UI
{
    public abstract class SelectAssetProperty<PrefabType> : EditorItem, IReusable
        where PrefabType : PrefabInfo
    {
        public event Action<PrefabType> OnValueChanged;
        bool IReusable.InCache { get; set; }
        public override bool SupportEven => true;

        public abstract PrefabType Prefab { get; set; }

        public override void DeInit()
        {
            base.DeInit();
            OnValueChanged = null;
        }
        protected virtual void OnSelectObjectChanged(PrefabType prefab)
        {
            OnValueChanged?.Invoke(prefab);
        }
    }
    public abstract class SelectAssetProperty<PrefabType, DropDownType> : SelectAssetProperty<PrefabType>
        where PrefabType : PrefabInfo
        where DropDownType : UIDropDown<PrefabType>
    {
        protected override float DefaultHeight => 100f;

        private CustomUIPanel Screenshot { get; set; }
        private DropDownType DropDown { get; set; }

        public override PrefabType Prefab
        {
            get => DropDown.SelectedObject;
            set
            {
                DropDown.SelectedObject = value;
                SetScreenshot();
            }
        }

        public SelectAssetProperty()
        {
            autoLayout = true;
            autoLayoutPadding = new RectOffset(5, 5, 5, 5);

            AddScreenshot();
            AddDropdown();
        }
        private void AddScreenshot()
        {
            Screenshot = AddUIComponent<CustomUIPanel>();
            Screenshot.size = new Vector2(90f, 90f);
            Screenshot.relativePosition = new Vector2(ItemsPadding, 5);
        }
        private void AddDropdown()
        {
            DropDown = AddUIComponent<DropDownType>();
            DropDown.UseWheel = true;
            DropDown.UseScrollBar = true;
            DropDown.SetDefaultStyle();
            DropDown.OnSelectObjectChanged += OnSelectObjectChanged;
            DropDown.eventDropdownOpen += DropDownOpen;
            DropDown.eventDropdownClose += DropDownClose;
        }

        public override void Init() => Init(null);
        public void Init(Func<PrefabType, bool> selector = null)
        {
            base.Init(null);
            FillItems(selector);
        }
        public override void DeInit()
        {
            base.DeInit();
            DropDown.Clear();
        }

        public void FillItems(Func<PrefabType, bool> selector)
        {
            DropDown.Clear();
            DropDown.AddItem(null, NodeMarkup.Localize.SelectPanel_NotSet);
            for (uint i = 0; i < PrefabCollection<PrefabType>.LoadedCount(); i += 1)
            {
                if (PrefabCollection<PrefabType>.GetLoaded(i) is PrefabType prefab && selector?.Invoke(prefab) != false)
                    DropDown.AddItem(prefab, GetLocalizedTitle(prefab));
            }
        }
        protected abstract string GetLocalizedTitle(PrefabType prefab);

        protected override void OnSelectObjectChanged(PrefabType prefab)
        {
            base.OnSelectObjectChanged(prefab);
            SetScreenshot();
        }
        private void DropDownOpen(UIDropDown dropdown, UIListBox popup, ref bool overridden)
        {
            dropdown.triggerButton.isInteractive = false;
        }
        private void DropDownClose(UIDropDown dropdown, UIListBox popup, ref bool overridden)
        {
            dropdown.triggerButton.isInteractive = true;
        }
        private void SetScreenshot()
        {
            if (Prefab is PrefabType prefab)
            {
                Screenshot.atlas = prefab.m_Atlas;
                Screenshot.backgroundSprite = prefab.m_Thumbnail;
            }
            else
            {
                Screenshot.atlas = null;
                Screenshot.backgroundSprite = string.Empty;
            }
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            SetPosition();
        }
        private void SetPosition()
        {
            if (Screenshot != null && DropDown != null)
            {
                DropDown.size = new Vector2(width - Screenshot.width - autoLayoutPadding.horizontal * 2f, 20f);
            }
        }
    }

    public class SelectPropProperty : SelectAssetProperty<PropInfo, SelectPropProperty.PropDropDown>
    {
        protected override string GetLocalizedTitle(PropInfo prefab)
        {
            if (ColossalFramework.Globalization.Locale.Exists("PROPS_TITLE", prefab.name))
                return ColossalFramework.Globalization.Locale.Get("PROPS_TITLE", prefab.name);
            else
                return prefab.name;
        }
        public class PropDropDown : UIDropDown<PropInfo> { }
    }
    public class SelectTreeProperty : SelectAssetProperty<TreeInfo, SelectTreeProperty.TreeDropDown>
    {
        protected override string GetLocalizedTitle(TreeInfo prefab)
        {
            if (ColossalFramework.Globalization.Locale.Exists("TREE_TITLE", prefab.name))
                return ColossalFramework.Globalization.Locale.Get("TREE_TITLE", prefab.name);
            else
                return prefab.name;
        }
        public class TreeDropDown : UIDropDown<TreeInfo> { }
    }
    public class SelectNetworkProperty : SelectAssetProperty<NetInfo, SelectNetworkProperty.NetDropDown>
    {
        protected override string GetLocalizedTitle(NetInfo prefab)
        {
            if (ColossalFramework.Globalization.Locale.Exists("NET_TITLE", prefab.name))
                return ColossalFramework.Globalization.Locale.Get("NET_TITLE", prefab.name);
            else
                return prefab.name;
        }
        public class NetDropDown : UIDropDown<NetInfo> { }
    }
}
