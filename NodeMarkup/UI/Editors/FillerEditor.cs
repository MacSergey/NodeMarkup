using ColossalFramework.Math;
using ColossalFramework.UI;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static ToolBase;

namespace NodeMarkup.UI.Editors
{
    public class FillerEditor : Editor<FillerItem, MarkupFiller, UIPanel>
    {
        public override string Name => NodeMarkup.Localize.FillerEditor_Fillers;

        private ButtonPanel AddButton { get; set; }
        private MarkupFiller Filler { get; set; }

        private bool IsSelectFillerMode { get; set; } = false;
        public List<IFillerVertex> SupportPoints { get; } = new List<IFillerVertex>();
        private IFillerVertex HoverSupportPoint { get; set; }
        private bool IsHoverSupportPoint => IsSelectFillerMode && HoverSupportPoint != null;

        public StylePropertyPanel Style { get; private set; }

        private List<UIComponent> StyleProperties { get; } = new List<UIComponent>();

        public FillerEditor()
        {
            SettingsPanel.autoLayoutPadding = new RectOffset(10, 10, 0, 0);
        }
        public override void UpdateEditor()
        {
            base.UpdateEditor();
            AddAddButton();
        }
        protected override void FillItems()
        {
            foreach (var filler in Markup.Fillers)
            {
                AddItem(filler);
            }
        }
        protected override void OnObjectSelect()
        {
            AddHeader();
            AddStyleTypeProperty();
            AddStyleProperties();
        }

        private void AddHeader()
        {
            var header = SettingsPanel.AddUIComponent<StyleHeaderPanel>();
            header.AddRange(TemplateManager.Templates);
            header.Init(false);
            //header.OnSaveTemplate += OnSaveTemplate;
            //header.OnSelectTemplate += OnSelectTemplate;
        }
        private void AddStyleTypeProperty()
        {
            Style = SettingsPanel.AddUIComponent<FillerStylePropertyPanel>();
            Style.Text = NodeMarkup.Localize.LineEditor_Style;
            Style.Init();
            Style.SelectedObject = EditObject.Style.Type;
            //Style.OnSelectObjectChanged += StyleChanged;
        }
        private void AddStyleProperties()
        {
            AddColorProperty();
            AddWidthProperty();
            AddStyleAdditionalProperties();
        }
        private void AddColorProperty()
        {
            var colorProperty = SettingsPanel.AddUIComponent<ColorPropertyPanel>();
            colorProperty.Text = NodeMarkup.Localize.LineEditor_Color;
            colorProperty.Init();
            colorProperty.Value = EditObject.Style.Color;
            colorProperty.OnValueChanged += ColorChanged;
            StyleProperties.Add(colorProperty);
        }
        private void AddWidthProperty()
        {
            var widthProperty = SettingsPanel.AddUIComponent<FloatPropertyPanel>();
            widthProperty.Text = NodeMarkup.Localize.LineEditor_Width;
            widthProperty.UseWheel = true;
            widthProperty.WheelStep = 0.01f;
            widthProperty.CheckMin = true;
            widthProperty.MinValue = 0.05f;
            widthProperty.Init();
            widthProperty.Value = EditObject.Style.Width;
            widthProperty.OnValueChanged += WidthChanged;
            //widthProperty.OnHover += PropertyHover;
            //widthProperty.OnLeave += PropertyLeave;
            StyleProperties.Add(widthProperty);
        }
        private void AddStyleAdditionalProperties()
        {
            if (EditObject.Style is IStrokeFiller strokeStyle)
            {
                var stepProperty = SettingsPanel.AddUIComponent<FloatPropertyPanel>();
                stepProperty.Text = "Step";
                stepProperty.UseWheel = true;
                stepProperty.WheelStep = 0.1f;
                stepProperty.CheckMin = true;
                stepProperty.MinValue = EditObject.Style.Width;
                stepProperty.Init();
                stepProperty.Value = strokeStyle.Step;
                stepProperty.OnValueChanged += StepChanged;
                StyleProperties.Add(stepProperty);

                var angleProperty = SettingsPanel.AddUIComponent<FloatPropertyPanel>();
                angleProperty.Text = "Angle";
                angleProperty.UseWheel = true;
                angleProperty.WheelStep = 1f;
                angleProperty.CheckMin = true;
                angleProperty.MinValue = -90;
                angleProperty.CheckMax = true;
                angleProperty.MaxValue = 90;
                angleProperty.Init();
                angleProperty.Value = strokeStyle.Angle;
                angleProperty.OnValueChanged += AngleChanged;
                StyleProperties.Add(angleProperty);

                var offsetProperty = SettingsPanel.AddUIComponent<FloatPropertyPanel>();
                offsetProperty.Text = "Offset";
                offsetProperty.UseWheel = true;
                offsetProperty.WheelStep = 0.1f;
                offsetProperty.CheckMin = true;
                offsetProperty.MinValue = 0f;
                offsetProperty.Init();
                offsetProperty.Value = strokeStyle.Offset;
                offsetProperty.OnValueChanged += OffsetChanged;
                StyleProperties.Add(offsetProperty);
            }
        }

        private void ColorChanged(Color32 color) => EditObject.Style.Color = color;
        private void WidthChanged(float value) => EditObject.Style.Width = value;
        private void StepChanged(float value) => (EditObject.Style as IStrokeFiller).Step = value;
        private void AngleChanged(float value) => (EditObject.Style as IStrokeFiller).Angle = value;
        private void OffsetChanged(float value) => (EditObject.Style as IStrokeFiller).Offset = value;

        private void AddAddButton()
        {
            AddButton = SettingsPanel.AddUIComponent<ButtonPanel>();
            AddButton.Text = "Add Filler";
            AddButton.Init();
            AddButton.OnButtonClick += AddButtonClick;
        }

        private void AddButtonClick()
        {
            NodeMarkupPanel.StartEditorAction(this, out bool isAccept);
            if (isAccept)
            {
                Filler = new MarkupFiller(Markup, FillerStyle.FillerType.Stroke);
                CalculateSupportPoints();
                IsSelectFillerMode = true;
            }
        }

        public override void OnUpdate()
        {
            if (!UIView.IsInsideUI() && Cursor.visible)
            {
                Ray ray = Camera.main.ScreenPointToRay(NodeMarkupTool.MousePosition);

                foreach (var supportPoint in SupportPoints)
                {
                    if (supportPoint.IsIntersect(ray))
                    {
                        HoverSupportPoint = supportPoint;
                        return;
                    }
                }
            }

            HoverSupportPoint = null;
        }
        public override void OnPrimaryMouseClicked(Event e, out bool isDone)
        {
            if (IsHoverSupportPoint)
            {
                Filler.Add(HoverSupportPoint);
                if (Filler.IsDone)
                {
                    isDone = true;
                    Markup.AddFiller(Filler);
                    NodeMarkupPanel.EditFiller(Filler);
                    NodeMarkupPanel.EndEditorAction();
                    return;
                }
                CalculateSupportPoints();
            }
            isDone = false;
        }
        public override void OnSecondaryMouseClicked(out bool isDone)
        {
            if (Filler.VertexCount == 0)
            {
                NodeMarkupPanel.EndEditorAction();
                isDone = true;
            }
            else
            {
                Filler.Remove();
                CalculateSupportPoints();
                isDone = false;
            }
        }
        private void CalculateSupportPoints()
        {
            SupportPoints.Clear();
            SupportPoints.AddRange(Filler.GetNextСandidates());
        }
        public override void Render(RenderManager.CameraInfo cameraInfo)
        {
            if (IsSelectFillerMode)
            {
                RenderFillerLines(Filler, cameraInfo);
                RenderFillerBounds(cameraInfo);
                RenderConnectLine(cameraInfo);
                if (IsHoverSupportPoint)
                    NodeMarkupTool.RenderManager.OverlayEffect.DrawCircle(cameraInfo, Color.white, HoverSupportPoint.Position, 1f, -1f, 1280f, false, true);
            }
            else if (IsHoverItem)
                RenderFillerLines(HoverItem.Object, cameraInfo);
        }
        private void RenderFillerBounds(RenderManager.CameraInfo cameraInfo)
        {
            foreach (var supportPoint in SupportPoints)
            {
                NodeMarkupTool.RenderManager.OverlayEffect.DrawCircle(cameraInfo, Color.red, supportPoint.Position, 0.5f, -1f, 1280f, false, true);
            }
        }
        private void RenderFillerLines(MarkupFiller filler, RenderManager.CameraInfo cameraInfo)
        {
            var color = IsHoverSupportPoint && HoverSupportPoint.Equals(Filler.First) ? Color.green : Color.white;
            foreach (var part in filler.Parts)
            {
                var bezier = part.GetTrajectory();
                NodeMarkupTool.RenderManager.OverlayEffect.DrawBezier(cameraInfo, color, bezier, 0.5f, 0f, 0f, -1f, 1280f, false, true);

            }
        }
        private void RenderConnectLine(RenderManager.CameraInfo cameraInfo)
        {
            if (Filler.VertexCount == 0)
                return;

            Bezier3 bezier;
            Color color;

            if (IsHoverSupportPoint)
            {
                var linePart = Filler.GetFillerLine(Filler.Last, HoverSupportPoint);
                bezier = linePart.GetTrajectory();

                color = Color.green;
            }
            else
            {
                RaycastInput input = new RaycastInput(NodeMarkupTool.MouseRay, NodeMarkupTool.MouseRayLength);
                NodeMarkupTool.RayCast(input, out RaycastOutput output);

                bezier.a = Filler.Last.Position;
                bezier.b = output.m_hitPos;
                bezier.c = Filler.Last.Position;
                bezier.d = output.m_hitPos;

                color = Color.white;
            }

            NodeMarkupTool.RenderManager.OverlayEffect.DrawBezier(cameraInfo, color, bezier, 0.5f, 0f, 0f, -1f, 1280f, false, true);
        }

        public override string GetInfo()
        {
            return base.GetInfo();
        }
        public override void EndEditorAction()
        {
            IsSelectFillerMode = false;
            Filler = null;
        }
        protected override void OnObjectDelete(MarkupFiller filler)
        {
            Markup.RemoveFiller(filler);
        }
    }
    public class FillerItem : EditableItem<MarkupFiller, UIPanel>
    {
        public FillerItem() : base(false, true) { }

        public override string Description => NodeMarkup.Localize.FillerEditor_ItemDescription;
    }
}
