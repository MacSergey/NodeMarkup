using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.UI;
using NodeMarkup.Manager;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
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
        private static TemplateComparer Comparer { get; } = new TemplateComparer();

        public void Fill(Style.StyleType group)
        {
            var templates = SingletonManager<StyleTemplateManager>.Instance.GetTemplates(group).OrderBy(t => t, Comparer).ToArray();
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
                item.Init(null, template);
                item.eventClick += ItemClick;
            }
        }
        private void ItemClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (component is TemplatePopupItem item)
                OnSelectTemplate?.Invoke(item.Object);
        }

        private class TemplateComparer : IComparer<StyleTemplate>
        {
            public int Compare(StyleTemplate x, StyleTemplate y)
            {
                var result = Settings.DefaultTemlatesFirst ? SortByDefault(x, y) : 0;

                if (result == 0)
                {
                    if (Settings.SortApplyType == 0)
                    {
                        if ((result = SortByAuthor(x, y)) == 0)
                            if ((result = SortByType(x, y)) == 0)
                                result = SortByName(x, y);
                    }
                    else if (Settings.SortApplyType == 1)
                    {
                        if ((result = SortByType(x, y)) == 0)
                            result = SortByName(x, y);
                    }
                    else if (Settings.SortApplyType == 2)
                    {
                        if ((result = SortByName(x, y)) == 0)
                            result = SortByType(x, y);
                    }
                }

                return result;


                static int SortByDefault(StyleTemplate x, StyleTemplate y) => -x.IsDefault.CompareTo(y.IsDefault);
                static int SortByAuthor(StyleTemplate x, StyleTemplate y) => (x.Asset?.Author ?? string.Empty).CompareTo(y.Asset?.Author ?? string.Empty);
                static int SortByType(StyleTemplate x, StyleTemplate y) => x.Style.Type.CompareTo(y.Style.Type);
                static int SortByName(StyleTemplate x, StyleTemplate y) => x.Name.CompareTo(y.Name);
            }
        }
    }


    public class TemplatePopupItem : StyleTemplateItem
    {
        public override bool ShowDelete => false;
    }
}
