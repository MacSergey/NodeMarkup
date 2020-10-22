using ColossalFramework;
using ColossalFramework.UI;
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
            AddButton(HeaderButton.Copy, GetText(NodeMarkup.Localize.Panel_CopyMarking, NodeMarkupTool.CopyMarkingShortcut), onClick: (UIComponent _, UIMouseEventParameter __) => NodeMarkupTool.CopyMarkingShortcut.Press());
            AddButton(HeaderButton.Paste, GetText(NodeMarkup.Localize.Panel_PasteMarking, NodeMarkupTool.PasteMarkingShortcut), onClick: (UIComponent _, UIMouseEventParameter __) => NodeMarkupTool.PasteMarkingShortcut.Press());
            AddButton(HeaderButton.Clear, GetText(NodeMarkup.Localize.Panel_ClearMarking, NodeMarkupTool.DeleteAllShortcut), onClick: (UIComponent _, UIMouseEventParameter __) => NodeMarkupTool.DeleteAllShortcut.Press());
            Additionally = AddButton<AdditionallyHeaderButton>(HeaderButton.Additionally, NodeMarkup.Localize.Panel_Additional);
            Additionally.OpenPopupEvent += OnAdditionallyPopup;
        }

        private void OnAdditionallyPopup(AdditionallyPopup popup)
        {
            var buttons = new List<SimpleHeaderButton>
            {
                AddButton(popup.Content, HeaderButton.Edit, GetText(NodeMarkup.Localize.Panel_EditMarking,NodeMarkupTool.EditMarkingShortcut), true, GetOnAdditionallyClick(() => NodeMarkupTool.EditMarkingShortcut.Press())),
                AddButton(popup.Content, HeaderButton.Offset, GetText(NodeMarkup.Localize.Panel_ResetOffset,NodeMarkupTool.ResetOffsetsShortcut), true, GetOnAdditionallyClick(() => NodeMarkupTool.ResetOffsetsShortcut.Press())),
                AddButton(popup.Content, HeaderButton.EdgeLines, GetText(NodeMarkup.Localize.Panel_CreateEdgeLines,NodeMarkupTool.CreateEdgeLinesShortcut), true, GetOnAdditionallyClick(() => NodeMarkupTool.CreateEdgeLinesShortcut.Press())),
            };

            foreach(var button in buttons)
            {
                button.autoSize = true;
                button.autoSize = false;
            }

            popup.Width = buttons.Max(b => b.width);
        }

        MouseEventHandler GetOnAdditionallyClick(Action onClick)
        {
            return (UIComponent component, UIMouseEventParameter eventParam) =>
            {
                Additionally.ClosePopup();
                onClick?.Invoke();
            };
        }
        string GetText(string text, Shortcut shortcut) => $"{text} ({shortcut})";
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
}
