using IMT.Manager;
using IMT.Tools;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using UnifiedUI.Helpers;
using UnityEngine;

namespace IMT.UI.Editors
{
    public class FillerEditor : SimpleEditor<FillerItemsPanel, MarkingFiller>, IPropertyContainer
    {
        #region PROPERTIES

        public override string Name => IMT.Localize.FillerEditor_Fillers;
        public override string EmptyMessage => string.Format(IMT.Localize.FillerEditor_EmptyMessage, LocalizeExtension.Alt, IntersectionMarkingTool.AddFillerShortcut);
        public override Marking.SupportType Support => Marking.SupportType.Fillers;

        public StylePropertyPanel Style { get; private set; }

        private FillerGuideToolMode FillerGuideToolMode { get; }

        public FillerGuidePropertyPanel.SelectGuideButton HoverGuideSelectButton { get; private set; }

        object IPropertyEditor.EditObject => EditObject;
        bool IPropertyEditor.IsTemplate => false;
        UIAutoLayoutPanel IPropertyContainer.MainPanel => PropertiesPanel;
        Style IPropertyContainer.Style => EditObject.Style.Value;
        Dictionary<string, bool> IPropertyContainer.ExpandList { get; } = new Dictionary<string, bool>();

        Dictionary<string, IPropertyCategoryInfo> IPropertyContainer.CategoryInfos { get; } = new Dictionary<string, IPropertyCategoryInfo>();
        Dictionary<string, List<IPropertyInfo>> IPropertyContainer.PropertyInfos { get; } = new Dictionary<string, List<IPropertyInfo>>();
        Dictionary<string, CategoryItem> IPropertyContainer.CategoryItems { get; } = new Dictionary<string, CategoryItem>();
        List<EditorItem> IPropertyContainer.StyleProperties { get; } = new List<EditorItem>();

        #endregion

        #region BASIC

        public FillerEditor()
        {
            FillerGuideToolMode = Tool.CreateToolMode<FillerGuideToolMode>();
            FillerGuideToolMode.Init(this);
        }
        protected override IEnumerable<MarkingFiller> GetObjects() => Marking.Fillers;

        protected override void OnFillPropertiesPanel(MarkingFiller filler)
        {
            AddHeader();
            AddStyleTypeProperty();
            AddStyleProperties();
        }
        protected override void OnObjectDelete(MarkingFiller filler)
        {
            Marking.RemoveFiller(filler);
            base.OnObjectDelete(filler);
        }
        protected override void OnClear()
        {
            base.OnClear();
            Style = null;
        }
        protected override void OnObjectUpdate(MarkingFiller editObject) => (this as IPropertyEditor).RefreshProperties();
        void IPropertyEditor.RefreshProperties() => PropertyEditorHelper.RefreshProperties(this);

        private void AddHeader()
        {
            var header = ComponentPool.Get<StyleHeaderPanel>(PropertiesPanel, "Header");
            header.Init(this, Manager.Style.StyleType.Filler, false);
            header.OnSaveTemplate += SaveTemplate;
            header.OnCopy += CopyStyle;
            header.OnPaste += PasteStyle;
            header.OnReset += ResetStyle;
            header.OnApplySameStyle += ApplyStyleSameStyle;
            header.OnApplySameType += ApplyStyleSameType;
            header.OnSelectTemplate += SelectTemplate;
        }
        private void AddStyleTypeProperty()
        {
            Style = ComponentPool.Get<FillerStylePropertyPanel>(PropertiesPanel, nameof(Style));
            Style.Label = IMT.Localize.Editor_Style;
            Style.Init();
            Style.UseWheel = true;
            Style.WheelTip = true;
            Style.SelectedObject = EditObject.Style.Value.Type;
            Style.OnSelectObjectChanged += StyleChanged;
        }

        private void AddStyleProperties()
        {
            this.AddProperties();

            foreach (var property in (this as IPropertyContainer).StyleProperties)
            {
                if (property is ColorPropertyPanel colorProperty && colorProperty.name == nameof(Manager.Style.Color))
                    colorProperty.OnValueChanged += (Color32 c) => RefreshSelectedItem();
                else if (property is FillerGuidePropertyPanel guideProperty)
                {
                    guideProperty.OnSelect += (panel) => SelectGuide(panel);
                    guideProperty.OnEnter += HoverGuide;
                    guideProperty.OnLeave += LeaveGuide;
                }
            }
        }

        #endregion

        #region STYLE CHANGE

        private void StyleChanged(Style.StyleType style)
        {
            if (style == EditObject.Style.Value.Type)
                return;

            var newStyle = SingletonManager<StyleTemplateManager>.Instance.GetDefault<BaseFillerStyle>(style);
            EditObject.Style.Value.CopyTo(newStyle);
            EditObject.Style.Value = newStyle;

            AfterStyleChanged();
        }
        private void AfterStyleChanged()
        {
            RefreshSelectedItem();
            PropertiesPanel.StopLayout();
            this.ClearProperties();
            AddStyleProperties();
            PropertiesPanel.StartLayout();
        }

        private void ApplyStyle(BaseFillerStyle style)
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
            if (template.Style is BaseFillerStyle style)
                ApplyStyle(style);
        }
        private void CopyStyle() => Tool.ToStyleBuffer(Manager.Style.StyleType.Filler, EditObject.Style.Value);
        private void PasteStyle()
        {
            if (Tool.FromStyleBuffer<BaseFillerStyle>(Manager.Style.StyleType.Filler, out var style))
                ApplyStyle(style);
        }
        private void ResetStyle() => ApplyStyle(Manager.Style.GetDefault<BaseFillerStyle>(EditObject.Style.Value.Type));
        private void ApplyStyleSameStyle()
        {
            foreach (var filler in Marking.Fillers)
            {
                if (filler != EditObject && filler.Style.Value.Type == EditObject.Style.Value.Type)
                    filler.Style.Value = EditObject.Style.Value.CopyStyle();
            }

            RefreshEditor();
            ItemsPanel.RefreshItems();
        }
        private void ApplyStyleSameType()
        {
            foreach (var filler in Marking.Fillers)
            {
                if (filler != EditObject)
                    filler.Style.Value = EditObject.Style.Value.CopyStyle();
            }

            RefreshEditor();
            ItemsPanel.RefreshItems();
        }

        public void HoverGuide(FillerGuidePropertyPanel.SelectGuideButton selectButton) => HoverGuideSelectButton = selectButton;
        public void LeaveGuide(FillerGuidePropertyPanel.SelectGuideButton selectButton) => HoverGuideSelectButton = null;
        public bool SelectGuide(FillerGuidePropertyPanel.SelectGuideButton selectButton) => SelectGuide(selectButton, null);
        public bool SelectGuide(FillerGuidePropertyPanel.SelectGuideButton selectButton, Func<Event, bool> afterAction)
        {
            if (Tool.Mode == FillerGuideToolMode && selectButton == FillerGuideToolMode.SelectButton)
            {
                Tool.SetDefaultMode();
                return true;
            }
            else
            {
                Tool.SetMode(FillerGuideToolMode);
                FillerGuideToolMode.Contour = EditObject.Contour;
                FillerGuideToolMode.SelectButton = selectButton;
                FillerGuideToolMode.AfterSelectButton = afterAction;
                selectButton.Focus();
                return false;
            }
        }

        public override void Render(RenderManager.CameraInfo cameraInfo)
        {
            ItemsPanel.HoverObject?.Render(new OverlayData(cameraInfo) { Color = Colors.Hover });

            if (HoverGuideSelectButton != null)
            {
                var guide = EditObject.Contour.GetGuide(HoverGuideSelectButton.Value.a, HoverGuideSelectButton.Value.b, HoverGuideSelectButton.Other.a, HoverGuideSelectButton.Other.b);
                guide.Render(new OverlayData(cameraInfo) { Color = Colors.Hover });
            }
        }

        #endregion
    }
    public class FillerItemsPanel : ItemsPanel<FillerItem, MarkingFiller>
    {
        public override int Compare(MarkingFiller x, MarkingFiller y) => 0;
    }
    public class FillerItem : EditItem<MarkingFiller, StyleIcon>
    {
        public override void Refresh()
        {
            base.Refresh();

            Icon.Type = EditObject.Style.Value.Type;
            Icon.StyleColor = EditObject.Style.Value is IColorStyle ? EditObject.Style.Value.Color : Color.white;
        }
    }

    public class FillerGuideToolMode : BasePanelMode<FillerEditor, FillerGuidePropertyPanel.SelectGuideButton, FillerGuide>
    {
        protected override FillerGuide Hover => throw new NotImplementedException();
        protected override bool IsHover => throw new NotImplementedException();

        private IFillerVertex FirstPoint { get; set; }
        private bool IsFirstSelected => FirstPoint != null;

        public FillerContour Contour { get; set; }
        private PointsSelector<IFillerVertex> PointsSelector { get; set; }
        private LinesSelector<GuideBound> LineSelector { get; set; }

        protected override void OnSetButton()
        {
            FirstPoint = null;
            PointsSelector = GetPointsSelector();
            LineSelector = new LinesSelector<GuideBound>(Contour.TrajectoriesProcessed.Select((t, i) => new GuideBound(t, 0.5f, i)), Colors.Orange);
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
            SelectButton.Value = new FillerGuide(a % Contour.ProcessedCount, b % Contour.ProcessedCount);
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
                foreach (var part in Contour.RawEdges)
                {
                    if (part.IsPoint)
                        part.Render(overlayData);
                }
                LineSelector.Render(cameraInfo, !(PointsSelector.IsHoverGroup || PointsSelector.IsHoverPoint));
            }
            PointsSelector.Render(cameraInfo);
        }

        private class GuideBound : TrajectoryBound
        {
            public int Index { get; }
            public GuideBound(ITrajectory trajectory, float size, int index) : base(trajectory, size)
            {
                Index = index;
            }
        }
    }
}
