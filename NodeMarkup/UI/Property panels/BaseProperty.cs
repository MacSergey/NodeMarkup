using ColossalFramework.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI
{
    public abstract class EditorItem : UIPanel
    {
        public virtual void Init()
        {
            if (parent is UIScrollablePanel scrollablePanel)
                width = scrollablePanel.width - scrollablePanel.autoLayoutPadding.horizontal;
            else if (parent is UIPanel panel)
                width = panel.width - panel.autoLayoutPadding.horizontal;
            else
                width = parent.width;

            height = 30;
        }
    }
    public abstract class EditorPropertyPanel : EditorItem
    {
        private UILabel Label { get; set; }
        protected UIPanel Control { get; set; }

        public string Text
        {
            get => Label.text;
            set => Label.text = value;
        }

        public EditorPropertyPanel()
        {
            Label = AddUIComponent<UILabel>();
            Label.textScale = 0.8f;

            Control = AddUIComponent<UIPanel>();
            Control.autoLayout = true;
            Control.autoLayoutDirection = LayoutDirection.Horizontal;
            Control.autoLayoutStart = LayoutStart.TopRight;
            Control.autoLayoutPadding = new RectOffset(5, 0, 0, 0);
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            Label.relativePosition = new Vector2(0, (height - Label.height) / 2);
            Control.size = size;
            Control.autoLayout = true;
            Control.autoLayout = false;

            foreach (var item in Control.components)
            {
                item.relativePosition = new Vector3(item.relativePosition.x, (Control.size.y - item.size.y) / 2);
            }
        }
    }
}
