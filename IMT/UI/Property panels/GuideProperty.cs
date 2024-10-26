using ColossalFramework.UI;
using IMT.Manager;
using IMT.Utilities;
using ModsCommon.UI;
using System;
using UnityEngine;

namespace IMT.UI
{
    public class FillerGuidePropertyPanel : EditorPropertyPanel, IReusable
    {
        public event Action<bool, FillerGuide, FillerGuide> OnValueChanged;
        public event Action<SelectGuideButton> OnSelect;
        public event Action<SelectGuideButton> OnEnter;
        public event Action<SelectGuideButton> OnLeave;

        private CustomUIToggle FollowGuide { get; set; }
        protected SelectGuideButton LeftGuideSelector { get; private set; }
        protected SelectGuideButton RightGuideSelector { get; private set; }
        protected CustomUIButton TurnButton { get; set; }

        public int VertexCount { get; private set; }
        public FillerGuide LeftGuide
        {
            get => LeftGuideSelector.Value;
            set
            {
                LeftGuideSelector.Value = value;
                RightGuideSelector.Other = value;
                OnValueChanged?.Invoke(FollowGuide.Value, LeftGuideSelector.Value, RightGuide);
            }
        }
        public FillerGuide RightGuide
        {
            get => RightGuideSelector.Value;
            set
            {
                RightGuideSelector.Value = value;
                LeftGuideSelector.Other = value;
                OnValueChanged?.Invoke(FollowGuide.Value, LeftGuide, RightGuideSelector.Value);
            }
        }
        public bool? Follow
        {
            get => FollowGuide.isVisible ? FollowGuide.Value : null;
            set
            {
                if (value != null)
                {
                    FollowGuide.isVisible = true;
                    FollowGuide.Value = value.Value;
                }
                else
                    FollowGuide.isVisible = false;

                Refresh();
            }
        }

        protected override void FillContent()
        {
            FollowGuide = Content.AddUIComponent<CustomUIToggle>();
            FollowGuide.DefaultStyle();
            FollowGuide.OnValueChanged += FollowChanged;

            LeftGuideSelector = Content.AddUIComponent<SelectGuideButton>();
            LeftGuideSelector.name = nameof(LeftGuideSelector);
            LeftGuideSelector.width = 80f;
            LeftGuideSelector.eventClick += ButtonClick;
            LeftGuideSelector.eventMouseEnter += ButtonMouseEnter;
            LeftGuideSelector.eventMouseLeave += ButtonMouseLeave;
            LeftGuideSelector.OnValueChanged += LeftGuideChanged;

            RightGuideSelector = Content.AddUIComponent<SelectGuideButton>();
            RightGuideSelector.name = nameof(RightGuideSelector);
            RightGuideSelector.width = 80f;
            RightGuideSelector.eventClick += ButtonClick;
            RightGuideSelector.eventMouseEnter += ButtonMouseEnter;
            RightGuideSelector.eventMouseLeave += ButtonMouseLeave;
            RightGuideSelector.OnValueChanged += RightGuideChanged;

            TurnButton = Content.AddUIComponent<CustomUIButton>();
            TurnButton.name = nameof(TurnButton);
            TurnButton.SetDefaultStyle();
            TurnButton.size = new Vector2(20f, 20f);
            TurnButton.IconAtlas = IMTTextures.Atlas;
            TurnButton.AllIconSprites = IMTTextures.RotateButtonIcon;
            TurnButton.tooltip = IMT.Localize.StyleOption_Turn;
            TurnButton.eventClick += TurnClick;
        }
        public void Init(int vertexCount)
        {
            base.Init();
            VertexCount = vertexCount;
        }
        public override void DeInit()
        {
            base.DeInit();

            VertexCount = 0;
            OnSelect = null;
            OnEnter = null;
            OnLeave = null;
            OnValueChanged = null;
            FollowGuide.isVisible = true;
            LeftGuideSelector.isEnabled = true;
            RightGuideSelector.isEnabled = true;
            TurnButton.isEnabled = true;
        }

        private void FollowChanged(bool value)
        {
            Refresh();
            OnValueChanged?.Invoke(value, LeftGuide, RightGuide);
        }
        private void LeftGuideChanged(FillerGuide leftGuide)
        {
            OnValueChanged?.Invoke(true, leftGuide, RightGuide);
        }
        private void RightGuideChanged(FillerGuide rightGuide)
        {
            OnValueChanged?.Invoke(true, LeftGuide, rightGuide);
        }

        private void Refresh()
        {
            var isEnabled = !FollowGuide.isVisible || FollowGuide.Value;
            LeftGuideSelector.isEnabled = isEnabled;
            RightGuideSelector.isEnabled = isEnabled;
            TurnButton.isEnabled = isEnabled;
        }

        protected virtual void ButtonClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (component.isEnabled)
                OnSelect?.Invoke(component as SelectGuideButton);
        }
        protected virtual void ButtonMouseEnter(UIComponent component, UIMouseEventParameter eventParam)
        {
            if(component.isEnabled)
                OnEnter?.Invoke(component as SelectGuideButton);
        }
        protected virtual void ButtonMouseLeave(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (component.isEnabled)
                OnLeave?.Invoke(component as SelectGuideButton);
        }

        protected virtual void TurnClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (VertexCount > 0)
            {
                LeftGuide = (LeftGuide + 1) % VertexCount;
                RightGuide = (RightGuide + 1) % VertexCount;
            }
        }

        public override void SetStyle(ControlStyle style)
        {
            FollowGuide.ToggleStyle = style.Toggle;
            LeftGuideSelector.SelectorStyle = style.DropDown;
            RightGuideSelector.SelectorStyle = style.DropDown;

            TurnButton.ButtonStyle = style.SmallButton;
            TurnButton.IconAtlas = IMTTextures.Atlas;
            TurnButton.AllIconSprites = IMTTextures.RotateButtonIcon;
        }

        public class SelectGuideButton : SelectItemPropertyButton<FillerGuide>
        {
            protected override string NotSet => string.Empty;
            public FillerGuide Other { get; set; }
        }
    }
}
