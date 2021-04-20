using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using NodeMarkup.Tools;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NodeMarkup.UI.Panel
{
    public class PanelHeader : HeaderMoveablePanel<PanelHeaderContent>
    {
        public MarkupType Type
        {
            get => Content.Type;
            set => Content.Type = value;
        }
        public bool Available { set => Content.SetAvailable(value); }
    }
    public class PanelHeaderContent : BaseHeaderContent
    {
        private AdditionallyHeaderButton Additionally { get; }
        private PanelHeaderButton PasteButton { get; }
        public MarkupType Type { get; set; }

        public PanelHeaderContent()
        {
            AddButton(NodeMarkupTextures.AddTemplate, NodeMarkup.Localize.Panel_SaveAsPreset, NodeMarkupTool.SaveAsIntersectionTemplateShortcut);

            AddButton(NodeMarkupTextures.Copy, NodeMarkup.Localize.Panel_CopyMarking, NodeMarkupTool.CopyMarkingShortcut, CopyClick);
            PasteButton = AddButton(NodeMarkupTextures.Paste, NodeMarkup.Localize.Panel_PasteMarking, NodeMarkupTool.PasteMarkingShortcut);
            AddButton(NodeMarkupTextures.Clear, NodeMarkup.Localize.Panel_ClearMarking, NodeMarkupTool.DeleteAllShortcut);

            Additionally = AddButton<AdditionallyHeaderButton>(NodeMarkupTextures.Additionally, NodeMarkup.Localize.Panel_Additional);
            Additionally.OpenPopupEvent += OnAdditionallyPopup;

            SetPasteEnabled();
        }

        private void OnAdditionallyPopup(AdditionallyPopup popup)
        {
            var buttons = new List<PanelHeaderButton>
            {
                AddPopupButton(popup.Content, NodeMarkupTextures.Edit, NodeMarkup.Localize.Panel_EditMarking, NodeMarkupTool.EditMarkingShortcut),
                AddPopupButton(popup.Content, NodeMarkupTextures.Offset, NodeMarkup.Localize.Panel_ResetOffset,NodeMarkupTool.ResetOffsetsShortcut),
            };
            if(Type == MarkupType.Node)
            {
                buttons.Add(AddPopupButton(popup.Content, NodeMarkupTextures.EdgeLines, NodeMarkup.Localize.Panel_CreateEdgeLines, NodeMarkupTool.CreateEdgeLinesShortcut));
                buttons.Add(AddPopupButton(popup.Content, NodeMarkupTextures.Cut, NodeMarkup.Localize.Panel_CutLinesByCrosswalks, NodeMarkupTool.CutLinesByCrosswalksShortcut));
            }
            if (Type == MarkupType.Segment)
            {
                buttons.Add(AddPopupButton(popup.Content, NodeMarkupTextures.BeetwenIntersections, NodeMarkup.Localize.Panel_ApplyBetweenIntersections, NodeMarkupTool.ApplyBetweenIntersectionsShortcut));
                buttons.Add(AddPopupButton(popup.Content, NodeMarkupTextures.WholeStreet, NodeMarkup.Localize.Panel_ApplyWholeStreet, NodeMarkupTool.ApplyWholeStreetShortcut));
            }

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
        private void SetPasteEnabled() => PasteButton.isEnabled = !SingletonTool<NodeMarkupTool>.Instance.IsMarkupBufferEmpty;


        private string GetText(string text, NodeMarkupShortcut shortcut) => $"{text} ({shortcut})";
    }
    public class SimpleHeaderButton : HeaderButton
    {
        protected override UITextureAtlas IconAtlas => NodeMarkupTextures.Atlas;
    }
    public class PanelHeaderButton : SimpleHeaderButton
    {
        protected override Color32 HoveredColor => new Color32(112, 112, 112, 255);
        protected override Color32 PressedColor => new Color32(144, 144, 144, 255);
        protected override Color32 PressedIconColor => Color.white;
    }
    public class AdditionallyHeaderButton : HeaderPopupButton<AdditionallyPopup>
    {
        protected override UITextureAtlas IconAtlas => NodeMarkupTextures.Atlas;

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
