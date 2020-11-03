using ColossalFramework.UI;
using NodeMarkup.Tools;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI
{
    public abstract class EditorItem : UIPanel
    {
        protected virtual float DefaultHeight => 30;

        public virtual void Init() => Init(null);
        public virtual void DeInit() { }
        public void Init(float? height = null)
        {
            if (parent is UIScrollablePanel scrollablePanel)
                width = scrollablePanel.width - scrollablePanel.autoLayoutPadding.horizontal;
            else if (parent is UIPanel panel)
                width = panel.width - panel.autoLayoutPadding.horizontal;
            else
                width = parent.width;

            this.height = height ?? DefaultHeight;
        }

        protected UIButton AddButton(UIComponent parent)
        {
            var button = parent.AddUIComponent<UIButton>();
            button.SetDefaultStyle();
            return button;
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
        public bool EnableControl
        {
            get => Control.isEnabled;
            set => Control.isEnabled = value;
        }

        public EditorPropertyPanel()
        {
            Label = AddUIComponent<UILabel>();
            Label.textScale = 0.8f;
            Label.disabledTextColor = new Color32(160, 160, 160, 255);

            Control = AddUIComponent<UIPanel>();
            Control.autoLayoutDirection = LayoutDirection.Horizontal;
            Control.autoLayoutStart = LayoutStart.TopRight;
            Control.autoLayoutPadding = new RectOffset(5, 0, 0, 0);

            Control.eventSizeChanged += ControlSizeChanged;
        }
        public override void Init()
        {
            base.Init();
            RefreshContent();
        }
        public override void DeInit()
        {
            Text = string.Empty;
            isEnabled = true;
            Control.isEnabled = true;
        }
        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            Label.relativePosition = new Vector2(0, (height - Label.height) / 2);
            Control.size = size;
        }

        private void ControlSizeChanged(UIComponent component, Vector2 value) => RefreshContent();
        protected void RefreshContent()
        {
            Control.autoLayout = true;
            Control.autoLayout = false;

            foreach (var item in Control.components)
                item.relativePosition = new Vector2(item.relativePosition.x, (Control.size.y - item.size.y) / 2);
        }

        protected FieldType AddTextField<ValueType, FieldType>(UIComponent parent)
            where FieldType : UITextField<ValueType>
        {
            var field = parent.AddUIComponent<FieldType>();

            field.atlas = TextureUtil.Atlas;
            field.normalBgSprite = TextureUtil.FieldNormal;
            field.hoveredBgSprite = TextureUtil.FieldHovered;
            field.focusedBgSprite = TextureUtil.FieldNormal;
            field.disabledBgSprite = TextureUtil.FieldDisabled;
            field.selectionSprite = TextureUtil.EmptySprite;

            field.allowFloats = true;
            field.isInteractive = true;
            field.enabled = true;
            field.readOnly = false;
            field.builtinKeyNavigation = true;
            field.cursorWidth = 1;
            field.cursorBlinkTime = 0.45f;
            field.selectOnFocus = true;

            field.textScale = 0.7f;
            field.verticalAlignment = UIVerticalAlignment.Middle;
            field.padding = new RectOffset(0, 0, 6, 0);

            return field;
        }
    }
}
