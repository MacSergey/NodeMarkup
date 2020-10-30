using ColossalFramework.UI;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public class IntersectionTemplateEditor : BaseTemplateEditor<PresetItem, IntersectionTemplate, PresetIcon, PresetGroup, bool, IntersectionTemplateHeaderPanel>
    {
        public override string Name => NodeMarkup.Localize.PresetEditor_Presets;
        public override string EmptyMessage => string.Format(NodeMarkup.Localize.PresetEditor_EmptyMessage, NodeMarkup.Localize.Panel_SaveAsPreset);

        protected override bool GroupingEnabled => false;

        protected override IEnumerable<IntersectionTemplate> GetTemplates() => TemplateManager.IntersectionManager.Templates;
        protected override bool SelectGroup(IntersectionTemplate editableItem) => true;
        protected override string GroupName(bool group) => throw new NotSupportedException();

        protected override void OnObjectSelect()
        {
            base.OnObjectSelect();
            AddApplyButton();
        }
        private void AddApplyButton()
        {
            var applyButton = ComponentPool.Get<ButtonPanel>(SettingsPanel);
            applyButton.Text = NodeMarkup.Localize.PresetEditor_ApplyPreset;
            applyButton.Init();
            applyButton.OnButtonClick += OnApply;
        }
        private void OnApply() => Tool.ApplyPreset(EditObject);
    }

    public class PresetItem : EditableItem<IntersectionTemplate, PresetIcon>
    {
        public override bool ShowDelete => !Object.IsAsset;

        public override void Refresh()
        {
            base.Refresh();
            Icon.Count = Object.EntersCount;
        }
    }
    public class PresetIcon : ColorIcon
    {
        protected UILabel CountLabel { get; }
        public int Count { set => CountLabel.text = value.ToString(); }
        public PresetIcon()
        {
            CountLabel = AddUIComponent<UILabel>();
            CountLabel.textColor = Color.white;
            CountLabel.textScale = 0.7f;
            CountLabel.relativePosition = new Vector3(0, 0);
            CountLabel.autoSize = false;
            CountLabel.textAlignment = UIHorizontalAlignment.Center;
            CountLabel.verticalAlignment = UIVerticalAlignment.Middle;
            CountLabel.padding = new RectOffset(0, 0, 5, 0);
        }
        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            if (CountLabel != null)
                CountLabel.size = size;
        }
    }
    public class PresetGroup : EditableGroup<bool, PresetItem, IntersectionTemplate, PresetIcon> { }
}
