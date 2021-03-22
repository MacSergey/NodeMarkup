using ColossalFramework.UI;
using ModsCommon.UI;
using NodeMarkup.Manager;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public class ApplyTemplateHeaderButton : HeaderPopupButton<ApplyTemplatePopupPanel>
    {
        protected override UITextureAtlas IconAtlas => TextureUtil.Atlas;

        protected Action<StyleTemplate> OnSelect { get; set; }

        private Style.StyleType StyleGroup { get; set; }
        public void Init(Style.StyleType styleGroup, Action<StyleTemplate> onSelectTemplate)
        {
            StyleGroup = styleGroup.GetGroup();
            OnSelect = onSelectTemplate;
        }
        public void DeInit() => OnSelect = null;
        protected override void OnOpenPopup()
        {
            Popup.Fill(StyleGroup);
            Popup.OnSelectTemplate += PopupOnSelectTemplate;
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
            var templates = TemplateManager.StyleManager.GetTemplates(group).OrderByDescending(t => t.IsDefault).ThenBy(t => t.Style.Type).ThenBy(t => t.Name).ToArray();
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
                var item = Content.AddUIComponent<TemplatePopupItem>();
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
