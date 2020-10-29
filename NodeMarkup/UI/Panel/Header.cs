using ColossalFramework;
using ColossalFramework.UI;
using NodeMarkup.Manager;
using NodeMarkup.Tools;
using NodeMarkup.UI.Editors;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI.Panel
{
    public class PanelHeader : UIDragHandle
    {
        private UILabel Caption { get; set; }
        public PanelHeaderContent Header { get; private set; }

        public string Text
        {
            get => Caption.text;
            set => Caption.text = value;
        }

        public PanelHeader()
        {
            CreateCaption();
            CreateButtonsPanel();
        }

        private void CreateCaption()
        {
            Caption = AddUIComponent<UILabel>();
            Caption.autoSize = false;
            Caption.text = nameof(NodeMarkupPanel);
            Caption.textAlignment = UIHorizontalAlignment.Center;
            Caption.verticalAlignment = UIVerticalAlignment.Middle;
        }
        private void CreateButtonsPanel()
        {
            Header = AddUIComponent<PanelHeaderContent>();
        }
        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            Header.autoLayout = true;
            Header.autoLayout = false;
            Header.FitChildrenHorizontally();
            Header.height = height;

            foreach (var item in Header.components)
                item.relativePosition = new Vector2(item.relativePosition.x, (Header.height - item.height) / 2);

            Caption.width = width - Header.width;
            Caption.relativePosition = new Vector2(0, (height - Caption.height) / 2);

            Header.relativePosition = new Vector2(Caption.width - 5, (height - Header.height) / 2);
        }
    }
    public class PanelHeaderContent : HeaderContent
    {
        private AdditionallyHeaderButton Additionally { get; }

        public PanelHeaderContent()
        {
            AddButton(HeaderButton.AddTemplate, NodeMarkup.Localize.HeaderPanel_SaveAsPreset, NodeMarkupTool.SaveAsPresetShortcut);

            AddButton(HeaderButton.Copy, NodeMarkup.Localize.Panel_CopyMarking, NodeMarkupTool.CopyMarkingShortcut);
            AddButton(HeaderButton.Paste, NodeMarkup.Localize.Panel_PasteMarking, NodeMarkupTool.PasteMarkingShortcut);
            AddButton(HeaderButton.Clear, NodeMarkup.Localize.Panel_ClearMarking, NodeMarkupTool.DeleteAllShortcut);

            Additionally = AddButton<AdditionallyHeaderButton>(HeaderButton.Additionally, NodeMarkup.Localize.Panel_Additional);
            Additionally.OpenPopupEvent += OnAdditionallyPopup;
        }

        private void OnAdditionallyPopup(AdditionallyPopup popup)
        {
            var buttons = new List<SimpleHeaderButton>
            {
                AddButton(popup.Content, HeaderButton.Edit, NodeMarkup.Localize.Panel_EditMarking, NodeMarkupTool.EditMarkingShortcut),
                AddButton(popup.Content, HeaderButton.Offset, NodeMarkup.Localize.Panel_ResetOffset,NodeMarkupTool.ResetOffsetsShortcut),
                AddButton(popup.Content, HeaderButton.EdgeLines, NodeMarkup.Localize.Panel_CreateEdgeLines,NodeMarkupTool.CreateEdgeLinesShortcut),
            };

            foreach (var button in buttons)
            {
                button.autoSize = true;
                button.autoSize = false;
            }

            popup.Width = buttons.Max(b => b.width);
        }
        private SimpleHeaderButton AddButton(string sprite, string text, Shortcut shortcut) => AddButton(sprite, GetText(text, shortcut), onClick: (UIComponent _, UIMouseEventParameter __) => shortcut.Press());
        private SimpleHeaderButton AddButton(UIComponent parent, string sprite, string text, Shortcut shortcut)
        {
            return AddButton(parent, sprite, GetText(text, shortcut), true, action);

            void action(UIComponent component, UIMouseEventParameter eventParam)
            {
                Additionally.ClosePopup();
                shortcut.Press();
            }
        }


        private string GetText(string text, Shortcut shortcut) => $"{text} ({shortcut})";
    }
    public class AdditionallyHeaderButton : HeaderPopupButton<AdditionallyPopup>
    {
        public event Action<AdditionallyPopup> OpenPopupEvent;
        protected override void OnOpenPopup() => OpenPopupEvent?.Invoke(Popup);
    }
    public class AdditionallyPopup : PopupPanel
    {
        protected override Color32 Background => Color.white;
    }

    public class ApplyPresetHeaderButton : ApplyHeaderButton<IntersectionTemplate, ApplyPresetPopupPanel, PresetPopupItem, PresetIcon, string>
    {
        protected override Func<IntersectionTemplate, bool> Selector => (t) => true;
        protected override Func<IntersectionTemplate, string> Order => (t) => t.Name;
    }

    public class ApplyPresetPopupPanel : ApplyPopupPanel<IntersectionTemplate, PresetPopupItem, PresetIcon>
    {
        protected override string EmptyText => NodeMarkup.Localize.HeaderPanel_NoPresets;
        protected override IEnumerable<IntersectionTemplate> GetItems(Func<IntersectionTemplate, bool> selector) => TemplateManager.IntersectionManager.Templates;
    }
    public class PresetPopupItem : PresetItem
    {
        public override bool ShowDelete => false;
    }
}
