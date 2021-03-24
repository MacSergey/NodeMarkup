using ColossalFramework.UI;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.Tools;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NodeMarkup.UI.Panel
{
    public class PanelHeader : CustomUIDragHandle
    {
        private bool CanMove { get; set; }
        private CustomUILabel Caption { get; set; }
        public PanelHeaderContent Buttons { get; private set; }

        public string Text
        {
            get => Caption.text;
            set => Caption.text = value;
        }
        public bool Available { set => Buttons.SetAvailable(value); }

        public PanelHeader()
        {
            CreateCaption();
            CreateButtonsPanel();
        }

        private void CreateCaption()
        {
            Caption = AddUIComponent<CustomUILabel>();
            Caption.autoSize = false;
            Caption.text = nameof(NodeMarkupPanel);
            Caption.textAlignment = UIHorizontalAlignment.Center;
            Caption.verticalAlignment = UIVerticalAlignment.Middle;
        }
        private void CreateButtonsPanel()
        {
            Buttons = AddUIComponent<PanelHeaderContent>();
        }
        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            Buttons.autoLayout = true;
            Buttons.autoLayout = false;
            Buttons.FitChildrenHorizontally();
            Buttons.height = height;

            foreach (var item in Buttons.components)
                item.relativePosition = new Vector2(item.relativePosition.x, (Buttons.height - item.height) / 2);

            Caption.width = width - Buttons.width - 20;
            Caption.relativePosition = new Vector2(10, (height - Caption.height) / 2);

            Buttons.relativePosition = new Vector2(Caption.width - 5 + 20, (height - Buttons.height) / 2);
        }
        protected override void OnMouseDown(UIMouseEventParameter p)
        {
            CanMove = !new Rect(Buttons.absolutePosition, Buttons.size).Contains(NodeMarkupTool.MousePosition);
            base.OnMouseDown(p);
        }
        protected override void OnMouseMove(UIMouseEventParameter p)
        {
            if (CanMove)
                base.OnMouseMove(p);
        }
    }
    public class PanelHeaderContent : HeaderContent
    {
        private AdditionallyHeaderButton Additionally { get; }
        private PanelHeaderButton PasteButton { get; }

        public PanelHeaderContent()
        {
            AddButton(TextureUtil.AddTemplate, NodeMarkup.Localize.Panel_SaveAsPreset, NodeMarkupTool.SaveAsIntersectionTemplateShortcut);

            AddButton(TextureUtil.Copy, NodeMarkup.Localize.Panel_CopyMarking, NodeMarkupTool.CopyMarkingShortcut, CopyClick);
            PasteButton = AddButton(TextureUtil.Paste, NodeMarkup.Localize.Panel_PasteMarking, NodeMarkupTool.PasteMarkingShortcut);
            AddButton(TextureUtil.Clear, NodeMarkup.Localize.Panel_ClearMarking, NodeMarkupTool.DeleteAllShortcut);

            Additionally = AddButton<AdditionallyHeaderButton>(TextureUtil.Additionally, NodeMarkup.Localize.Panel_Additional);
            Additionally.OpenPopupEvent += OnAdditionallyPopup;

            SetPasteEnabled();
        }

        private void OnAdditionallyPopup(AdditionallyPopup popup)
        {
            var buttons = new List<PanelHeaderButton>
            {
                AddPopupButton(popup.Content, TextureUtil.Edit, NodeMarkup.Localize.Panel_EditMarking, NodeMarkupTool.EditMarkingShortcut),
                AddPopupButton(popup.Content, TextureUtil.Offset, NodeMarkup.Localize.Panel_ResetOffset,NodeMarkupTool.ResetOffsetsShortcut),
                AddPopupButton(popup.Content, TextureUtil.EdgeLines, NodeMarkup.Localize.Panel_CreateEdgeLines,NodeMarkupTool.CreateEdgeLinesShortcut),
                AddPopupButton(popup.Content, TextureUtil.Cut, NodeMarkup.Localize.Panel_CutLinesByCrosswalks,NodeMarkupTool.CutLinesByCrosswalks),
            };

            foreach (var button in buttons)
            {
                button.autoSize = true;
                button.autoSize = false;
            }

            popup.Width = buttons.Max(b => b.width);
        }
        private PanelHeaderButton AddButton(string sprite, string text, NodeMarkupShortcut shortcut, MouseEventHandler onClick = null)
            => AddButton<PanelHeaderButton>(sprite, GetText(text, shortcut), onClick: onClick ?? ((UIComponent _, UIMouseEventParameter __) => shortcut.Press()));
        private PanelHeaderButton AddPopupButton(UIComponent parent, string sprite, string text, NodeMarkupShortcut shortcut)
        {
            return AddButton<PanelHeaderButton>(parent, sprite, GetText(text, shortcut), true, action);

            void action(UIComponent component, UIMouseEventParameter eventParam)
            {
                Additionally.ClosePopup();
                shortcut.Press();
            }
        }
        private void CopyClick(UIComponent copyButton, UIMouseEventParameter e)
        {
            NodeMarkupTool.CopyMarkingShortcut.Press();
            SetPasteEnabled();
        }
        private void SetPasteEnabled() => PasteButton.isEnabled = !NodeMarkupTool.Instance.IsMarkupBufferEmpty;


        private string GetText(string text, NodeMarkupShortcut shortcut) => $"{text} ({shortcut})";
    }
    public class SimpleHeaderButton : HeaderButton
    {
        protected override UITextureAtlas IconAtlas => TextureUtil.Atlas;
    }
    public class PanelHeaderButton : SimpleHeaderButton
    {
        protected override Color32 HoveredColor => new Color32(112, 112, 112, 255);
        protected override Color32 PressedColor => new Color32(144, 144, 144, 255);
        protected override Color32 PressedIconColor => Color.white;
    }
    public class AdditionallyHeaderButton : HeaderPopupButton<AdditionallyPopup>
    {
        protected override UITextureAtlas IconAtlas => TextureUtil.Atlas;

        protected override Color32 HoveredColor => new Color32(112, 112, 112, 255);
        protected override Color32 PressedColor => new Color32(144, 144, 144, 255);
        protected override Color32 PressedIconColor => Color.white;

        public event Action<AdditionallyPopup> OpenPopupEvent;
        protected override void OnOpenPopup() => OpenPopupEvent?.Invoke(Popup);
    }
    public class AdditionallyPopup : PopupPanel
    {
        protected override Color32 Background => new Color32(64, 64, 64, 255);
    }
}
