using ColossalFramework.UI;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public class PropertyGroupPanel : UIPanel, IReusable
    {
        
        private static Color32 NormalColor { get; } = new Color32(82, 101, 117, 255);

        protected virtual Color32 Color => NormalColor;

        public PropertyGroupPanel()
        {
            atlas = TextureHelper.InGameAtlas;
            backgroundSprite = "ButtonWhite";
            color = Color;

            autoLayout = true;
            autoFitChildrenVertically = true;
            autoLayoutDirection = LayoutDirection.Vertical;
            autoLayoutPadding = new RectOffset(5, 5, 0, 0);
        }

        public virtual void Init()
        {
            SetSize();
        }

        public virtual void DeInit()
        {
            var components = this.components.ToArray();
            foreach (var component in components)
                ComponentPool.Free(component);
        }

        private void SetSize()
        {
            if (parent is UIScrollablePanel scrollablePanel)
                width = scrollablePanel.width - scrollablePanel.autoLayoutPadding.horizontal;
            else if (parent is UIPanel panel)
                width = panel.width - panel.autoLayoutPadding.horizontal;
            else
                width = parent.width;
        }
        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            foreach (var item in components)
            {
                item.width = width - autoLayoutPadding.horizontal;
            }
        }
    }
}
