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

namespace NodeMarkup.UI.Editors
{
    public class FillerEditor : SimpleEditor<FillerItemsPanel, MarkupFiller>
    {
        #region PROPERTIES

        public override string Name => NodeMarkup.Localize.FillerEditor_Fillers;
        public override string EmptyMessage => string.Format(NodeMarkup.Localize.FillerEditor_EmptyMessage, LocalizeExtension.Alt, NodeMarkupTool.AddFillerShortcut);
        public override Markup.SupportType Support { get; } = Markup.SupportType.Fillers;

        public StylePropertyPanel Style { get; private set; }
        private List<EditorItem> StyleProperties { get; set; } = new List<EditorItem>();
        private MoreOptionsPanel MoreOptionsButton { get; set; }
        private bool ShowMoreOptions { get; set; }

        private FillerRailToolMode FillerRailToolMode { get; }

        public FillerRailPropertyPanel.SelectRailButton HoverRailSelectButton { get; private set; }

        #endregion

        #region BASIC

        public FillerEditor()
        {
            FillerRailToolMode = Tool.CreateToolMode<FillerRailToolMode>();
            FillerRailToolMode.Init(this);
        }
        protected override IEnumerable<MarkupFiller> GetObjects() => Markup.Fillers;

        protected override void OnFillPropertiesPanel(MarkupFiller filler)
        {
            AddHeader();
            AddStyleTypeProperty();
            AddMoreOptions();
            AddStyleProperties();
        }
        protected override void OnObjectDelete(MarkupFiller filler)
        {
            Markup.RemoveFiller(filler);
            base.OnObjectDelete(filler);
        }
        protected override void OnClear()
        {
            base.OnClear();

            Style = null;
            MoreOptionsButton = null;
            ShowMoreOptions = false;
            StyleProperties.Clear();
        }

        private void AddHeader()
        {
            var header = ComponentPool.Get<StyleHeaderPanel>(PropertiesPanel, "Header");
            header.Init(Manager.Style.StyleType.Filler, SelectTemplate, false);
            header.OnSaveTemplate += SaveTemplate;
            header.OnCopy += CopyStyle;
            header.OnPaste += PasteStyle;
        }
        private void AddStyleTypeProperty()
        {
            Style = ComponentPool.Get<FillerStylePropertyPanel>(PropertiesPanel, nameof(Style));
            Style.Text = NodeMarkup.Localize.Editor_Style;
            Style.Init();
            Style.UseWheel = true;
            Style.WheelTip = true;
            Style.SelectedObject = EditObject.Style.Value.Type;
            Style.OnSelectObjectChanged += StyleChanged;
        }
        private void AddMoreOptions()
        {
            MoreOptionsButton = ComponentPool.Get<MoreOptionsPanel>(PropertiesPanel, nameof(MoreOptionsButton));
            MoreOptionsButton.Init();
            MoreOptionsButton.OnButtonClick += () =>
            {
                ShowMoreOptions = !ShowMoreOptions;
                SetOptionsCollapse();
            };
        }
        private void SetOptionsCollapse()
        {
            MoreOptionsButton.Text = ShowMoreOptions ? $"▲ {NodeMarkup.Localize.Editor_LessOptions} ▲" : $"▼ {NodeMarkup.Localize.Editor_MoreOptions} ▼";

            foreach (var option in StyleProperties)
                option.IsCollapsed = !ShowMoreOptions;
        }

        private void AddStyleProperties()
        {
            var startIndex = PropertiesPanel.childCount;
            var style = EditObject.Style.Value;
            StyleProperties = style.GetUIComponents(EditObject, PropertiesPanel);
            StyleProperties.Sort((x, y) => style.GetUIComponentSortIndex(x) - style.GetUIComponentSortIndex(y));
            for (int i = 0; i < StyleProperties.Count; i += 1)
                StyleProperties[i].zOrder = startIndex + i;

            foreach (var property in StyleProperties)
            {
                if (property is ColorPropertyPanel colorProperty)
                    colorProperty.OnValueChanged += (Color32 c) => RefreshSelectedItem();
                else if (property is FillerRailPropertyPanel railProperty)
                {
                    railProperty.OnSelect += (panel) => SelectRail(panel);
                    railProperty.OnEnter += HoverRail;
                    railProperty.OnLeave += LeaveRail;
                }
            }

            if (Settings.CollapseOptions && StyleProperties.Count(p => p.CanCollapse) >= 2)
            {
                MoreOptionsButton.isVisible = true;
                MoreOptionsButton.BringToFront();
                SetOptionsCollapse();
            }
            else
                MoreOptionsButton.isVisible = false;
        }
        private void ClearStyleProperties()
        {
            foreach (var property in StyleProperties)
                ComponentPool.Free(property);

            StyleProperties.Clear();
        }

        #endregion

        #region STYLE CHANGE

        private void StyleChanged(Style.StyleType style)
        {
            if (style == EditObject.Style.Value.Type)
                return;

            var newStyle = SingletonManager<StyleTemplateManager>.Instance.GetDefault<FillerStyle>(style);
            EditObject.Style.Value.CopyTo(newStyle);
            EditObject.Style.Value = newStyle;

            AfterStyleChanged();
        }
        private void AfterStyleChanged()
        {
            RefreshSelectedItem();
            PropertiesPanel.StopLayout();
            ClearStyleProperties();
            AddStyleProperties();
            PropertiesPanel.StartLayout();
        }

        private void ApplyStyle(FillerStyle style)
        {
            EditObject.Style.Value = style.CopyStyle();
            Style.SelectedObject = EditObject.Style.Value.Type;

            AfterStyleChanged();
        }

        #endregion

        #region HANDLERS

        private void SaveTemplate()
        {
            if (SingletonManager<StyleTemplateManager>.Instance.AddTemplate(EditObject.Style, out StyleTemplate template))
                Panel.EditStyleTemplate(template);
        }
        private void SelectTemplate(StyleTemplate template)
        {
            if (template.Style is FillerStyle style)
                ApplyStyle(style);
        }
        private void CopyStyle() => Tool.ToStyleBuffer(Manager.Style.StyleType.Filler, EditObject.Style.Value);
        private void PasteStyle()
        {
            if (Tool.FromStyleBuffer<FillerStyle>(Manager.Style.StyleType.Filler, out var style))
                ApplyStyle(style);
        }

        public void HoverRail(FillerRailPropertyPanel.SelectRailButton selectButton) => HoverRailSelectButton = selectButton;
        public void LeaveRail(FillerRailPropertyPanel.SelectRailButton selectButton) => HoverRailSelectButton = null;
        public bool SelectRail(FillerRailPropertyPanel.SelectRailButton selectButton) => SelectRail(selectButton, null);
        public bool SelectRail(FillerRailPropertyPanel.SelectRailButton selectButton, Func<Event, bool> afterAction)
        {
            if (Tool.Mode == FillerRailToolMode && selectButton == FillerRailToolMode.SelectButton)
            {
                Tool.SetDefaultMode();
                return true;
            }
            else
            {
                Tool.SetMode(FillerRailToolMode);
                FillerRailToolMode.Contour = EditObject.Contour;
                FillerRailToolMode.SelectButton = selectButton;
                FillerRailToolMode.AfterSelectButton = afterAction;
                selectButton.Focus();
                return false;
            }
        }

        public override void Render(RenderManager.CameraInfo cameraInfo)
        {
            ItemsPanel.HoverObject?.Render(new OverlayData(cameraInfo) { Color = Colors.Hover });

            if (HoverRailSelectButton != null)
            {
                var rail = EditObject.Contour.GetRail(HoverRailSelectButton.Value.A, HoverRailSelectButton.Value.B, HoverRailSelectButton.Other.A, HoverRailSelectButton.Other.B);
                rail.Render(new OverlayData(cameraInfo) { Color = Colors.Hover });
            }
        }

        #endregion
    }
    public class FillerItemsPanel : ItemsPanel<FillerItem, MarkupFiller>
    {
        public override int Compare(MarkupFiller x, MarkupFiller y) => 0;
    }
    public class FillerItem : EditItem<MarkupFiller, StyleIcon>
    {
        public override void Refresh()
        {
            base.Refresh();

            Icon.Type = Object.Style.Value.Type;
            Icon.StyleColor = Object.Style.Value.Color;
        }
    }

    public class FillerRailToolMode : BasePanelMode<FillerEditor, FillerRailPropertyPanel.SelectRailButton, FillerRail>
    {
        protected override FillerRail Hover => throw new NotImplementedException();
        protected override bool IsHover => throw new NotImplementedException();

        private IFillerVertex FirstPoint { get; set; }
        private bool IsFirstSelected => FirstPoint != null;

        public FillerContour Contour { get; set; }
        private PointsSelector<IFillerVertex> PointsSelector { get; set; }
        private LinesSelector<RailBound> LineSelector { get; set; }

        protected override void OnSetButton()
        {
            FirstPoint = null;
            PointsSelector = GetPointsSelector();
            LineSelector = new LinesSelector<RailBound>(Contour.TrajectoriesProcessed.Select((t, i) => new RailBound(t, 0.5f, i)), Colors.Orange);
        }
        public override void OnToolUpdate()
        {
            if (!IsFirstSelected)
                LineSelector.OnUpdate();
            PointsSelector.OnUpdate();
        }

        public override void OnPrimaryMouseClicked(Event e)
        {
            if (!IsFirstSelected)
            {
                if (PointsSelector.IsHoverPoint)
                {
                    FirstPoint = PointsSelector.HoverPoint;
                    PointsSelector = GetPointsSelector(FirstPoint);
                }
                else if (LineSelector.IsHoverLine)
                    SetValue(e, LineSelector.HoverLine.Index, LineSelector.HoverLine.Index + 1);
            }
            else if (PointsSelector.IsHoverPoint)
                SetValue(e, Contour.IndexOfProcessed(FirstPoint), Contour.IndexOfProcessed(PointsSelector.HoverPoint));
        }
        public override void OnSecondaryMouseClicked()
        {
            if (IsFirstSelected)
            {
                FirstPoint = null;
                PointsSelector = GetPointsSelector();
            }
            else
                base.OnSecondaryMouseClicked();
        }
        private void SetValue(Event e, int a, int b)
        {
            SelectButton.Value = new FillerRail(a % Contour.ProcessedCount, b % Contour.ProcessedCount);
            if (AfterSelectButton?.Invoke(e) ?? true)
                Tool.SetDefaultMode();
        }
        public override string GetToolInfo() => !IsFirstSelected ? Localize.FillerEditor_InfoSelectRailFirst : Localize.FillerEditor_InfoSelectRailSecond;

        private PointsSelector<IFillerVertex> GetPointsSelector(IFillerVertex ignore = null)
        {
            if (ignore != null)
                ignore = ignore.ProcessedVertex;

            return new PointsSelector<IFillerVertex>(Contour.RawVertices.Where(v => ignore == null || !v.ProcessedVertex.Equals(ignore)), Colors.Purple);
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            if (!IsFirstSelected)
            {
                var overlayData = new OverlayData(cameraInfo) { Color = Colors.Hover };
                foreach (var part in Contour.RawParts)
                {
                    if (part.IsPoint)
                        part.Render(overlayData);
                }
                LineSelector.Render(cameraInfo, !(PointsSelector.IsHoverGroup || PointsSelector.IsHoverPoint));
            }
            PointsSelector.Render(cameraInfo);
        }

        private class RailBound : TrajectoryBound
        {
            public int Index { get; }
            public RailBound(ITrajectory trajectory, float size, int index) : base(trajectory, size)
            {
                Index = index;
            }
        }
    }
}
