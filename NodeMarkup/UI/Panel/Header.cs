using ColossalFramework;
using ColossalFramework.UI;
using ModsCommon.Utilities;
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
            Caption = AddUIComponent<UILabel>();
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
    }
    public class PanelHeaderContent : HeaderContent
    {
        private AdditionallyHeaderButton Additionally { get; }

        public PanelHeaderContent()
        {
            AddButton(TextureUtil.AddTemplate, NodeMarkup.Localize.Panel_SaveAsPreset, NodeMarkupTool.SaveAsIntersectionTemplateShortcut);

            AddButton(TextureUtil.Copy, NodeMarkup.Localize.Panel_CopyMarking, NodeMarkupTool.CopyMarkingShortcut);
            AddButton(TextureUtil.Paste, NodeMarkup.Localize.Panel_PasteMarking, NodeMarkupTool.PasteMarkingShortcut);
            AddButton(TextureUtil.Clear, NodeMarkup.Localize.Panel_ClearMarking, NodeMarkupTool.DeleteAllShortcut);

            Additionally = AddButton<AdditionallyHeaderButton>(TextureUtil.Additionally, NodeMarkup.Localize.Panel_Additional);
            Additionally.OpenPopupEvent += OnAdditionallyPopup;
        }

        private void OnAdditionallyPopup(AdditionallyPopup popup)
        {
            var buttons = new List<PanelHeaderButton>
            {
                AddButton(popup.Content, TextureUtil.Edit, NodeMarkup.Localize.Panel_EditMarking, NodeMarkupTool.EditMarkingShortcut),
                AddButton(popup.Content, TextureUtil.Offset, NodeMarkup.Localize.Panel_ResetOffset,NodeMarkupTool.ResetOffsetsShortcut),
                AddButton(popup.Content, TextureUtil.EdgeLines, NodeMarkup.Localize.Panel_CreateEdgeLines,NodeMarkupTool.CreateEdgeLinesShortcut),
                AddButton(popup.Content, TextureUtil.Cut, NodeMarkup.Localize.Panel_CutLinesByCrosswalks,NodeMarkupTool.CutLinesByCrosswalks),
            };

            foreach (var button in buttons)
            {
                button.autoSize = true;
                button.autoSize = false;
            }

            popup.Width = buttons.Max(b => b.width);
        }
        private PanelHeaderButton AddButton(string sprite, string text, NodeMarkupShortcut shortcut) 
            => AddButton<PanelHeaderButton>(sprite, GetText(text, shortcut), onClick: (UIComponent _, UIMouseEventParameter __) => shortcut.Press());
        private PanelHeaderButton AddButton(UIComponent parent, string sprite, string text, NodeMarkupShortcut shortcut)
        {
            return AddButton<PanelHeaderButton>(parent, sprite, GetText(text, shortcut), true, action);

            void action(UIComponent component, UIMouseEventParameter eventParam)
            {
                Additionally.ClosePopup();
                shortcut.Press();
            }
        }


        private string GetText(string text, NodeMarkupShortcut shortcut) => $"{text} ({shortcut})";
    }
    public class PanelHeaderButton : SimpleHeaderButton
    {
        protected override Color32 HoveredColor => new Color32(112, 112, 112, 255);
        protected override Color32 PressedColor => new Color32(144, 144, 144, 255);
        protected override Color32 PressedIconColor => Color.white;
    }
    public class AdditionallyHeaderButton : HeaderPopupButton<AdditionallyPopup>
    {
        protected override Color32 HoveredColor => new Color32(112, 112, 112, 255);
        protected override Color32 PressedColor => new Color32(144, 144, 144, 255);
        protected override Color32 PressedIconColor => Color.white;

        public event Action<AdditionallyPopup> OpenPopupEvent;
        protected override void OnOpenPopup() => OpenPopupEvent?.Invoke(Popup);
    }
    public class AdditionallyPopup : PopupPanel
    {
        protected override Color32 Background => new Color32(64,64,64,255);
    }
}
