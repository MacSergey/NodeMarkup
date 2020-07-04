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
            DeleteButton.atlas = TextureUtil.GetAtlas("Ingame");
            DeleteButton.normalBgSprite = "buttonclose";
            DeleteButton.hoveredBgSprite = "buttonclosehover";
            DeleteButton.pressedBgSprite = "buttonclosepressed";
            DeleteButton.size = new Vector2(20, 20);
            DeleteButton.eventClick += DeleteClick;
        }
        private void DeleteClick(UIComponent component, UIMouseEventParameter eventParam) => OnDelete?.Invoke();
    }
    public class RuleHeaderPanel : HeaderPanel
    {
        public event Action OnSaveTemplate;
        public event Action<LineStyleTemplate> OnSelectTemplate;


        UIButton SaveTemplateButton { get; set; }
        TemplateDropDown SelectTemplate { get; set; }

        public RuleHeaderPanel()
        {
            AddSaveTemplate();
            AddApplyTemplate();
        }

        public override void Init(bool isDeletable = true)
        {
            base.Init(isDeletable);
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
            SaveTemplateButton.atlas = TextureUtil.GetAtlas("InMapEditor");
            SaveTemplateButton.normalBgSprite = "InfoDisplay";
            SaveTemplateButton.hoveredBgSprite = "InfoDisplayHover";
            SaveTemplateButton.pressedBgSprite = "InfoDisplayFocused";
            SaveTemplateButton.text = "Save as template";
            SaveTemplateButton.textScale = 0.7f;
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

            SelectTemplate.atlas = TextureUtil.GetAtlas("InMapEditor");
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
            SelectTemplate.textColor = Color.black;
            SelectTemplate.popupColor = Color.white;
            SelectTemplate.popupTextColor = Color.black;
            SelectTemplate.verticalAlignment = UIVerticalAlignment.Middle;
            SelectTemplate.horizontalAlignment = UIHorizontalAlignment.Left;
            SelectTemplate.textFieldPadding = new RectOffset(8, 0, 8, 0);
            SelectTemplate.itemPadding = new RectOffset(14, 0, 8, 0);
            SelectTemplate.filteredItems = new int[] { 0 };
            SelectTemplate.eventDropdownOpen += DropdownOpen;
            SelectTemplate.eventDropdownClose += DropdownClose;

            var button = SelectTemplate.AddUIComponent<UIButton>();
            button.atlas = TextureUtil.GetAtlas("Ingame");
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

            SelectTemplate.triggerButton = button;

            Add(new LineStyleTemplate("Apply template", LineStyle.DefaultSolid) { IsEmpty = true });
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

        public void Add(LineStyleTemplate item)
        {
            SelectTemplate.AddItem(item);
        }
        public void AddRange(IEnumerable<LineStyleTemplate> items)
        {
            foreach (var item in items)
            {
                SelectTemplate.AddItem(item);
            }
        }

        public class TemplateDropDown : CustomUIDropDown<LineStyleTemplate> { }
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
            base.Init(true);

            SetAsDefaultButton.text = $"{(isDefault ? "Unset" : "Set")} as default";
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            SetAsDefaultButton.relativePosition = new Vector2(5, (height - SetAsDefaultButton.height) / 2);
        }

        private void AddSetAsDefault()
        {
            SetAsDefaultButton = AddUIComponent<UIButton>();
            SetAsDefaultButton.atlas = TextureUtil.GetAtlas("InMapEditor");
            SetAsDefaultButton.normalBgSprite = "InfoDisplay";
            SetAsDefaultButton.hoveredBgSprite = "InfoDisplayHover";
            SetAsDefaultButton.pressedBgSprite = "InfoDisplayFocused";
            SetAsDefaultButton.textScale = 0.7f;
            SetAsDefaultButton.size = new Vector2(120, 20);
            SetAsDefaultButton.textColor = Color.black;
            SetAsDefaultButton.hoveredTextColor = Color.black;
            SetAsDefaultButton.pressedTextColor = Color.black;
            SetAsDefaultButton.focusedTextColor = Color.black;
            SetAsDefaultButton.eventClick += SetAsDefaultClick;
        }
        private void SetAsDefaultClick(UIComponent component, UIMouseEventParameter eventParam) => OnSetAsDefault?.Invoke();
    }
}
