using ColossalFramework.UI;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public class ApplyTemplateHeaderButton : HeaderPopupButton<TemplatePopupPanel>
    {
        private Style.StyleType StyleGroup { get; set; }
        private Action<StyleTemplate> OnSelectTemplate { get; set; }
        public void Init(Style.StyleType styleGroup, Action<StyleTemplate> onSelectTemplate)
        {
            StyleGroup = styleGroup & Style.StyleType.GroupMask;
            OnSelectTemplate = onSelectTemplate;
        }
        public void DeInit()
        {
            OnSelectTemplate = null;
        }
        protected override void OnOpenPopup()
        {
            Popup.Fill(StyleGroup);
            Popup.OnSelectTemplate += PopupOnSelectTemplate;
        }

        private void PopupOnSelectTemplate(StyleTemplate template)
        {
            ClosePopup();
            OnSelectTemplate?.Invoke(template);
        }
    }

    public class TemplatePopupPanel : PopupPanel
    {
        public event Action<StyleTemplate> OnSelectTemplate;
        public void Fill(Style.StyleType styleGroup)
        {
            var templates = TemplateManager.GetTemplates(styleGroup).ToArray();
            if (!templates.Any())
            {
                var emptyLabel = Content.AddUIComponent<UILabel>();
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
                var item = Content.AddUIComponent<TemplateItem>();
                item.Init(true, false);
                item.name = template.ToString();
                item.Object = template;
                item.eventClick += ItemClick;
            }
        }

        private void ItemClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (component is TemplateItem item)
                OnSelectTemplate?.Invoke(item.Object);
        }
    }
}
