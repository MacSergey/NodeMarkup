using ColossalFramework.UI;
using NodeMarkup.Manager;
using NodeMarkup.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public class FillerEditor : Editor<FillerItem, MarkupFiller, StyleIcon>
    {
        private static FillerStyle Buffer { get; set; }

        public override string Name => NodeMarkup.Localize.FillerEditor_Fillers;
        public override string EmptyMessage => string.Format(NodeMarkup.Localize.FillerEditor_EmptyMessage, NodeMarkupTool.AddFillerShortcut.ToString());

        public StylePropertyPanel Style { get; private set; }
        private StyleHeaderPanel Header { get; set; }
        private List<UIComponent> StyleProperties { get; set; } = new List<UIComponent>();

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
            Header = SettingsPanel.AddUIComponent<StyleHeaderPanel>();
            Header.Init(Manager.Style.StyleType.Filler, false);
            Header.OnSaveTemplate += OnSaveTemplate;
            Header.OnSelectTemplate += OnSelectTemplate;
            Header.OnCopy += CopyStyle;
            Header.OnPaste += PasteStyle;
        }
        private void AddStyleTypeProperty()
        {
            Style = SettingsPanel.AddUIComponent<FillerStylePropertyPanel>();
            Style.Text = NodeMarkup.Localize.LineEditor_Style;
            Style.Init();
            Style.SelectedObject = EditObject.Style.Type;
            Style.OnSelectObjectChanged += StyleChanged;
        }
        private void AddStyleProperties()
        {
            StyleProperties = EditObject.Style.GetUIComponents(EditObject, SettingsPanel);
            if (StyleProperties.FirstOrDefault() is ColorPropertyPanel colorProperty)
                colorProperty.OnValueChanged += (Color32 c) => RefreshItem();
        }
        private void StyleChanged(Style.StyleType style)
        {
            if (style == EditObject.Style.Type)
                return;

            var newStyle = TemplateManager.GetDefault<FillerStyle>(style);
            EditObject.Style.CopyTo(newStyle);

            EditObject.Style = newStyle;

            RefreshItem();
            ClearStyleProperties();
            AddStyleProperties();
        }

        private void OnSaveTemplate()
        {
            if (TemplateManager.AddTemplate(EditObject.Style, out StyleTemplate template))
                NodeMarkupPanel.EditTemplate(template);
        }
        private void ApplyStyle(FillerStyle style)
        {
            var newStyle = style.CopyFillerStyle();

            newStyle.MedianOffset = EditObject.Style.MedianOffset;
            if (newStyle is IRotateFiller newSimple && EditObject.Style is IRotateFiller oldSimple)
            {
                newSimple.Angle = oldSimple.Angle;
            }

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
        private void CopyStyle()
        {
                Buffer = EditObject.Style.CopyFillerStyle();
        }
        private void PasteStyle()
        {
            if (Buffer is FillerStyle style)
                ApplyStyle(style);
        }
        private void ClearStyleProperties()
        {
            foreach (var property in StyleProperties)
            {
                SettingsPanel.RemoveUIComponent(property);
                Destroy(property);
            }
        }


        public override void Render(RenderManager.CameraInfo cameraInfo)
        {
            if (IsHoverItem)
            {
                foreach (var trajectory in HoverItem.Object.Trajectories)
                    NodeMarkupTool.RenderTrajectory(cameraInfo, MarkupColors.White, trajectory, 0.2f);
            }
        }
        private void RefreshItem() => SelectItem.Refresh();
        protected override void OnObjectDelete(MarkupFiller filler) => Markup.RemoveFiller(filler);
    }
    public class FillerItem : EditableItem<MarkupFiller, StyleIcon>
    {
        public override string DeleteCaptionDescription => NodeMarkup.Localize.FillerEditor_DeleteCaptionDescription;
        public override string DeleteMessageDescription => NodeMarkup.Localize.FillerEditor_DeleteMessageDescription;
        public override void Init() => Init(true, true);

        protected override void OnObjectSet() => SetIcon();
        public override void Refresh()
        {
            base.Refresh();
            SetIcon();
        }
        private void SetIcon()
        {
            Icon.Type = Object.Style.Type;
            Icon.StyleColor = Object.Style.Color;
        }
    }
}
