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
        protected Markup Markup { get; set; }

        public abstract string PanelName { get; }
        protected UIScrollablePanel ItemsPanel { get; set; }
        protected UIScrollbar Scrollbar { get; set; }
        protected UIPanel SettingsPanel { get; set; }

        public virtual void SetMarkup(Markup markup)
        {
            Clear();

            Markup = markup;
        }
        protected void Clear()
        {
            var componets = ItemsPanel.components.ToArray();
            foreach (var item in componets)
            {
                item.eventClick -= ItemClick;
                ItemsPanel.RemoveUIComponent(item);
                Destroy(item.gameObject);
            }
        }
        protected abstract void ItemClick(UIComponent component, UIMouseEventParameter eventParam);
    }
    public abstract class Editor<ItemType> : Editor where ItemType : EditableItem
    {
        public Editor(string name)
        {
            autoLayout = true;
            autoLayoutDirection = LayoutDirection.Horizontal;
            clipChildren = true;

            AddPanels();

            var lable = SettingsPanel.AddUIComponent<UILabel>();
            lable.text = name;
        }
        private void AddPanels()
        {
            AddItemsPanel();
            AddScrollbar();
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
        }

        private void ItemsPanelSizeChanged(UIComponent component, Vector2 value)
        {
            foreach (var item in ItemsPanel.components)
            {
                item.width = ItemsPanel.width;
            }
        }

        private void AddScrollbar()
        {
            Scrollbar = AddUIComponent<UIScrollbar>();
            Scrollbar.orientation = UIOrientation.Vertical;
            Scrollbar.pivot = UIPivotPoint.TopLeft;
            Scrollbar.minValue = 0;
            Scrollbar.value = 0;
            Scrollbar.incrementAmount = 50;
            Scrollbar.autoHide = true;
            Scrollbar.width = 10;

            UISlicedSprite trackSprite = Scrollbar.AddUIComponent<UISlicedSprite>();
            trackSprite.relativePosition = Vector2.zero;
            trackSprite.autoSize = true;
            trackSprite.anchor = UIAnchorStyle.All;
            trackSprite.size = trackSprite.parent.size;
            trackSprite.fillDirection = UIFillDirection.Vertical;
            trackSprite.spriteName = "ScrollbarTrack";
            Scrollbar.trackObject = trackSprite;

            UISlicedSprite thumbSprite = trackSprite.AddUIComponent<UISlicedSprite>();
            thumbSprite.relativePosition = Vector2.zero;
            thumbSprite.fillDirection = UIFillDirection.Vertical;
            thumbSprite.autoSize = true;
            thumbSprite.width = thumbSprite.parent.width;
            thumbSprite.spriteName = "ScrollbarThumb";
            Scrollbar.thumbObject = thumbSprite;

            Scrollbar.eventValueChanged += (component, value) => ItemsPanel.scrollPosition = new Vector2(0, value);

            eventMouseWheel += (component, eventParam) => {
                Scrollbar.value -= (int)eventParam.wheelDelta * Scrollbar.incrementAmount;
            };

            ItemsPanel.eventMouseWheel += (component, eventParam) => 
            {
                Scrollbar.value -= (int)eventParam.wheelDelta * Scrollbar.incrementAmount;
            };

            ItemsPanel.verticalScrollbar = Scrollbar;

            Scrollbar.eventVisibilityChanged += ScrollbarVisibilityChanged;
        }

        private void ScrollbarVisibilityChanged(UIComponent component, bool value)
        {
            ItemsPanel.width = size.x / 10 * 3 - (Scrollbar.isVisible ? Scrollbar.width : 0);
        }

        private void AddSettingPanel()
        {
            SettingsPanel = AddUIComponent<UIPanel>();
            SettingsPanel.autoLayout = true;
            SettingsPanel.autoLayoutDirection = LayoutDirection.Vertical;
            SettingsPanel.backgroundSprite = "GenericPanel";
        }
        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            ItemsPanel.width = size.x / 10 * 3 - (Scrollbar.isVisible ? Scrollbar.width : 0);
            SettingsPanel.width = size.x / 10 * 7;
            ItemsPanel.height = size.y;
            SettingsPanel.height = size.y;
            Scrollbar.height = size.y;
        }

        public ItemType AddItem(string name)
        {
            var item = ItemsPanel.AddUIComponent<ItemType>();
            item.width = ItemsPanel.width;
            item.Text = name;
            item.eventClick += ItemClick;

            return item;
        }
        protected override void ItemClick(UIComponent component, UIMouseEventParameter eventParam) => ItemClick((ItemType)component);
        protected abstract void ItemClick(ItemType item);
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
    public abstract class EditableItem<UIType> : EditableItem where UIType : UIComponent
    {
        public UIType Icon { get; }

        public EditableItem()
        {
            atlas = TextureUtil.GetAtlas("Ingame");

            normalBgSprite = "ButtonSmall";
            disabledBgSprite = "ButtonSmallDisabled";
            focusedBgSprite = "ButtonSmallPressed";
            hoveredBgSprite = "ButtonSmallHovered";
            pressedBgSprite = "ButtonSmallPressed";

            Icon = AddUIComponent<UIType>();

            Label = AddUIComponent<UILabel>();
            Label.textAlignment = UIHorizontalAlignment.Left;
            Label.verticalAlignment = UIVerticalAlignment.Middle;
            Label.autoSize = false;
            Label.autoHeight = false;
            Label.textScale = 0.7f;

            height = 25;
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
