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
        bool IReusable.InCache { get; set; }

        public event Action<bool, FillerGuide, FillerGuide> OnValueChanged;
        public event Action<SelectGuideButton> OnSelect;
        public event Action<SelectGuideButton> OnEnter;
        public event Action<SelectGuideButton> OnLeave;

        private CustomUIToggle FollowGuide { get; }
        protected SelectGuideButton LeftGuideSelector { get; set; }
        protected SelectGuideButton RightGuideSelector { get; set; }
        protected CustomUIButton TurnButton { get; set; }

        public int VertexCount { get; private set; }
        public FillerGuide LeftGuide
        {
            get => LeftGuideSelector.Value;
            set
            {
                LeftGuideSelector.Value = value;
                RightGuideSelector.Other = value;
                OnValueChanged?.Invoke(FollowGuide.State, LeftGuideSelector.Value, RightGuide);
            }
        }
        public FillerGuide RightGuide
        {
            get => RightGuideSelector.Value;
            set
            {
                RightGuideSelector.Value = value;
                LeftGuideSelector.Other = value;
                OnValueChanged?.Invoke(FollowGuide.State, LeftGuide, RightGuideSelector.Value);
            }
        }
        public bool? Follow
        {
            get => FollowGuide.isVisible ? FollowGuide.State : null;
            set
            {
                if (value != null)
                {
                    FollowGuide.isVisible = true;
                    FollowGuide.State = value.Value;
                }
                else
                    FollowGuide.isVisible = false;

                Refresh();
            }
        }

        public FillerGuidePropertyPanel()
        {
            FollowGuide = Content.AddUIComponent<CustomUIToggle>();
            FollowGuide.CustomStyle();
            FollowGuide.OnStateChanged += FollowChanged;

            LeftGuideSelector = Content.AddUIComponent<SelectGuideButton>();
            LeftGuideSelector.width = 80f;
            LeftGuideSelector.eventClick += ButtonClick;
            LeftGuideSelector.eventMouseEnter += ButtonMouseEnter;
            LeftGuideSelector.eventMouseLeave += ButtonMouseLeave;
            LeftGuideSelector.OnValueChanged += LeftGuideChanged;

            RightGuideSelector = Content.AddUIComponent<SelectGuideButton>();
            RightGuideSelector.width = 80f;
            RightGuideSelector.eventClick += ButtonClick;
            RightGuideSelector.eventMouseEnter += ButtonMouseEnter;
            RightGuideSelector.eventMouseLeave += ButtonMouseLeave;
            RightGuideSelector.OnValueChanged += RightGuideChanged;

            TurnButton = Content.AddUIComponent<CustomUIButton>();
            TurnButton.SetDefaultStyle();
            TurnButton.size = new Vector2(20f, 20f);
            TurnButton.atlasForeground = IMTTextures.Atlas;
            TurnButton.normalFgSprite = IMTTextures.RotateButtonIcon;
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
            var isEnabled = !FollowGuide.isVisible || FollowGuide.State;
            LeftGuideSelector.isEnabled = isEnabled;
            RightGuideSelector.isEnabled = isEnabled;
            TurnButton.isEnabled = isEnabled;
        }

        protected virtual void ButtonClick(UIComponent component, UIMouseEventParameter eventParam) => OnSelect?.Invoke(component.parent as SelectGuideButton);
        protected virtual void ButtonMouseEnter(UIComponent component, UIMouseEventParameter eventParam) => OnEnter?.Invoke(component.parent as SelectGuideButton);
        protected virtual void ButtonMouseLeave(UIComponent component, UIMouseEventParameter eventParam) => OnLeave?.Invoke(component.parent as SelectGuideButton);

        protected virtual void TurnClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (VertexCount > 0)
            {
                LeftGuide = (LeftGuide + 1) % VertexCount;
                RightGuide = (RightGuide + 1) % VertexCount;
            }
        }

        public class SelectGuideButton : SelectItemPropertyButton<FillerGuide>
        {
            protected override string NotSet => string.Empty;
            public FillerGuide Other { get; set; }
        }
    }
}
