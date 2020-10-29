using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup.UI.Editors
{
    public class IntersectionTemplateEditor : BaseTemplateEditor<PresetItem, IntersectionTemplate, PresetIcon, PresetGroup, bool, TemplateHeaderPanel>
    {
        public override string Name => NodeMarkup.Localize.PresetEditor_Presets;
        public override string EmptyMessage => string.Format(NodeMarkup.Localize.PresetEditor_EmptyMessage, NodeMarkup.Localize.HeaderPanel_SaveAsPreset);

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

    }
    public class PresetIcon : StyleIcon { }
    public class PresetGroup : EditableGroup<bool, PresetItem, IntersectionTemplate, PresetIcon> { }
}
