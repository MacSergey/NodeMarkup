using ColossalFramework.UI;
using NodeMarkup.Manager;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public abstract class Editor : UIPanel
    {
        public NodeMarkupPanel NodeMarkupPanel { get; set; }
        protected Markup Markup => NodeMarkupPanel.Markup;

        public abstract string Name { get; }
        protected UIScrollablePanel ItemsPanel { get; set; }
        protected UIScrollbar ItemsScrollbar { get; set; }
        protected UIScrollablePanel SettingsPanel { get; set; }
        protected UIScrollbar SettingsScrollbar { get; set; }

        public virtual void Init()
        {

        }

        public virtual void UpdateEditor()
        {
            ClearItems();
            FillItems();
        }
        protected void ClearItems()
        {
            var componets = ItemsPanel.components.ToArray();
            foreach (var item in componets)
            {
                item.eventClick -= ItemClick;
                ItemsPanel.RemoveUIComponent(item);
                Destroy(item.gameObject);
            }
        }
        protected void ClearSettings()
        {
            var componets = SettingsPanel.components.ToArray();
            foreach (var item in componets)
            {
                SettingsPanel.RemoveUIComponent(item);
                Destroy(item.gameObject);
            }
        }
        protected virtual void FillItems()
        {

        }
        public virtual void Select(int index)
        {

        }
        public virtual void Render()
        {

        }

        protected abstract void ItemClick(UIComponent component, UIMouseEventParameter eventParam);
    }
    public abstract class Editor<EditableItemType, EditableObject, ItemIcon> : Editor
        where EditableItemType : EditableItem<EditableObject, ItemIcon>
        where ItemIcon : UIComponent
    {
        protected EditableObject EditObject { get; set; }

        public Editor()
        {
            autoLayout = true;
            autoLayoutDirection = LayoutDirection.Horizontal;
            clipChildren = true;
            atlas = TextureUtil.GetAtlas("Ingame");
            backgroundSprite = "UnlockingItemBackground";

            AddPanels();
        }
        private void AddPanels()
        {
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
            ItemsPanel.atlas = TextureUtil.GetAtlas("Ingame");
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
            SettingsPanel.atlas = TextureUtil.GetAtlas("Ingame");
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
            var item = ItemsPanel.AddUIComponent<EditableItemType>();
            item.name = editableObject.ToString();
            item.width = ItemsPanel.width;
            item.Object = editableObject;
            item.eventClick += ItemClick;

            return item;
        }
        protected override void ItemClick(UIComponent component, UIMouseEventParameter eventParam) => ItemClick((EditableItemType)component);
        protected virtual void ItemClick(EditableItemType item)
        {
            ClearSettings();
            EditObject = item.Object;
            OnObjectSelect();
        }
        protected virtual void OnObjectSelect()
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
            ItemsPanel.ScrollIntoView(item);
        }
        public void Select(EditableObject editableObject)
        {
            if (ItemsPanel.components.OfType<EditableItemType>().FirstOrDefault(c => System.Object.ReferenceEquals(c.Object, editableObject)) is EditableItemType item)
                Select(item);
        }
    }
    public abstract class EditableItem : UIButton
    {
        protected UILabel Label { get; set; }

        public string Text
        {
            get => Label.text;
            set => Label.text = value;
        }
    }
    public abstract class EditableItem<EditableObject, IconType> : EditableItem where IconType : UIComponent
    {
        EditableObject _object;
        public EditableObject Object
        {
            get => _object;
            set
            {
                _object = value;
                Text = value.ToString();
                OnObjectSet();
            }
        }
        public IconType Icon { get; }

        public EditableItem()
        {
            atlas = TextureUtil.GetAtlas("Ingame");

            normalBgSprite = "ButtonSmall";
            disabledBgSprite = "ButtonSmallDisabled";
            focusedBgSprite = "ButtonSmallPressed";
            hoveredBgSprite = "ButtonSmallHovered";
            pressedBgSprite = "ButtonSmallPressed";

            Icon = AddUIComponent<IconType>();

            Label = AddUIComponent<UILabel>();
            Label.textAlignment = UIHorizontalAlignment.Left;
            Label.verticalAlignment = UIVerticalAlignment.Middle;
            Label.autoSize = false;
            Label.autoHeight = false;
            Label.textScale = 0.7f;

            height = 25;
        }

        protected virtual void OnObjectSet()
        {

        }
        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            if (Icon != null)
            {
                Icon.size = new Vector2(size.y - 6, size.y - 6);
                Icon.relativePosition = new Vector2(3, 3);
            }

            if (Label != null)
            {
                Label.size = new Vector2(size.x - size.y, size.y);
                Label.relativePosition = new Vector3(size.y, 0);
            }
        }
    }

    public class ColorIcon : UIButton
    {
        private UIButton ColorCircule { get; set; }
        public Color32 Color
        {
            get => ColorCircule.color;
            set => ColorCircule.color = value;
        }
        public ColorIcon()
        {
            atlas = TextureUtil.GetAtlas("Ingame");
            normalBgSprite = "PieChartWhiteBg";
            isInteractive = false;
            color = UnityEngine.Color.white;

            ColorCircule = AddUIComponent<UIButton>();
            ColorCircule.atlas = TextureUtil.GetAtlas("Ingame");
            ColorCircule.normalBgSprite = "PieChartWhiteBg";
            ColorCircule.normalFgSprite = "PieChartWhiteFg";
            ColorCircule.isInteractive = false;
            ColorCircule.relativePosition = new Vector3(2, 2);
        }
        protected override void OnSizeChanged()
        {
            if (ColorCircule != null)
            {
                ColorCircule.height = height - 4;
                ColorCircule.width = width - 4;
            }
        }
    }
}
