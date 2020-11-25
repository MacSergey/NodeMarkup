using ColossalFramework.UI;
using ModsCommon.UI;
using IMT.Manager;
using IMT.Tools;
using IMT.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IMT.UI.Editors
{
    public class FillerEditor : Editor<FillerItem, MarkupFiller, StyleIcon>
    {
        protected override bool UseGroupPanel => true;

        private static FillerStyle Buffer { get; set; }

        public override string Name => IMT.Localize.FillerEditor_Fillers;
        public override string EmptyMessage => string.Format(IMT.Localize.FillerEditor_EmptyMessage, NodeMarkupTool.AddFillerShortcut.ToString());

        public StylePropertyPanel Style { get; private set; }
        private List<EditorItem> StyleProperties { get; set; } = new List<EditorItem>();

        protected override void FillItems()
        {
            foreach (var filler in Markup.Fillers)
                AddItem(filler);
        }
        protected override void OnObjectSelect()
        {
            AddHeader();
            AddStyleTypeProperty();
            AddStyleProperties();
        }
        protected override void OnClear()
        {
            Style = null;
            StyleProperties.Clear();
        }

        private void AddHeader()
        {
            var header = ComponentPool.Get<StyleHeaderPanel>(PropertiesPanel);
            header.Init(Manager.Style.StyleType.Filler, OnSelectTemplate, false);
            header.OnSaveTemplate += OnSaveTemplate;
            header.OnCopy += CopyStyle;
            header.OnPaste += PasteStyle;
        }
        private void AddStyleTypeProperty()
        {
            Style = ComponentPool.Get<FillerStylePropertyPanel>(PropertiesPanel);
            Style.Text = IMT.Localize.Editor_Style;
            Style.Init();
            Style.SelectedObject = EditObject.Style.Type;
            Style.OnSelectObjectChanged += StyleChanged;
        }
        private void AddStyleProperties()
        {
            StyleProperties = EditObject.Style.GetUIComponents(EditObject, PropertiesPanel);
            if (StyleProperties.OfType<ColorPropertyPanel>().FirstOrDefault() is ColorPropertyPanel colorProperty)
                colorProperty.OnValueChanged += (Color32 c) => RefreshItem();
        }
        private void StyleChanged(Style.StyleType style)
        {
            if (style == EditObject.Style.Type)
                return;

            var newStyle = TemplateManager.StyleManager.GetDefault<FillerStyle>(style);
            EditObject.Style.CopyTo(newStyle);

            EditObject.Style = newStyle;

            RefreshItem();
            ClearStyleProperties();
            AddStyleProperties();
        }

        private void OnSaveTemplate()
        {
            if (TemplateManager.StyleManager.AddTemplate(EditObject.Style, out StyleTemplate template))
                Panel.EditStyleTemplate(template);
        }
        private void ApplyStyle(FillerStyle style)
        {
            var newStyle = style.CopyFillerStyle();

            newStyle.MedianOffset.Value = EditObject.Style.MedianOffset;
            if (newStyle is IRotateFiller newSimple && EditObject.Style is IRotateFiller oldSimple)
                newSimple.Angle.Value = oldSimple.Angle;

            EditObject.Style = newStyle;
            Style.SelectedObject = EditObject.Style.Type;

            RefreshItem();
            ClearStyleProperties();
            AddStyleProperties();
        }
        private void OnSelectTemplate(StyleTemplate template)
        {
            if (template.Style is FillerStyle style)
                ApplyStyle(style);
        }
        private void CopyStyle() => Buffer = EditObject.Style.CopyFillerStyle();
        private void PasteStyle()
        {
            if (Buffer is FillerStyle style)
                ApplyStyle(style);
        }
        private void ClearStyleProperties()
        {
            foreach (var property in StyleProperties)
                ComponentPool.Free(property);

            StyleProperties.Clear();
        }


        public override void Render(RenderManager.CameraInfo cameraInfo)
        {
            if (IsHoverItem)
                HoverItem.Object.Render(cameraInfo, Colors.Hover);
        }
        private void RefreshItem() => SelectItem.Refresh();
        protected override void OnObjectDelete(MarkupFiller filler) => Markup.RemoveFiller(filler);
    }
    public class FillerItem : EditableItem<MarkupFiller, StyleIcon>
    {
        public override void Refresh()
        {
            base.Refresh();

            Icon.Type = Object.Style.Type;
            Icon.StyleColor = Object.Style.Color;
        }
    }
}
