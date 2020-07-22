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
            header.AddRange(TemplateManager.GetTemplates(Manager.Style.StyleType.Filler));
            header.Init(false);
            header.OnSaveTemplate += OnSaveTemplate;
            header.OnSelectTemplate += OnSelectTemplate;
        }
        private void AddStyleTypeProperty()
        {
            Style = SettingsPanel.AddUIComponent<FillerStylePropertyPanel>();
            Style.Text = NodeMarkup.Localize.LineEditor_Style;
            Style.Init();
            Style.SelectedObject = EditObject.Style.Type;
            Style.OnSelectObjectChanged += StyleChanged;
        }
        private void AddStyleProperties() => StyleProperties.AddRange(EditObject.Style.GetUIComponents(SettingsPanel));
        private void StyleChanged(Style.StyleType style)
        {
            if (style == EditObject.Style.Type)
                return;

            var newStyle = TemplateManager.GetDefault<FillerStyle>(style);
            newStyle.Color = EditObject.Style.Color;
            newStyle.Width = EditObject.Style.Width;
            if (newStyle is IStrokeFiller newStroke && EditObject.Style is IStrokeFiller oldStroke)
            {
                newStroke.Step = oldStroke.Step;
                newStroke.Angle = oldStroke.Angle;
                newStroke.Offset = oldStroke.Offset;
            }

            EditObject.Style = newStyle;

            ClearStyleProperties();
            AddStyleProperties();
        }

        private void OnSaveTemplate()
        {
            if (TemplateManager.AddTemplate(EditObject.Style, out StyleTemplate template))
                NodeMarkupPanel.EditTemplate(template);
        }
        private void OnSelectTemplate(StyleTemplate template)
        {
            if (template.Style.Copy() is FillerStyle style)
            {
                EditObject.Style = style;
                Style.SelectedObject = EditObject.Style.Type;
                ClearStyleProperties();
                AddStyleProperties();
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


        public override void Render(RenderManager.CameraInfo cameraInfo)
        {
            if (IsHoverItem)
            {
                foreach (var part in HoverItem.Object.Parts)
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
