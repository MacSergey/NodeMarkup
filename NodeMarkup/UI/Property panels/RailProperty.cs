using ColossalFramework.UI;
using ICities;
using ModsCommon.UI;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static NetInfo;

namespace NodeMarkup.UI
{
    public class FillerRailPropertyPanel : EditorPropertyPanel, IReusable
    {
        bool IReusable.InCache { get; set; }

        public event Action<bool, FillerRail, FillerRail> OnValueChanged;
        public event Action<SelectRailButton> OnSelect;
        public event Action<SelectRailButton> OnEnter;
        public event Action<SelectRailButton> OnLeave;

        private BoolSegmented FollowRail { get; }
        protected SelectRailButton LeftRailSelector { get; set; }
        protected SelectRailButton RightRailSelector { get; set; }

        public FillerRail LeftRail
        {
            get => LeftRailSelector.Value;
            set
            {
                LeftRailSelector.Value = value;
                RightRailSelector.Other = value;
                OnValueChanged?.Invoke(FollowRail.SelectedObject, LeftRailSelector.Value, RightRail);
            }
        }
        public FillerRail RightRail
        {
            get => RightRailSelector.Value;
            set
            {
                RightRailSelector.Value = value;
                LeftRailSelector.Other = value;
                OnValueChanged?.Invoke(FollowRail.SelectedObject, LeftRail, RightRailSelector.Value);
            }
        }
        public bool? Follow
        {
            get => FollowRail.isVisible ? FollowRail.SelectedObject : null;
            set
            {
                if (value != null)
                {
                    FollowRail.isVisible = true;
                    FollowRail.SelectedObject = value.Value;
                }
                else
                    FollowRail.isVisible = false;

                Refresh();
            }
        }

        public FillerRailPropertyPanel()
        {
            FollowRail = Content.AddUIComponent<BoolSegmented>();
            FollowRail.StopLayout();
            FollowRail.AutoButtonSize = false;
            FollowRail.ButtonWidth = 25f;
            FollowRail.AddItem(true, "I");
            FollowRail.AddItem(false, "O");
            FollowRail.StartLayout();
            FollowRail.OnSelectObjectChanged += FollowChanged;

            LeftRailSelector = Content.AddUIComponent<SelectRailButton>();
            LeftRailSelector.width = 80f;
            LeftRailSelector.Button.eventClick += ButtonClick;
            LeftRailSelector.Button.eventMouseEnter += ButtonMouseEnter;
            LeftRailSelector.Button.eventMouseLeave += ButtonMouseLeave;

            RightRailSelector = Content.AddUIComponent<SelectRailButton>();
            RightRailSelector.width = 80f;
            RightRailSelector.Button.eventClick += ButtonClick;
            RightRailSelector.Button.eventMouseEnter += ButtonMouseEnter;
            RightRailSelector.Button.eventMouseLeave += ButtonMouseLeave;
        }
        public override void DeInit()
        {
            base.DeInit();

            OnSelect = null;
            OnEnter = null;
            OnLeave = null;
            OnValueChanged = null;
        }

        private void FollowChanged(bool value)
        {
            Refresh();
            OnValueChanged?.Invoke(value, LeftRail, RightRail);
        }

        private void Refresh()
        {
            LeftRailSelector.isEnabled = !FollowRail.isVisible || FollowRail.SelectedObject;
            RightRailSelector.isEnabled = !FollowRail.isVisible || FollowRail.SelectedObject;
        }

        protected virtual void ButtonClick(UIComponent component, UIMouseEventParameter eventParam) => OnSelect?.Invoke(component.parent as SelectRailButton);
        protected virtual void ButtonMouseEnter(UIComponent component, UIMouseEventParameter eventParam) => OnEnter?.Invoke(component.parent as SelectRailButton);
        protected virtual void ButtonMouseLeave(UIComponent component, UIMouseEventParameter eventParam) => OnLeave?.Invoke(component.parent as SelectRailButton);

        public class SelectRailButton : SelectItemPropertyButton<FillerRail>
        {
            protected override string NotSet => string.Empty;
            public FillerRail Other { get; set; }
        }
    }
}
