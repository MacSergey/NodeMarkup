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
    public class PanelHeaderContent : BasePanelHeaderContent<PanelHeaderButton, AdditionallyHeaderButton>
    {
        private PanelHeaderButton PasteButton { get; set; }
        public MarkupType Type { get; set; }

        protected override void AddButtons()
        {
            AddButton(NodeMarkupTextures.AddTemplate, NodeMarkup.Localize.Panel_SaveAsPreset, NodeMarkupTool.SaveAsIntersectionTemplateShortcut);

            AddButton(NodeMarkupTextures.Copy, NodeMarkup.Localize.Panel_CopyMarking, NodeMarkupTool.CopyMarkingShortcut);
            PasteButton = AddButton(NodeMarkupTextures.Paste, NodeMarkup.Localize.Panel_PasteMarking, NodeMarkupTool.PasteMarkingShortcut);
            AddButton(NodeMarkupTextures.Clear, NodeMarkup.Localize.Panel_ClearMarking, NodeMarkupTool.DeleteAllShortcut);

            SetPasteEnabled();
        }
        protected override AdditionallyHeaderButton GetAdditionally() => AddButton<AdditionallyHeaderButton>(NodeMarkupTextures.Additionally, CommonLocalize.Panel_Additional);

        protected override void AddAdditionallyButtons(UIComponent parent)
        {
            AddPopupButton(parent, NodeMarkupTextures.Edit, NodeMarkup.Localize.Panel_EditMarking, NodeMarkupTool.EditMarkingShortcut);
            AddPopupButton(parent, NodeMarkupTextures.Offset, NodeMarkup.Localize.Panel_ResetOffset, NodeMarkupTool.ResetOffsetsShortcut);

            if (Type == MarkupType.Node)
            {
                AddPopupButton(parent, NodeMarkupTextures.EdgeLines, NodeMarkup.Localize.Panel_CreateEdgeLines, NodeMarkupTool.CreateEdgeLinesShortcut);
                AddPopupButton(parent, NodeMarkupTextures.Cut, NodeMarkup.Localize.Panel_CutLinesByCrosswalks, NodeMarkupTool.CutLinesByCrosswalksShortcut);
            }
            if (Type == MarkupType.Segment)
            {
                AddPopupButton(parent, NodeMarkupTextures.BeetwenIntersections, NodeMarkup.Localize.Panel_ApplyBetweenIntersections, NodeMarkupTool.ApplyBetweenIntersectionsShortcut);
                AddPopupButton(parent, NodeMarkupTextures.WholeStreet, NodeMarkup.Localize.Panel_ApplyWholeStreet, NodeMarkupTool.ApplyWholeStreetShortcut);
            }
        }
        public override void Refresh()
        {
            SetPasteEnabled();
            base.Refresh();
        }
        private void SetPasteEnabled() => PasteButton.isEnabled = !SingletonTool<NodeMarkupTool>.Instance.IsMarkupBufferEmpty;
    }
    public class PanelHeaderButton : BasePanelHeaderButton
    {
        protected override UITextureAtlas IconAtlas => NodeMarkupTextures.Atlas;
    }
    public class AdditionallyHeaderButton : BaseAdditionallyHeaderButton
    {
        protected override UITextureAtlas IconAtlas => NodeMarkupTextures.Atlas;
    }
}
