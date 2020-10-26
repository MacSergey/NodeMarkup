using ColossalFramework.UI;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public abstract class ListPropertyPanel<Type, UISelector> : EditorPropertyPanel, IReusable
        where UISelector : UIComponent, IUISelector<Type>
    {
        public event Action<Type> OnSelectObjectChanged;
        public event Action<bool> OnDropDownStateChange;

        protected UISelector Selector { get; set; }

        protected virtual float DropDownWidth => 230;
        protected virtual bool AllowNull => true;
        public string NullText { get; set; } = string.Empty;
        public bool IsOpen { get; private set; } = false;

        public Type SelectedObject
        {
            get => Selector.SelectedObject;
            set => Selector.SelectedObject = value;
        }

        public ListPropertyPanel()
        {
            AddDropDown();
            Selector.IsEqualDelegate = IsEqual;
        }
        private void AddDropDown()
        {
            Selector = Control.AddUIComponent<UISelector>();

            Selector.SetDefaultStyle(new Vector2(DropDownWidth, 20));
            Selector.OnSelectObjectChanged += DropDownValueChanged;
            Selector.eventSizeChanged += SelectorSizeChanged;
            if (Selector is UIDropDown dropDown)
            {
                dropDown.eventDropdownOpen += DropDownOpen;
                dropDown.eventDropdownClose += DropDownClose;
            }
        }
        private void SelectorSizeChanged(UIComponent component, Vector2 value) => RefreshContent();

        private void DropDownValueChanged(Type value) => OnSelectObjectChanged?.Invoke(value);
        private void DropDownClose(UIDropDown dropdown, UIListBox popup, ref bool overridden)
        {
            IsOpen = false;
            OnDropDownStateChange?.Invoke(false);
        }

        private void DropDownOpen(UIDropDown dropdown, UIListBox popup, ref bool overridden)
        {
            IsOpen = true;
            OnDropDownStateChange?.Invoke(true);
        }

        public override void Init()
        {
            base.Init();
            Selector.Clear();

            if (AllowNull)
                Selector.AddItem(default, NullText ?? string.Empty);
        }
        public override void DeInit()
        {
            base.DeInit();

            OnSelectObjectChanged = null;
            OnDropDownStateChange = null;

            Selector.Clear();
        }
        public void Add(Type item) => Selector.AddItem(item);
        public void AddRange(IEnumerable<Type> items)
        {
            foreach (var item in items)
                Selector.AddItem(item);
        }
        protected abstract bool IsEqual(Type first, Type second);
    }
}
