using ColossalFramework.UI;
using NodeMarkup.Manager;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public class TemplateEditor : Editor<TemplateItem, LineStyleTemplate, DefaultTemplateIcon>
    {
        public override string Name => "Templates";
        private List<UIComponent> StyleProperties { get; } = new List<UIComponent>();
        private StringPropertyPanel NameProperty { get; set; }
        private TemplateHeaderPanel HeaderPanel { get; set; }

        public TemplateEditor()
        {
            SettingsPanel.autoLayoutPadding = new RectOffset(10, 10, 0, 0);
        }

        protected override void FillItems()
        {
            foreach (var templates in TemplateManager.Templates)
            {
                AddItem(templates);
            }
        }

        protected override void OnObjectSelect()
        {
            AddHeader();
            AddTemplateName();
            AddColorProperty();
            AddStyleProperty();
            AddStyleProperties();
        }
        private void AddHeader()
        {
            HeaderPanel = SettingsPanel.AddUIComponent<TemplateHeaderPanel>();
            HeaderPanel.Init(EditObject.IsDefault());
            HeaderPanel.OnDelete += DeleteTemplate;
            HeaderPanel.OnSetAsDefault += ToggleAsDefault;
        }
        private void AddTemplateName()
        {
            NameProperty = SettingsPanel.AddUIComponent<StringPropertyPanel>();
            NameProperty.Text = "Name";
            NameProperty.FieldWidth = 230;
            NameProperty.UseWheel = false;
            NameProperty.Init();
            NameProperty.Value = EditObject.Name;
            NameProperty.OnValueSubmitted += NameSubmitted;
        }
        private void AddColorProperty()
        {
            var colorProperty = SettingsPanel.AddUIComponent<ColorPropertyPanel>();
            colorProperty.Text = "Color";
            colorProperty.Init();
            colorProperty.Value = EditObject.Style.Color;
            colorProperty.OnValueChanged += ColorChanged;
        }
        private void AddStyleProperty()
        {
            var styleProperty = SettingsPanel.AddUIComponent<StylePropertyPanel>();
            styleProperty.Text = "Style";
            styleProperty.Init();
            styleProperty.SelectedObject = EditObject.Style.Type;
            styleProperty.OnSelectObjectChanged += StyleChanged;
        }
        private void AddStyleProperties()
        {
            if (EditObject.Style is IDashedLine dashedStyle)
            {
                var dashLengthProperty = SettingsPanel.AddUIComponent<FloatPropertyPanel>();
                dashLengthProperty.Text = "Dashed lenght";
                dashLengthProperty.UseWheel = true;
                dashLengthProperty.Step = 0.1f;
                dashLengthProperty.Init();
                dashLengthProperty.Value = dashedStyle.DashLength;
                dashLengthProperty.OnValueChanged += DashLengthChanged;
                StyleProperties.Add(dashLengthProperty);

                var spaceLengthProperty = SettingsPanel.AddUIComponent<FloatPropertyPanel>();
                spaceLengthProperty.Text = "Space lenght";
                spaceLengthProperty.UseWheel = true;
                spaceLengthProperty.Step = 0.1f;
                spaceLengthProperty.Init();
                spaceLengthProperty.Value = dashedStyle.SpaceLength;
                spaceLengthProperty.OnValueChanged += SpaceLengthChanged;
                StyleProperties.Add(spaceLengthProperty);
            }
            if (EditObject.Style is IDoubleLine doubleStyle)
            {
                var offsetProperty = SettingsPanel.AddUIComponent<FloatPropertyPanel>();
                offsetProperty.Text = "Offset";
                offsetProperty.UseWheel = true;
                offsetProperty.Step = 0.1f;
                offsetProperty.Init();
                offsetProperty.Value = doubleStyle.Offset;
                offsetProperty.OnValueChanged += OffsetChanged;
                StyleProperties.Add(offsetProperty);
            }
        }
        private void ClearStyleProperties()
        {
            foreach (var property in StyleProperties)
            {
                SettingsPanel.RemoveUIComponent(property);
                Destroy(property);
            }

            StyleProperties.Clear();
        }
        private void NameSubmitted(string value)
        {
            EditObject.Name = value;
            NameProperty.Value = EditObject.Name;
            SelectItem.Refresh();
        }
        private void ColorChanged(Color32 color) => EditObject.Style.Color = color;
        private void StyleChanged(LineStyle.LineType style)
        {
            var newStyle = LineStyle.GetDefault(style);
            newStyle.Color = EditObject.Style.Color;
            if (newStyle is IDashedLine newDashed && EditObject.Style is IDashedLine oldDashed)
            {
                newDashed.DashLength = oldDashed.DashLength;
                newDashed.SpaceLength = oldDashed.SpaceLength;
            }
            if (newStyle is IDoubleLine newDouble && EditObject.Style is IDoubleLine oldDouble)
                newDouble.Offset = oldDouble.Offset;

            EditObject.Style = newStyle;

            ClearStyleProperties();
            AddStyleProperties();

            AsDefaultRefresh();
        }
        private void DashLengthChanged(float value) => (EditObject.Style as IDashedLine).DashLength = value;
        private void SpaceLengthChanged(float value) => (EditObject.Style as IDashedLine).SpaceLength = value;
        private void OffsetChanged(float value) => (EditObject.Style as IDoubleLine).Offset = value;


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

        private void DeleteTemplate()
        {
            TemplateManager.DeleteTemplate(EditObject);
            UpdateEditor();
        }
    }

    public class TemplateItem : EditableItem<LineStyleTemplate, DefaultTemplateIcon> 
    {
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
            atlas = TextureUtil.GetAtlas("Ingame");
            backgroundSprite = "ParkLevelStar";
        }
    }
}
