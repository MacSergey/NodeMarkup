using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.UI;
using NodeMarkup.Manager;
using NodeMarkup.Utilities;
using System;
using System.Linq;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public class ApplyTemplateHeaderButton : HeaderPopupButton<ApplyTemplatePopupPanel>
    {
        protected Action<StyleTemplate> OnSelect { get; set; }

        private Style.StyleType StyleGroup { get; set; }
        public void Init(Style.StyleType styleGroup, Action<StyleTemplate> onSelectTemplate)
        {
            StyleGroup = styleGroup;
            OnSelect = onSelectTemplate;
        }
        public override void DeInit()
        {
            base.DeInit();
            OnSelect = null;
        }
        protected override void OnPopupOpened()
        {
            Popup.Fill(StyleGroup);
            Popup.OnSelectTemplate += PopupOnSelectTemplate;

            base.OnPopupOpened();
        }
        private void PopupOnSelectTemplate(StyleTemplate template)
        {
            ClosePopup();
            OnSelect?.Invoke(template);
        }
    }
    public class ApplyTemplatePopupPanel : PopupPanel
    {
        public event Action<StyleTemplate> OnSelectTemplate;

        public void Fill(Style.StyleType group)
        {
            var templates = SingletonManager<StyleTemplateManager>.Instance.GetTemplates(group).OrderByDescending(t => t.IsDefault).ThenBy(t => t.Asset?.Author ?? string.Empty).ThenBy(t => t.Style.Type).ThenBy(t => t.Name).ToArray();
            if (!templates.Any())
            {
                var emptyLabel = Content.AddUIComponent<CustomUILabel>();
                emptyLabel.text = NodeMarkup.Localize.HeaderPanel_NoTemplates;
                emptyLabel.textScale = 0.8f;
                emptyLabel.autoSize = false;
                emptyLabel.width = Content.width;
                emptyLabel.autoHeight = true;
                emptyLabel.textAlignment = UIHorizontalAlignment.Center;
                emptyLabel.padding = new RectOffset(0, 0, 5, 5);
                return;
            }

            foreach (var template in templates)
            {
                var item = ComponentPool.Get<TemplatePopupItem>(Content);
                item.Init(template);
                item.eventClick += ItemClick;
            }
        }
        private void ItemClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (component is TemplatePopupItem item)
                OnSelectTemplate?.Invoke(item.Object);
        }
    }


    public class TemplatePopupItem : StyleTemplateItem
    {
        public override bool ShowDelete => false;
    }
}
