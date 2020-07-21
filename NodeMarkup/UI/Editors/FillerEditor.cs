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

        public StylePropertyPanel Style { get; private set; }

        private List<UIComponent> StyleProperties { get; } = new List<UIComponent>();

        public FillerEditor()
        {
            SettingsPanel.autoLayoutPadding = new RectOffset(10, 10, 0, 0);
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

        public override void Render(RenderManager.CameraInfo cameraInfo)
        {
            if (IsHoverItem)
            {
                foreach (var part in EditObject.Parts)
                {
                    var bezier = part.GetTrajectory();
                    NodeMarkupTool.RenderManager.OverlayEffect.DrawBezier(cameraInfo, Color.white, bezier, 0.5f, 0f, 0f, -1f, 1280f, false, true);

                }
            }
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
