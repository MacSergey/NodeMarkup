using ColossalFramework.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public abstract class EditableItem : UIButton
    {
        protected UILabel Label { get; set; }

        public string Text
        {
            get => Label.text;
            set => Label.text = value;
        }

        public void Select() => normalBgSprite = "ButtonSmallPressed";
        public void Unselect() => normalBgSprite = "ButtonSmall";
    }
    public abstract class EditableItem<EditableObject, IconType> : EditableItem where IconType : UIComponent
    {
        public event Action<EditableItem<EditableObject, IconType>> OnDelete;

        EditableObject _object;
        public abstract string Description { get; }
        public EditableObject Object
        {
            get => _object;
            set
            {
                _object = value;
                Refresh();
                OnObjectSet();
            }
        }
        protected IconType Icon { get; set; }
        private UIButton DeleteButton { get; set; }

        public bool ShowIcon { get; set; }
        public bool ShowDelete { get; set; }

        public EditableItem(bool showIcon, bool showDelete)
        {
#if STOPWATCH
            var sw = Stopwatch.StartNew();
#endif
            ShowIcon = showIcon;
            ShowDelete = showDelete;

            if (ShowIcon)
                AddIcon();
            AddLable();
            if (ShowDelete)
                AddDeleteButton();

            atlas = NodeMarkupPanel.InGameAtlas;

            normalBgSprite = "ButtonSmall";
            disabledBgSprite = "ButtonSmallPressed";
            focusedBgSprite = "ButtonSmallPressed";
            hoveredBgSprite = "ButtonSmallHovered";
            pressedBgSprite = "ButtonSmallPressed";


            height = 25;
#if STOPWATCH
            Logger.LogDebug($"{nameof(EditableItem)}.constructor: {sw.ElapsedMilliseconds}ms");
#endif
        }

        private void AddIcon()
        {
            Icon = AddUIComponent<IconType>();
            Icon.isEnabled = ShowIcon;
        }
        private void AddLable()
        {
            Label = AddUIComponent<UILabel>();
            Label.textAlignment = UIHorizontalAlignment.Left;
            Label.verticalAlignment = UIVerticalAlignment.Middle;
            Label.autoSize = false;
            Label.autoHeight = false;
            Label.textScale = 0.6f;
            Label.padding = new RectOffset(0, 0, 2, 0);
        }
        private void AddDeleteButton()
        {
            DeleteButton = AddUIComponent<UIButton>();
            DeleteButton.atlas = NodeMarkupPanel.InGameAtlas;
            DeleteButton.normalBgSprite = "buttonclose";
            DeleteButton.hoveredBgSprite = "buttonclosehover";
            DeleteButton.pressedBgSprite = "buttonclosepressed";
            DeleteButton.size = new Vector2(20, 20);
            DeleteButton.isEnabled = ShowDelete;
            DeleteButton.eventClick += DeleteClick;
        }
        private void DeleteClick(UIComponent component, UIMouseEventParameter eventParam) => OnDelete?.Invoke(this);
        protected virtual void OnObjectSet()
        {

        }
        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            var labelWidth = size.x- 3;
            if (ShowIcon)
            {
                Icon.size = new Vector2(size.y - 6, size.y - 6);
                Icon.relativePosition = new Vector2(3, 3);
                labelWidth -= 25;
            }

            if (ShowDelete)
            {
                DeleteButton.size = new Vector2(size.y - 6, size.y - 6);
                DeleteButton.relativePosition = new Vector2(size.x - (size.y - 3), 3);
                labelWidth -= 25;
            }

            Label.size = new Vector2(labelWidth, size.y);
            Label.relativePosition = new Vector3(ShowIcon ? size.y : 3, 0);
        }

        public virtual void Refresh()
        {
            Text = Object.ToString();
        }
    }

    public class ColorIcon : UIButton
    {
        private UIButton ColorCircule { get; set; }
        public Color32 Color
        {
            get => ColorCircule.color;
            set
            {
                ColorCircule.color = value;
                ColorCircule.disabledColor = value;
            }
        }
        public ColorIcon()
        {
            atlas = NodeMarkupPanel.InGameAtlas;
            normalBgSprite = "PieChartWhiteBg";
            disabledBgSprite = "PieChartWhiteBg";
            isInteractive = false;
            color = UnityEngine.Color.white;

            ColorCircule = AddUIComponent<UIButton>();
            ColorCircule.atlas = NodeMarkupPanel.InGameAtlas;
            ColorCircule.normalBgSprite = "PieChartWhiteBg";
            ColorCircule.normalFgSprite = "PieChartWhiteFg";
            ColorCircule.disabledBgSprite = "PieChartWhiteBg";
            ColorCircule.disabledFgSprite = "PieChartWhiteFg";
            ColorCircule.isInteractive = false;
            ColorCircule.relativePosition = new Vector3(2, 2);
        }
        protected override void OnSizeChanged()
        {
            if (ColorCircule != null)
            {
                ColorCircule.height = height - 4;
                ColorCircule.width = width - 4;
            }
        }
    }
}
