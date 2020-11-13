using ColossalFramework.Globalization;
using ColossalFramework.UI;
using NodeMarkup.UI.Editors;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI
{
    public abstract class TabStrip<TabType> : UIPanel
        where TabType : Tab
    {
        public Action<int> SelectedTabChanged;
        int _selectedTab;
        public int SelectedTab
        {
            get => _selectedTab;
            set
            {
                if (value != _selectedTab)
                {
                    if (_selectedTab >= 0 && _selectedTab < Tabs.Count)
                        Tabs[_selectedTab].state = UIButton.ButtonState.Normal;

                    _selectedTab = value;
                    SelectedTabChanged?.Invoke(_selectedTab);
                }
            }
        }
        protected List<TabType> Tabs { get; } = new List<TabType>();

        public TabStrip()
        {
            clipChildren = true;
        }
        public override void Update()
        {
            base.Update();

            if (SelectedTab >= 0 && SelectedTab < Tabs.Count)
                Tabs[SelectedTab].state = UIButton.ButtonState.Focused;
        }
        public void AddTab(string name, float textScale = 0.85f) => AddTabImpl(name, textScale);
        protected TabType AddTabImpl(string name, float textScale = 0.85f)
        {
            var tabButton = AddUIComponent<TabType>();
            tabButton.text = name;
            tabButton.textPadding = new RectOffset(5, 5, 2, 2);
            tabButton.textScale = textScale;
            tabButton.verticalAlignment = UIVerticalAlignment.Middle;
            tabButton.eventClick += TabClick;
            tabButton.eventIsEnabledChanged += TabButtonIsEnabledChanged;

            SetStyle(tabButton);

            ArrangeTabs();

            return tabButton;
        }

        private bool ArrangeInProgress { get; set; }
        protected void ArrangeTabs()
        {
            if (Tabs.Count == 0 || ArrangeInProgress)
                return;

            ArrangeInProgress = true;

            foreach (var tab in Tabs)
            {
                tab.autoSize = true;
                tab.autoSize = false;
                tab.textHorizontalAlignment = UIHorizontalAlignment.Center;
            }

            var tabRows = FillTabRows();
            ArrangeTabRows(tabRows);
            PlaceTabRows(tabRows);

            FitChildrenVertically();

            ArrangeInProgress = false;
        }
        private List<List<TabType>> FillTabRows()
        {
            var totalWidth = Tabs.Sum(t => t.width);
            var rows = (int)(totalWidth / width) + 1;
            var tabInRow = Tabs.Count / rows;
            var extraRows = Tabs.Count - (tabInRow * rows);

            var tabRows = new List<List<TabType>>();
            for (var i = 0; i < rows; i += 1)
            {
                var tabRow = new List<TabType>();
                tabRows.Add(tabRow);

                var from = i * tabInRow + Math.Min(i, extraRows);
                var to = from + tabInRow + (i < extraRows ? 1 : 0);
                for (var j = from; j < to; j += 1)
                    tabRow.Add(Tabs[j]);
            }
            return tabRows;
        }
        private void ArrangeTabRows(List<List<TabType>> tabRows)
        {
            for (var i = 0; i < tabRows.Count; i += 1)
            {
                var tabRow = tabRows[i];
                var totalRowWidth = 0f;
                for (var j = 0; j < tabRow.Count; j += 1)
                {
                    if (totalRowWidth + tabRow[j].width > width)
                    {
                        var toMove = tabRow.Skip(j == 0 ? j + 1 : j).ToArray();

                        if(toMove.Any())
                        {
                            if (i == tabRows.Count - 1)
                                tabRows.Add(new List<TabType>());

                            tabRows[i + 1].InsertRange(0, toMove);
                            foreach (var tab in toMove)
                                tabRow.Remove(tab);
                        }

                        break;
                    }
                    else
                        totalRowWidth += tabRow[j].width;
                }
            }
        }
        private void PlaceTabRows(List<List<TabType>> tabRows)
        {
            var totalHeight = 0f;
            for (var i = 0; i < tabRows.Count; i += 1)
            {
                var tabRow = tabRows[i];

                var rowWidth = tabRow.Sum(t => t.width);
                var rowHeight = tabRow.Max(t => t.height);
                if (i < tabRows.Count - 1)
                    rowHeight += 4;

                var space = (width - rowWidth) / tabRow.Count;
                var totalRowWidth = 0f;

                for(var j = 0; j < tabRow.Count; j += 1)
                {
                    var tab = tabRow[j];

                    tab.width = j < tabRow.Count - 1 ? Mathf.Floor(tab.width + space) : width - totalRowWidth;
                    tab.height = rowHeight;
                    tab.relativePosition = new Vector2(totalRowWidth, totalHeight);
                    totalRowWidth += tab.width;
                }

                totalHeight += rowHeight - 4;
            }
        }

        protected override void OnComponentAdded(UIComponent child)
        {
            base.OnComponentAdded(child);

            if (child is TabType tabButton)
            {
                tabButton.eventClick += TabClick;
                tabButton.eventIsEnabledChanged += TabButtonIsEnabledChanged;
                Tabs.Add(tabButton);
            }
        }
        protected override void OnComponentRemoved(UIComponent child)
        {
            base.OnComponentRemoved(child);

            if (child is TabType tabButton)
            {
                tabButton.eventClick -= TabClick;
                tabButton.eventIsEnabledChanged -= TabButtonIsEnabledChanged;
                Tabs.Remove(tabButton);
            }
        }
        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            ArrangeTabs();
        }

        private void TabClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (component is TabType tabButton)
                SelectedTab = Tabs.IndexOf(tabButton);
        }

        private void TabButtonIsEnabledChanged(UIComponent component, bool value)
        {
            if (!component.isEnabled)
            {
                var button = component as TabType;
                button.disabledColor = button.state == UIButton.ButtonState.Focused ? button.focusedColor : button.color;
            }
        }

        protected virtual void SetStyle(TabType tabButton)
        {
            tabButton.atlas = TextureUtil.Atlas;

            tabButton.normalBgSprite = TextureUtil.TabNormal;
            tabButton.focusedBgSprite = TextureUtil.TabFocused;
            tabButton.hoveredBgSprite = TextureUtil.TabHover;
        }
    }
    public class Tab : UIButton { }

    public class TabStrip : TabStrip<Tab> { }

    public class PanelTabStrip : TabStrip<PanelTabStrip.PanelTab>
    {
        private static Color32 NormalColor { get; } = new Color32(107, 113, 115, 255);
        private static Color32 HoverColor { get; } = new Color32(143, 149, 150, 255);
        private static Color32 FocusColor { get; } = new Color32(177, 195, 94, 255);

        public PanelTabStrip() => isLocalized = true;

        protected override void OnLocalize()
        {
            foreach (var tab in Tabs)
                tab.text = tab.Editor.Name;

            ArrangeTabs();
        }

        protected override void SetStyle(PanelTab tabButton)
        {
            tabButton.atlas = TextureUtil.Atlas;

            tabButton.normalBgSprite = TextureUtil.Tab;
            tabButton.focusedBgSprite = TextureUtil.Tab;
            tabButton.hoveredBgSprite = TextureUtil.Tab;
            tabButton.disabledBgSprite = TextureUtil.Tab;

            tabButton.color = NormalColor;
            tabButton.hoveredColor = HoverColor;
            tabButton.pressedColor = FocusColor;
            tabButton.focusedColor = FocusColor;
        }

        public void AddTab(Editor editor) 
        {
            var tab = AddTabImpl(editor.Name);
            tab.Editor = editor;
        }
        public class PanelTab : Tab
        {
            public Editor Editor { get; set; }
        }

        
    }
}
