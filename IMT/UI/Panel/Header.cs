using IMT.Manager;
using IMT.Tools;
using IMT.Utilities;
using ModsCommon;
using ModsCommon.UI;
using UnityEngine;

namespace IMT.UI.Panel
{
    public class PanelHeader : HeaderMoveablePanel<PanelHeaderContent>
    {
        private MarkingType Type { get; set; }

        private BlurEffect Blur { get; set; }

        private HeaderButtonInfo<HeaderButton> PasteButton { get; }
        private HeaderButtonInfo<HeaderButton> EdgeLinesButton { get; }
        private HeaderButtonInfo<HeaderButton> CutButton { get; }
        private HeaderButtonInfo<HeaderButton> BeetwenIntersectionsButton { get; }
        private HeaderButtonInfo<HeaderButton> WholeStreetButton { get; }

        private bool available = true;
        public bool Available
        {
            get => available;
            set
            {
                if (value != available)
                {
                    available = value;
                    Blur.isVisible = !available;
                }
            }
        }

        public PanelHeader()
        {
            Blur = AddUIComponent<BlurEffect>();
            Blur.relativePosition = Vector3.zero;
            Blur.size = size;
            Blur.isVisible = false;
            Caption.zOrder = Blur.zOrder + 1;

            Content.AddButton(new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, IMTTextures.Atlas, IMTTextures.AddTemplateHeaderButton, IMT.Localize.Panel_SaveAsPreset, IntersectionMarkingTool.SaveAsIntersectionTemplateShortcut));

            Content.AddButton(new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, IMTTextures.Atlas, IMTTextures.CopyHeaderButton, IMT.Localize.Panel_CopyMarking, IntersectionMarkingTool.CopyMarkingShortcut));

            PasteButton = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, IMTTextures.Atlas, IMTTextures.PasteHeaderButton, IMT.Localize.Panel_PasteMarking, IntersectionMarkingTool.PasteMarkingShortcut);
            Content.AddButton(PasteButton);

            Content.AddButton(new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, IMTTextures.Atlas, IMTTextures.ClearHeaderButton, IMT.Localize.Panel_ClearMarking, IntersectionMarkingTool.DeleteAllShortcut));

            Content.AddButton(new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Additional, IMTTextures.Atlas, IMTTextures.EditHeaderButton, IMT.Localize.Panel_EditMarking, IntersectionMarkingTool.EditMarkingShortcut));

            Content.AddButton(new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Additional, IMTTextures.Atlas, IMTTextures.OffsetHeaderButton, IMT.Localize.Panel_ResetOffset, IntersectionMarkingTool.ResetOffsetsShortcut));

            EdgeLinesButton = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Additional, IMTTextures.Atlas, IMTTextures.EdgeLinesHeaderButton, IMT.Localize.Panel_CreateEdgeLines, IntersectionMarkingTool.CreateEdgeLinesShortcut);
            Content.AddButton(EdgeLinesButton);

            CutButton = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Additional, IMTTextures.Atlas, IMTTextures.CutHeaderButton, IMT.Localize.Panel_CutLinesByCrosswalks, IntersectionMarkingTool.CutLinesByCrosswalksShortcut);
            Content.AddButton(CutButton);

            BeetwenIntersectionsButton = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Additional, IMTTextures.Atlas, IMTTextures.BeetwenIntersectionsHeaderButton, IMT.Localize.Panel_ApplyBetweenIntersections, IntersectionMarkingTool.ApplyBetweenIntersectionsShortcut);
            Content.AddButton(BeetwenIntersectionsButton);

            WholeStreetButton = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Additional, IMTTextures.Atlas, IMTTextures.WholeStreetHeaderButton, IMT.Localize.Panel_ApplyWholeStreet, IntersectionMarkingTool.ApplyWholeStreetShortcut);
            Content.AddButton(WholeStreetButton);
        }

        public void Init(float height) => base.Init(height);
        public void Init(MarkingType type)
        {
            Type = type;
            base.Init(null);
        }

        public override void Refresh()
        {
            PasteButton.Enable = !SingletonTool<IntersectionMarkingTool>.Instance.IsMarkingBufferEmpty;

            EdgeLinesButton.Visible = Type == MarkingType.Node;
            CutButton.Visible = Type == MarkingType.Node;

            BeetwenIntersectionsButton.Visible = Type == MarkingType.Segment;
            WholeStreetButton.Visible = Type == MarkingType.Segment;

            base.Refresh();
        }
        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            Blur.size = size;
        }
    }
}
