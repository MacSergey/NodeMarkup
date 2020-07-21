using ColossalFramework.UI;
using NodeMarkup.Manager;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public class TemplateEditor : Editor<TemplateItem, StyleTemplate, DefaultTemplateIcon>
    {
        public override string Name => NodeMarkup.Localize.TemplateEditor_Templates;
        private List<UIComponent> StyleProperties { get; } = new List<UIComponent>();
        private StringPropertyPanel NameProperty { get; set; }
        private TemplateHeaderPanel HeaderPanel { get; set; }

        public TemplateEditor()
        {
            SettingsPanel.autoLayoutPadding = new RectOffset(10, 10, 0, 0);
        }

        protected override void FillItems()
        {
#if STOPWATCH
            var sw = Stopwatch.StartNew();
#endif
            foreach (var templates in TemplateManager.Templates)
            {
                var item = AddItem(templates);
            }
#if STOPWATCH
            Logger.LogDebug($"{nameof(TemplateEditor)}.{nameof(FillItems)}: {sw.ElapsedMilliseconds}ms");
#endif
        }

        protected override void OnObjectSelect()
        {
#if STOPWATCH
            var sw = Stopwatch.StartNew();
#endif
            AddHeader();
            AddTemplateName();
            //AddStyleProperty();
            AddColorProperty();
            AddWidthProperty();
            AddStyleAdditionalProperties();
#if STOPWATCH
            Logger.LogDebug($"{nameof(TemplateEditor)}.{nameof(OnObjectSelect)}: {sw.ElapsedMilliseconds}ms");
#endif
        }
        private void AddHeader()
        {
            HeaderPanel = SettingsPanel.AddUIComponent<TemplateHeaderPanel>();
            HeaderPanel.Init(EditObject.IsDefault());
            HeaderPanel.OnSetAsDefault += ToggleAsDefault;
        }
        private void AddTemplateName()
        {
            NameProperty = SettingsPanel.AddUIComponent<StringPropertyPanel>();
            NameProperty.Text = NodeMarkup.Localize.TemplateEditor_Name;
            NameProperty.FieldWidth = 230;
            NameProperty.UseWheel = false;
            NameProperty.Init();
            NameProperty.Value = EditObject.Name;
            NameProperty.OnValueChanged += NameSubmitted;
        }
        //private void AddStyleProperty()
        //{
        //    var styleProperty = default(StylePropertyPanel);

        //    switch (EditObject.Style)
        //    {
        //        case ISimpleLine _:
        //            styleProperty = SettingsPanel.AddUIComponent<SimpleStylePropertyPanel>();
        //            break;
        //        case IStopLine _:
        //            styleProperty = SettingsPanel.AddUIComponent<StopStylePropertyPanel>();
        //            break;
        //        case IFillerStyle _:
        //            styleProperty = SettingsPanel.AddUIComponent<FillerStylePropertyPanel>();
        //            break;
        //        default:
        //            return;
        //    }

        //    styleProperty.Text = NodeMarkup.Localize.TemplateEditor_Style;
        //    styleProperty.Init();
        //    styleProperty.SelectedObject = EditObject.Style.Type;
        //    styleProperty.OnSelectObjectChanged += StyleChanged;
        //}
        private void AddColorProperty()
        {
            var colorProperty = SettingsPanel.AddUIComponent<ColorPropertyPanel>();
            colorProperty.Text = NodeMarkup.Localize.TemplateEditor_Color;
            colorProperty.Init();
            colorProperty.Value = EditObject.Style.Color;
            colorProperty.OnValueChanged += ColorChanged;
        }
        private void AddWidthProperty()
        {
            var widthProperty = SettingsPanel.AddUIComponent<FloatPropertyPanel>();
            widthProperty.Text = NodeMarkup.Localize.TemplateEditor_Width;
            widthProperty.UseWheel = true;
            widthProperty.WheelStep = 0.01f;
            widthProperty.CheckMin = true;
            widthProperty.MinValue = 0.05f;
            widthProperty.Init();
            widthProperty.Value = EditObject.Style.Width;
            widthProperty.OnValueChanged += WidthChanged;
        }
        private void AddStyleAdditionalProperties()
        {
            if (EditObject.Style is IDashedLine dashedStyle)
            {
                var dashLengthProperty = SettingsPanel.AddUIComponent<FloatPropertyPanel>();
                dashLengthProperty.Text = NodeMarkup.Localize.TemplateEditor_DashedLength;
                dashLengthProperty.UseWheel = true;
                dashLengthProperty.WheelStep = 0.1f;
                dashLengthProperty.CheckMin = true;
                dashLengthProperty.MinValue = 0.1f;
                dashLengthProperty.Init();
                dashLengthProperty.Value = dashedStyle.DashLength;
                dashLengthProperty.OnValueChanged += DashLengthChanged;
                StyleProperties.Add(dashLengthProperty);

                var spaceLengthProperty = SettingsPanel.AddUIComponent<FloatPropertyPanel>();
                spaceLengthProperty.Text = NodeMarkup.Localize.TemplateEditor_SpaceLength;
                spaceLengthProperty.UseWheel = true;
                spaceLengthProperty.WheelStep = 0.1f;
                spaceLengthProperty.CheckMin = true;
                spaceLengthProperty.MinValue = 0.1f;
                spaceLengthProperty.Init();
                spaceLengthProperty.Value = dashedStyle.SpaceLength;
                spaceLengthProperty.OnValueChanged += SpaceLengthChanged;
                StyleProperties.Add(spaceLengthProperty);
            }
            if (EditObject.Style is IDoubleLine doubleStyle)
            {
                var offsetProperty = SettingsPanel.AddUIComponent<FloatPropertyPanel>();
                offsetProperty.Text = NodeMarkup.Localize.TemplateEditor_Offset;
                offsetProperty.UseWheel = true;
                offsetProperty.WheelStep = 0.1f;
                offsetProperty.Init();
                offsetProperty.Value = doubleStyle.Offset;
                offsetProperty.OnValueChanged += OffsetChanged;
                StyleProperties.Add(offsetProperty);
            }
            if (EditObject.Style is IAsymLine asymStyle)
            {
                var invertProperty = SettingsPanel.AddUIComponent<BoolPropertyPanel>();
                invertProperty.Text = NodeMarkup.Localize.TemplateEditor_Invert;
                invertProperty.Init();
                invertProperty.Value = asymStyle.Invert;
                invertProperty.OnValueChanged += InvertChanged;
                StyleProperties.Add(invertProperty);
            }
            if(EditObject.Style is IStrokeFiller fillerStyle)
            {
                var stepProperty = SettingsPanel.AddUIComponent<FloatPropertyPanel>();
                stepProperty.Text = "Step";
                stepProperty.UseWheel = true;
                stepProperty.WheelStep = 0.1f;
                stepProperty.CheckMin = true;
                stepProperty.MinValue = EditObject.Style.Width;
                stepProperty.Init();
                stepProperty.Value = fillerStyle.Step;
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
                angleProperty.Value = fillerStyle.Angle;
                angleProperty.OnValueChanged += AngleChanged;
                StyleProperties.Add(angleProperty);

                var offsetProperty = SettingsPanel.AddUIComponent<FloatPropertyPanel>();
                offsetProperty.Text = "Offset";
                offsetProperty.UseWheel = true;
                offsetProperty.WheelStep = 0.1f;
                offsetProperty.CheckMin = true;
                offsetProperty.MinValue = 0f;
                offsetProperty.Init();
                offsetProperty.Value = fillerStyle.Offset;
                offsetProperty.OnValueChanged += FillerOffsetChanged;
                StyleProperties.Add(offsetProperty);
            }
        }
        //private void ClearStyleProperties()
        //{
        //    foreach (var property in StyleProperties)
        //    {
        //        SettingsPanel.RemoveUIComponent(property);
        //        Destroy(property);
        //    }

        //    StyleProperties.Clear();
        //}
        private void NameSubmitted(string value)
        {
            EditObject.Name = value;
            NameProperty.Value = EditObject.Name;
            SelectItem.Refresh();
        }
        private void ColorChanged(Color32 color) => EditObject.Style.Color = color;
        //private void StyleChanged(Style.StyleType style)
        //{
        //    var newStyle = LineStyle.GetDefault(style);
        //    newStyle.Color = EditObject.Style.Color;
        //    if (newStyle is IDashedLine newDashed && EditObject.Style is IDashedLine oldDashed)
        //    {
        //        newDashed.DashLength = oldDashed.DashLength;
        //        newDashed.SpaceLength = oldDashed.SpaceLength;
        //    }
        //    if (newStyle is IDoubleLine newDouble && EditObject.Style is IDoubleLine oldDouble)
        //        newDouble.Offset = oldDouble.Offset;

        //    EditObject.Style = newStyle;

        //    ClearStyleProperties();
        //    AddStyleAdditionalProperties();

        //    AsDefaultRefresh();
        //}
        private void WidthChanged(float value) => EditObject.Style.Width = value;
        private void DashLengthChanged(float value) => (EditObject.Style as IDashedLine).DashLength = value;
        private void SpaceLengthChanged(float value) => (EditObject.Style as IDashedLine).SpaceLength = value;
        private void OffsetChanged(float value) => (EditObject.Style as IDoubleLine).Offset = value;
        private void InvertChanged(bool value) => (EditObject.Style as IAsymLine).Invert = value;
        private void StepChanged(float value) => (EditObject.Style as IStrokeFiller).Step = value;
        private void AngleChanged(float value) => (EditObject.Style as IStrokeFiller).Angle = value;
        private void FillerOffsetChanged(float value) => (EditObject.Style as IStrokeFiller).Offset = value;


        private void ToggleAsDefault()
        {
            TemplateManager.ToggleAsDefaultTemplate(EditObject);
            AsDefaultRefresh();
        }
        private void AsDefaultRefresh()
        {
            RefreshItems();
            HeaderPanel.Init(EditObject.IsDefault());
        }

        protected override void OnObjectDelete(StyleTemplate template)
        {
            TemplateManager.DeleteTemplate(template);
        }
    }

    public class TemplateItem : EditableItem<StyleTemplate, DefaultTemplateIcon>
    {
        public override string Description => NodeMarkup.Localize.TemplateEditor_ItemDescription;

        public TemplateItem() : base(true, true) { }

        protected override void OnObjectSet() => SetIsDefault();
        public override void Refresh()
        {
            base.Refresh();
            SetIsDefault();
        }
        private void SetIsDefault() => Icon.IsDefault = Object.IsDefault();
    }
    public class DefaultTemplateIcon : UIPanel
    {
        public bool IsDefault { set => isVisible = value; }
        public DefaultTemplateIcon()
        {
            atlas = NodeMarkupPanel.InGameAtlas;
            backgroundSprite = "ParkLevelStar";
        }
    }
}
