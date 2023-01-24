﻿using ColossalFramework.UI;
using IMT.Manager;
using IMT.Utilities;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using UnityEngine;

namespace IMT.UI.Editors
{
    public abstract class EditItemBase : CustomUIButton
    {
        public virtual Color32 NormalColor => new Color32(29, 58, 77, 255);
        public virtual Color32 HoveredColor => new Color32(44, 87, 112, 255);
        public virtual Color32 PressedColor => new Color32(51, 100, 132, 255);
        public virtual Color32 FocusColor => new Color32(171, 185, 196, 255);
        public virtual Color32 TextColor => Color.white;

        public bool IsSelect
        {
            get => state == ButtonState.Focused;
            set
            {
                if (IsSelect != value)
                {
                    state = value ? ButtonState.Focused : ButtonState.Normal;
                    SetColors();
                }
            }
        }

        protected CustomUILabel Label { get; set; }
        public string Text
        {
            get => Label.text;
            set => Label.text = value;
        }

        public EditItemBase()
        {
            AddLable();

            clipChildren = true;
            atlas = IMTTextures.Atlas;
            normalBgSprite = IMTTextures.ListItemBackground;
            height = 25;
        }

        private void AddLable()
        {
            Label = AddUIComponent<CustomUILabel>();
            Label.textAlignment = UIHorizontalAlignment.Left;
            Label.verticalAlignment = UIVerticalAlignment.Middle;
            Label.autoSize = false;
            Label.textScale = 0.55f;
            Label.padding = new RectOffset(0, 0, 3, 0);
            Label.autoHeight = true;
            Label.wordWrap = true;
            Label.eventSizeChanged += (c, v) => LabelSizeChanged();
        }
        public virtual void DeInit()
        {
            IsSelect = false;
            isVisible = true;
        }

        protected virtual void SetColors()
        {
            color = NormalColor;
            hoveredColor = HoveredColor;
            pressedColor = PressedColor;
            focusedColor = FocusColor;
            disabledColor = NormalColor;

            Label.textColor = TextColor;
        }
        protected abstract void LabelSizeChanged();
    }
    public abstract class EditItem<ObjectType> : EditItemBase, IReusable
        where ObjectType : class, IDeletable
    {
        public event Action<EditItem<ObjectType>> OnDelete;

        bool IReusable.InCache { get; set; }

        protected Editor Editor { get; private set; }
        private ObjectType _object;
        public ObjectType Object
        {
            get => _object;
            private set
            {
                _object = value;
                if (_object != null)
                    Refresh();
            }
        }
        protected bool Inited => Object != null;
        protected CustomUIButton DeleteButton { get; set; }
        public virtual bool ShowDelete => true;

        public EditItem()
        {
            AddDeleteButton();
        }

        public virtual void Init(Editor editor, ObjectType editObject)
        {
            Editor = editor;
            Object = editObject;

            Refresh();
            OnSizeChanged();
        }
        public override void DeInit()
        {
            base.DeInit();

            Text = string.Empty;
            Editor = null;
            Object = null;
            OnDelete = null;
        }

        private void AddDeleteButton()
        {
            DeleteButton = AddUIComponent<CustomUIButton>();
            DeleteButton.atlas = CommonTextures.Atlas;
            DeleteButton.normalFgSprite = CommonTextures.CloseButtonNormal;
            DeleteButton.hoveredFgSprite = CommonTextures.CloseButtonHovered;
            DeleteButton.pressedFgSprite = CommonTextures.CloseButtonPressed;
            DeleteButton.foregroundSpriteMode = UIForegroundSpriteMode.Stretch;
            DeleteButton.size = new Vector2(20, 20);
            DeleteButton.eventClick += DeleteClick;
        }
        private void DeleteClick(UIComponent component, UIMouseEventParameter eventParam) => OnDelete?.Invoke(this);

        public virtual void Refresh()
        {
            DeleteButton.isVisible = ShowDelete;
            Text = Object.ToString();
            SetColors();
        }
    }
    public abstract class EditItem<ObjectType, IconType> : EditItem<ObjectType>
        where ObjectType : class, IDeletable
        where IconType : UIComponent
    {
        public virtual bool ShowIcon => true;
        protected IconType Icon { get; set; }

        public EditItem()
        {
            Icon = AddUIComponent<IconType>();
        }
        public override void Init(Editor editor, ObjectType editObject)
        {
            Icon.isVisible = ShowIcon;
            base.Init(editor, editObject);
        }
        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            if (!Inited)
                return;

            var labelWidth = size.x;
            if (ShowIcon)
            {
                Icon.size = new Vector2(size.y - 6, size.y - 6);
                Icon.relativePosition = new Vector2(3, 3);
                labelWidth -= 25;
            }

            if (ShowDelete)
            {
                DeleteButton.size = new Vector2(size.y - 10, size.y - 10);
                DeleteButton.relativePosition = new Vector2(size.x - (size.y - 5), 5);
                labelWidth -= 19;
            }

            Label.size = new Vector2(ShowIcon ? labelWidth : labelWidth - 3, size.y);
        }
        protected override void LabelSizeChanged() => Label.relativePosition = new Vector3(ShowIcon ? size.y : 3, (size.y - Label.height) / 2);
    }

    public class ColorIcon : CustomUIButton
    {
        private static float Border => 1f;
        protected CustomUIButton InnerCircule { get; set; }
        public Color32 InnerColor { set => InnerCircule.color = value; }
        public Color32 BorderColor { set => color = value; }
        public ColorIcon()
        {
            atlas = TextureHelper.InGameAtlas;
            normalBgSprite = disabledBgSprite = "PieChartWhiteBg";
            isInteractive = false;
            color = Color.white;

            InnerCircule = AddUIComponent<CustomUIButton>();
            InnerCircule.atlas = TextureHelper.InGameAtlas;
            InnerCircule.normalBgSprite = InnerCircule.normalFgSprite = "PieChartWhiteBg";
            InnerCircule.disabledBgSprite = InnerCircule.disabledFgSprite = "PieChartWhiteBg";
            InnerCircule.isInteractive = false;
            InnerCircule.relativePosition = new Vector3(Border, Border);
            InnerCircule.color = InnerCircule.disabledColor = Color.black;

        }
        protected override void OnSizeChanged()
        {
            if (InnerCircule != null)
            {
                InnerCircule.height = height - (Border * 2);
                InnerCircule.width = width - (Border * 2);
            }
        }
    }
    public class StyleIcon : ColorIcon
    {
        protected CustomUIButton Thumbnail { get; set; }

        public Color32 StyleColor { set => Thumbnail.color = Thumbnail.disabledColor = value.GetStyleIconColor(); }
        public Style.StyleType Type { set => Thumbnail.normalBgSprite = Thumbnail.normalFgSprite = value.ToString(); }

        public StyleIcon()
        {
            Thumbnail = AddUIComponent<CustomUIButton>();
            Thumbnail.atlas = IMTTextures.Atlas;
            Thumbnail.relativePosition = new Vector3(0, 0);
            Thumbnail.isInteractive = false;
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            if (Thumbnail != null)
                Thumbnail.size = size;
        }
    }
    public class LineIcon : StyleIcon
    {
        protected CustomUILabel CountLabel { get; }
        public int Count
        {
            set
            {
                CountLabel.isVisible = value > 1;
                Thumbnail.isVisible = value == 1;
                CountLabel.text = value.ToString();
            }
        }

        public LineIcon()
        {
            CountLabel = AddUIComponent<CustomUILabel>();
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
}
