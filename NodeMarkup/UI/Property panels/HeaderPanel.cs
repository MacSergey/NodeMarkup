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
    public abstract class HeaderPanel : EditorItem
    {
        public event Action OnDelete;

        protected UIButton DeleteButton { get; set; }

        public HeaderPanel()
        {
            AddDeleteButton();
        }

        public virtual void Init(bool isDeletable = true)
        {
            base.Init();
            DeleteButton.enabled = isDeletable;
        }
        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            DeleteButton.relativePosition = new Vector2(width - DeleteButton.width - 5, (height - DeleteButton.height) / 2);
        }

        private void AddDeleteButton()
        {
            DeleteButton = AddUIComponent<UIButton>();
            DeleteButton.atlas = TextureUtil.InGameAtlas;
            DeleteButton.normalBgSprite = "buttonclose";
            DeleteButton.hoveredBgSprite = "buttonclosehover";
            DeleteButton.pressedBgSprite = "buttonclosepressed";
            DeleteButton.size = new Vector2(20, 20);
            DeleteButton.eventClick += DeleteClick;
        }
        private void DeleteClick(UIComponent component, UIMouseEventParameter eventParam) => OnDelete?.Invoke();
    }
    public class StyleHeaderPanel : HeaderPanel
    {
        private static StyleTemplate EmptyTemplate { get; set; } = new StyleTemplate(string.Empty, RegularLineStyle.GetDefault(RegularLineStyle.RegularLineType.Dashed));

        public event Action OnSaveTemplate;
        public event Action<StyleTemplate> OnSelectTemplate;


        UIButton SaveTemplateButton { get; set; }
        TemplateDropDown SelectTemplate { get; set; }
        Style.StyleType StyleGroup { get; set; }

        public StyleHeaderPanel()
        {
            AddSaveTemplate();
            AddApplyTemplate();
        }

        public void Init(Style.StyleType styleGroup, bool isDeletable = true)
        {
            base.Init(isDeletable);
            StyleGroup = styleGroup & Style.StyleType.GroupMask;
            Fill();
            SelectTemplate.selectedIndex = 0;
        }
        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            SaveTemplateButton.relativePosition = new Vector2(5, (height - SaveTemplateButton.height) / 2);
            SelectTemplate.relativePosition = new Vector2(SaveTemplateButton.relativePosition.x + SaveTemplateButton.width + 5, (height - SelectTemplate.height) / 2);
        }


        private void AddSaveTemplate()
        {
            SaveTemplateButton = AddUIComponent<UIButton>();
            SaveTemplateButton.atlas = TextureUtil.InMapEditorAtlas;
            SaveTemplateButton.normalBgSprite = "InfoDisplay";
            SaveTemplateButton.hoveredBgSprite = "InfoDisplayHover";
            SaveTemplateButton.pressedBgSprite = "InfoDisplayFocused";
            SaveTemplateButton.text = NodeMarkup.Localize.HeaderPanel_SaveAsTemplate;
            SaveTemplateButton.textScale = 0.7f;
            SaveTemplateButton.textPadding = new RectOffset(0, 0, 2, 0);
            SaveTemplateButton.size = new Vector2(120, 20);
            SaveTemplateButton.textColor = Color.black;
            SaveTemplateButton.hoveredTextColor = Color.black;
            SaveTemplateButton.pressedTextColor = Color.black;
            SaveTemplateButton.focusedTextColor = Color.black;
            SaveTemplateButton.eventClick += SaveTemplateClick;
        }
        private void AddApplyTemplate()
        {
            SelectTemplate = AddUIComponent<TemplateDropDown>();

            SelectTemplate.atlas = TextureUtil.InMapEditorAtlas;
            SelectTemplate.height = 20;
            SelectTemplate.width = 150;
            SelectTemplate.listBackground = "TextFieldPanel";
            SelectTemplate.itemHeight = 20;
            SelectTemplate.itemHover = "TextFieldPanelHovered";
            SelectTemplate.itemHighlight = "ListItemHighlight";
            SelectTemplate.normalBgSprite = "InfoDisplay";
            SelectTemplate.hoveredBgSprite = "InfoDisplayHover";
            SelectTemplate.listWidth = 150;
            SelectTemplate.listHeight = 700;
            SelectTemplate.listPosition = UIDropDown.PopupListPosition.Below;
            SelectTemplate.clampListToScreen = true;
            SelectTemplate.foregroundSpriteMode = UIForegroundSpriteMode.Stretch;
            SelectTemplate.popupColor = new Color32(45, 52, 61, 255);
            SelectTemplate.popupTextColor = new Color32(170, 170, 170, 255);
            SelectTemplate.textScale = 0.7f;
            SelectTemplate.textFieldPadding = new RectOffset(8, 0, 6, 0);
            SelectTemplate.textColor = Color.black;
            SelectTemplate.popupColor = Color.white;
            SelectTemplate.popupTextColor = Color.black;
            SelectTemplate.verticalAlignment = UIVerticalAlignment.Middle;
            SelectTemplate.horizontalAlignment = UIHorizontalAlignment.Left;
            SelectTemplate.itemPadding = new RectOffset(5, 0, 5, 0);
            SelectTemplate.filteredItems = new int[] { 0 };
            SelectTemplate.eventDropdownOpen += DropdownOpen;
            SelectTemplate.eventDropdownClose += DropdownClose;

            var button = SelectTemplate.AddUIComponent<UIButton>();
            button.atlas = TextureUtil.InGameAtlas;
            button.text = string.Empty;
            button.size = SelectTemplate.size;
            button.relativePosition = new Vector3(0f, 0f);
            button.textVerticalAlignment = UIVerticalAlignment.Middle;
            button.textHorizontalAlignment = UIHorizontalAlignment.Left;
            button.normalFgSprite = "IconDownArrow";
            button.hoveredFgSprite = "IconDownArrowHovered";
            button.pressedFgSprite = "IconDownArrowPressed";
            button.focusedFgSprite = "IconDownArrowFocused";
            button.disabledFgSprite = "IconDownArrowDisabled";
            button.foregroundSpriteMode = UIForegroundSpriteMode.Scale;
            button.horizontalAlignment = UIHorizontalAlignment.Right;
            button.verticalAlignment = UIVerticalAlignment.Middle;
            button.textScale = 0.8f;
            button.eventClick += (UIComponent component, UIMouseEventParameter eventParam) => Fill();

            SelectTemplate.triggerButton = button;
        }
        private void SaveTemplateClick(UIComponent component, UIMouseEventParameter eventParam) => OnSaveTemplate?.Invoke();
        private void DropdownOpen(UIDropDown dropdown, UIListBox popup, ref bool overridden)
        {
            popup.items = popup.items.ToArray();
            popup.items[0] = string.Empty;
            SelectTemplate.eventSelectedIndexChanged += DropDownIndexChanged;
        }
        private void DropDownIndexChanged(UIComponent component, int value)
        {
            if (value != -1)
                OnSelectTemplate?.Invoke(SelectTemplate.SelectedObject);
        }
        private void DropdownClose(UIDropDown dropdown, UIListBox popup, ref bool overridden)
        {
            SelectTemplate.eventSelectedIndexChanged -= DropDownIndexChanged;
            SelectTemplate.selectedIndex = 0;
        }
        private void Fill()
        {
            SelectTemplate.Clear();
            SelectTemplate.AddItem(EmptyTemplate, NodeMarkup.Localize.HeaderPanel_ApplyTemplate);
            foreach(var template in TemplateManager.GetTemplates(StyleGroup))
            {
                SelectTemplate.AddItem(template, template.ToStringWithShort());
            }
        }

        public class TemplateDropDown : CustomUIDropDown<StyleTemplate> { }
    }
    public class TemplateHeaderPanel : HeaderPanel
    {
        public event Action OnSetAsDefault;

        UIButton SetAsDefaultButton { get; set; }

        public TemplateHeaderPanel()
        {
            AddSetAsDefault();
        }
        public new void Init(bool isDefault)
        {
            base.Init(false);

            SetAsDefaultButton.text = isDefault ? NodeMarkup.Localize.HeaderPanel_UnsetAsDefault : NodeMarkup.Localize.HeaderPanel_SetAsDefault;
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            SetAsDefaultButton.relativePosition = new Vector2(5, (height - SetAsDefaultButton.height) / 2);
        }

        private void AddSetAsDefault()
        {
            SetAsDefaultButton = AddUIComponent<UIButton>();
            SetAsDefaultButton.atlas = TextureUtil.InMapEditorAtlas;
            SetAsDefaultButton.normalBgSprite = "InfoDisplay";
            SetAsDefaultButton.hoveredBgSprite = "InfoDisplayHover";
            SetAsDefaultButton.pressedBgSprite = "InfoDisplayFocused";
            SetAsDefaultButton.textScale = 0.7f;
            SetAsDefaultButton.textPadding = new RectOffset(0, 0, 2, 0);
            SetAsDefaultButton.size = new Vector2(180, 20);
            SetAsDefaultButton.textColor = Color.black;
            SetAsDefaultButton.hoveredTextColor = Color.black;
            SetAsDefaultButton.pressedTextColor = Color.black;
            SetAsDefaultButton.focusedTextColor = Color.black;
            SetAsDefaultButton.eventClick += SetAsDefaultClick;
        }
        private void SetAsDefaultClick(UIComponent component, UIMouseEventParameter eventParam) => OnSetAsDefault?.Invoke();
    }
}
