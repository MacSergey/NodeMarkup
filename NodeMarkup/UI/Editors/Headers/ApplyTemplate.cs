using ColossalFramework.UI;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public abstract class ApplyHeaderButton<Item, PopupPanelType, PopupItem, IconType, OrderKey> : HeaderPopupButton<PopupPanelType>
        where PopupPanelType : ApplyPopupPanel<Item, PopupItem, IconType>
        where PopupItem : EditableItem<Item, IconType>
        where Item : Template
        where IconType : UIComponent
    {
        protected Action<Item> OnSelect { get; set; }
        protected abstract Func<Item, bool> Selector { get; }
        protected abstract Func<Item, OrderKey> Order { get; }

        public void Init(Action<Item> onSelectTemplate) => OnSelect = onSelectTemplate;
        public void DeInit() => OnSelect = null;
        protected override void OnOpenPopup()
        {
            Popup.Fill(Selector, Order);
            Popup.OnSelectTemplate += PopupOnSelectTemplate;
        }
        private void PopupOnSelectTemplate(Item item)
        {
            ClosePopup();
            OnSelect?.Invoke(item);
        }
    }


    public class ApplyTemplateHeaderButton : ApplyHeaderButton<StyleTemplate, ApplyTemplatePopupPanel, TemplatePopupItem, TemplateIcon, bool>
    {
        private Style.StyleType StyleGroup { get; set; }
        protected override Func<StyleTemplate, bool> Selector => (t) => (t.Style.Type & StyleGroup & Style.StyleType.GroupMask) != 0;
        protected override Func<StyleTemplate, bool> Order => t => t.IsDefault;
        public void Init(Style.StyleType styleGroup, Action<StyleTemplate> onSelectTemplate)
        {
            Init(onSelectTemplate);
            StyleGroup = styleGroup & Style.StyleType.GroupMask;
        }
    }

    public abstract class ApplyPopupPanel<Item, PopupItem, IconType> : PopupPanel
        where PopupItem : EditableItem<Item, IconType>
        where Item : Template
        where IconType : UIComponent
    {
        public event Action<Item> OnSelectTemplate;
        protected abstract string EmptyText {get;}
        public void Fill<Key>(Func<Item, bool> selector, Func<Item, Key>  order)
        {
            var templates = GetItems(selector).OrderBy(t => order(t)).ToArray();
            if (!templates.Any())
            {
                var emptyLabel = Content.AddUIComponent<UILabel>();
                emptyLabel.text = EmptyText;
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
                var item = Content.AddUIComponent<PopupItem>();
                item.Init(template);
                item.eventClick += ItemClick;
            }
        }
        protected abstract IEnumerable<Item> GetItems(Func<Item, bool> selector);

        private void ItemClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (component is PopupItem item)
                OnSelectTemplate?.Invoke(item.Object);
        }
    }
    public class ApplyTemplatePopupPanel : ApplyPopupPanel<StyleTemplate, TemplatePopupItem, TemplateIcon>
    {
        protected override string EmptyText => NodeMarkup.Localize.HeaderPanel_NoTemplates;
        protected override IEnumerable<StyleTemplate> GetItems(Func<StyleTemplate, bool> selector) => TemplateManager.StyleManager.Templates.Where(t => selector(t));
    }


    public class TemplatePopupItem : TemplateItem
    {
        public override bool ShowDelete => false;
    }
}
