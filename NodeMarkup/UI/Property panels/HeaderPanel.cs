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

        protected UIButton AddButton(string text = null, MouseEventHandler onClick = null)
        {
            var button = AddUIComponent<UIButton>();
            button.atlas = TextureUtil.InMapEditorAtlas;
            button.normalBgSprite = "InfoDisplay";
            button.hoveredBgSprite = "InfoDisplayHover";
            button.pressedBgSprite = "InfoDisplayFocused";
            button.text = text;
            button.textScale = 0.7f;
            button.textPadding = new RectOffset(0, 0, 2, 0);
            button.size = new Vector2(180, 20);
            button.textColor = Color.black;
            button.hoveredTextColor = Color.black;
            button.pressedTextColor = Color.black;
            button.focusedTextColor = Color.black;
            if (onClick != null)
                button.eventClick += onClick;

            return button;
        }
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
            foreach (var template in TemplateManager.GetTemplates(StyleGroup))
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
            SetAsDefaultButton = AddButton(onClick: SetAsDefaultClick);
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
        private void SetAsDefaultClick(UIComponent component, UIMouseEventParameter eventParam) => OnSetAsDefault?.Invoke();
    }
    public class CopyPasteHeaderPanel : HeaderPanel
    {
        public event Action OnCopy;
        public event Action OnPaste;

        UIButton Copy { get; set; }
        UIButton Paste { get; set; }
        public new void Init()
        {
            base.Init(false);
        }
        public CopyPasteHeaderPanel()
        {
            Copy = AddButton(NodeMarkup.Localize.LineEditor_StyleCopy, CopyClick);
            Copy.width = 100;
            Paste = AddButton(NodeMarkup.Localize.LineEditor_StylePaste, PasteClick);
            Paste.width = 100;
        }
        private void CopyClick(UIComponent component, UIMouseEventParameter eventParam) => OnCopy?.Invoke();
        private void PasteClick(UIComponent component, UIMouseEventParameter eventParam) => OnPaste?.Invoke();

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            Copy.relativePosition = new Vector2(5, (height - Copy.height) / 2);
            Paste.relativePosition = new Vector2(Copy.relativePosition.x + Copy.width + 5, (height - Paste.height) / 2);
        }
    }
}
