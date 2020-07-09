using ColossalFramework.UI;
using NodeMarkup.Manager;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public abstract class Editor : UIPanel
    {
        public NodeMarkupPanel NodeMarkupPanel { get; private set; }
        protected Markup Markup => NodeMarkupPanel.Markup;

        protected UIScrollablePanel ItemsPanel { get; set; }
        protected UIScrollbar ItemsScrollbar { get; set; }
        protected UIScrollablePanel SettingsPanel { get; set; }
        protected UIScrollbar SettingsScrollbar { get; set; }

        public abstract string Name { get; }

        public Editor()
        {
            autoLayout = true;
            autoLayoutDirection = LayoutDirection.Horizontal;
            clipChildren = true;
            atlas = NodeMarkupPanel.InGameAtlas;
            backgroundSprite = "UnlockingItemBackground";

            AddItemsPanel();
            AddSettingPanel();
        }
        private void AddItemsPanel()
        {
            ItemsPanel = AddUIComponent<UIScrollablePanel>();
            ItemsPanel.autoLayout = true;
            ItemsPanel.autoLayoutDirection = LayoutDirection.Vertical;
            ItemsPanel.autoLayoutPadding = new RectOffset(0, 0, 0, 0);
            ItemsPanel.scrollWheelDirection = UIOrientation.Vertical;
            ItemsPanel.builtinKeyNavigation = true;
            ItemsPanel.clipChildren = true;
            ItemsPanel.eventSizeChanged += ItemsPanelSizeChanged;
            ItemsPanel.atlas = NodeMarkupPanel.InGameAtlas;
            ItemsPanel.backgroundSprite = "ScrollbarTrack";

            ItemsScrollbar = AddScrollbar();
            ItemsPanel.verticalScrollbar = ItemsScrollbar;

            ItemsScrollbar.eventVisibilityChanged += ItemsScrollbarVisibilityChanged;
        }

        private void ItemsPanelSizeChanged(UIComponent component, Vector2 value)
        {
            foreach (var item in ItemsPanel.components)
            {
                item.width = ItemsPanel.width;
            }
        }
        private void ItemsScrollbarVisibilityChanged(UIComponent component, bool value)
        {
            ItemsPanel.width = size.x / 10 * 3 - (ItemsScrollbar.isVisible ? ItemsScrollbar.width : 0);
        }

        private void AddSettingPanel()
        {
            SettingsPanel = AddUIComponent<UIScrollablePanel>();
            SettingsPanel.autoLayout = true;
            SettingsPanel.autoLayoutDirection = LayoutDirection.Vertical;
            SettingsPanel.autoLayoutPadding = new RectOffset(10, 10, 10, 10);
            SettingsPanel.scrollWheelDirection = UIOrientation.Vertical;
            SettingsPanel.builtinKeyNavigation = true;
            SettingsPanel.clipChildren = true;
            SettingsPanel.atlas = NodeMarkupPanel.InGameAtlas;
            SettingsPanel.backgroundSprite = "UnlockingItemBackground";
            SettingsPanel.eventSizeChanged += SettingsPanelSizeChanged;

            SettingsScrollbar = AddScrollbar();
            SettingsPanel.verticalScrollbar = SettingsScrollbar;

            SettingsScrollbar.eventVisibilityChanged += SettingsScrollbarVisibilityChanged;
        }
        private void SettingsPanelSizeChanged(UIComponent component, Vector2 value)
        {
            foreach (var item in SettingsPanel.components)
            {
                item.width = SettingsPanel.width - SettingsPanel.autoLayoutPadding.horizontal;
            }
        }
        private void SettingsScrollbarVisibilityChanged(UIComponent component, bool value)
        {
            SettingsPanel.width = size.x / 10 * 7 - (SettingsScrollbar.isVisible ? SettingsScrollbar.width : 0);
        }

        private UIScrollbar AddScrollbar()
        {
            var scrollbar = AddUIComponent<UIScrollbar>();
            scrollbar.orientation = UIOrientation.Vertical;
            scrollbar.pivot = UIPivotPoint.TopLeft;
            scrollbar.minValue = 0;
            scrollbar.value = 0;
            scrollbar.incrementAmount = 50;
            scrollbar.autoHide = true;
            scrollbar.width = 10;

            UISlicedSprite trackSprite = scrollbar.AddUIComponent<UISlicedSprite>();
            trackSprite.relativePosition = Vector2.zero;
            trackSprite.autoSize = true;
            trackSprite.anchor = UIAnchorStyle.All;
            trackSprite.size = trackSprite.parent.size;
            trackSprite.fillDirection = UIFillDirection.Vertical;
            trackSprite.spriteName = "ScrollbarTrack";
            scrollbar.trackObject = trackSprite;

            UISlicedSprite thumbSprite = trackSprite.AddUIComponent<UISlicedSprite>();
            thumbSprite.relativePosition = Vector2.zero;
            thumbSprite.fillDirection = UIFillDirection.Vertical;
            thumbSprite.autoSize = true;
            thumbSprite.width = thumbSprite.parent.width;
            thumbSprite.spriteName = "ScrollbarThumb";
            scrollbar.thumbObject = thumbSprite;

            scrollbar.eventValueChanged += (component, value) => ItemsPanel.scrollPosition = new Vector2(0, value);

            eventMouseWheel += (component, eventParam) =>
            {
                scrollbar.value -= (int)eventParam.wheelDelta * scrollbar.incrementAmount;
            };

            ItemsPanel.eventMouseWheel += (component, eventParam) =>
            {
                scrollbar.value -= (int)eventParam.wheelDelta * scrollbar.incrementAmount;
            };

            return scrollbar;
        }

        public virtual void Init(NodeMarkupPanel panel)
        {
            NodeMarkupPanel = panel;
        }

        public virtual void UpdateEditor()
        {
            Logger.LogDebug($"{nameof(Editor)}.{nameof(UpdateEditor)}");

            ClearItems();
            if (Markup != null)
            {
                FillItems();
            }
            ClearSettings();
            Select(0);
        }
        public void ClearEditor()
        {
            Logger.LogDebug($"{nameof(Editor)}.{nameof(ClearEditor)}");
            ClearItems();
            ClearSettings();
        }
        protected virtual void RefreshItems() { }
        protected virtual void ClearItems() { }
        protected virtual void ClearSettings() { }
        protected virtual void FillItems() { }
        public virtual void Select(int index) { }
        public virtual void Render(RenderManager.CameraInfo cameraInfo) { }
        public virtual string GetInfo() => string.Empty;
        public virtual void OnUpdate() { }
        public virtual void OnEvent(Event e) { }
        public virtual void OnPrimaryMouseClicked(Event e, out bool isDone)
        {
            isDone = true;
            NodeMarkupPanel.EndEditorAction();
        }
        public virtual void OnSecondaryMouseClicked(out bool isDone)
        {
            isDone = true;
            NodeMarkupPanel.EndEditorAction();
        }
        public virtual void EndEditorAction() { }

        protected abstract void ItemClick(UIComponent component, UIMouseEventParameter eventParam);
        protected abstract void ItemHover(UIComponent component, UIMouseEventParameter eventParam);
        protected abstract void ItemLeave(UIComponent component, UIMouseEventParameter eventParam);

        public void StopScroll() => SettingsPanel.scrollWheelDirection = UIOrientation.Horizontal;
        public void StartScroll() => SettingsPanel.scrollWheelDirection = UIOrientation.Vertical;
    }
    public abstract class Editor<EditableItemType, EditableObject, ItemIcon> : Editor
        where EditableItemType : EditableItem<EditableObject, ItemIcon>
        where ItemIcon : UIComponent
    {
        EditableItemType _selectItem;

        protected EditableItemType HoverItem { get; set; }
        protected bool IsHoverItem => HoverItem != null;
        protected EditableItemType SelectItem
        {
            get => _selectItem;
            private set
            {
                if (_selectItem != null)
                    _selectItem.Unselect();

                _selectItem = value;
                _selectItem.Select();
            }
        }
        protected EditableObject EditObject => SelectItem.Object;


        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            ItemsPanel.width = size.x / 10 * 3 - (ItemsScrollbar.isVisible ? ItemsScrollbar.width : 0);
            SettingsPanel.width = size.x / 10 * 7 - (SettingsScrollbar.isVisible ? SettingsScrollbar.width : 0);
            ItemsPanel.height = size.y;
            SettingsPanel.height = size.y;
            ItemsScrollbar.height = size.y;
            SettingsScrollbar.height = size.y;
        }

        public EditableItemType AddItem(EditableObject editableObject)
        {
#if STOPWATCH
            var sw = Stopwatch.StartNew();
#endif
            var item = ItemsPanel.AddUIComponent<EditableItemType>();
            item.name = editableObject.ToString();
            item.width = ItemsPanel.width;
            item.Object = editableObject;
            item.eventClick += ItemClick;
            item.eventMouseEnter += ItemHover;
            item.eventMouseLeave += ItemLeave;
            item.OnDelete += ItemDelete;
#if STOPWATCH
            Logger.LogDebug($"{nameof(TemplateEditor)}.{nameof(Editor)}: {sw.ElapsedMilliseconds}ms");
#endif
            return item;
        }

        private void ItemDelete(EditableItem<EditableObject, ItemIcon> deleteItem)
        {
            if (!(deleteItem is EditableItemType item))
                return;

            if (Settings.DeleteWarnings)
            {
                var messageBox = MessageBox.ShowModal<YesNoMessageBox>();
                messageBox.CaprionText = string.Format(NodeMarkup.Localize.Editor_DeleteCaption, item.Description);
                messageBox.MessageText = string.Format(NodeMarkup.Localize.Editor_DeleteMessage, item.Description, item.Object);
                messageBox.OnButton1Click = Delete;
            }
            else
                Delete();

            bool Delete()
            {
                OnObjectDelete(item.Object);
                var isSelect = item == SelectItem;
                DeleteItem(item);
                if (isSelect)
                {
                    ClearSettings();
                    Select(0);
                }
                return true;
            }
        }

        protected override void ClearItems()
        {
            var componets = ItemsPanel.components.ToArray();
            foreach (EditableItemType item in componets)
            {
                DeleteItem(item);
            }
        }
        private void DeleteItem(EditableItemType item)
        {
            item.eventClick -= ItemClick;
            item.eventMouseEnter -= ItemHover;
            item.eventMouseLeave -= ItemLeave;
            ItemsPanel.RemoveUIComponent(item);
            Destroy(item.gameObject);
        }
        protected override void RefreshItems()
        {
            foreach (EditableItemType item in ItemsPanel.components)
            {
                item.Refresh();
            }
        }
        protected override void ClearSettings()
        {
            var componets = SettingsPanel.components.ToArray();
            foreach (var item in componets)
            {
                SettingsPanel.RemoveUIComponent(item);
                Destroy(item.gameObject);
            }
        }

        protected override void ItemClick(UIComponent component, UIMouseEventParameter eventParam) => ItemClick((EditableItemType)component);
        protected virtual void ItemClick(EditableItemType item)
        {
            ClearSettings();
            SelectItem = item;
            OnObjectSelect();
        }
        protected override void ItemHover(UIComponent component, UIMouseEventParameter eventParam) => HoverItem = component as EditableItemType;
        protected override void ItemLeave(UIComponent component, UIMouseEventParameter eventParam) => HoverItem = null;
        protected virtual void OnObjectSelect()
        {

        }
        protected virtual void OnObjectDelete(EditableObject editableObject)
        {

        }
        protected override void OnVisibilityChanged()
        {
            if (isVisible)
                Select(0);
        }
        public override void Select(int index)
        {
            if (ItemsPanel.components.Count > index && ItemsPanel.components[index] is EditableItemType item)
                Select(item);
        }
        public void Select(EditableItemType item)
        {
            item.SimulateClick();
            item.Focus();
            ItemsPanel.ScrollToBottom();
            ItemsPanel.ScrollIntoView(item);
        }
        public void Select(EditableObject editableObject)
        {
            if (ItemsPanel.components.OfType<EditableItemType>().FirstOrDefault(c => System.Object.ReferenceEquals(c.Object, editableObject)) is EditableItemType item)
                Select(item);
        }
    }
}
