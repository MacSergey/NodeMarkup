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

        private BoolSegmented FollowGuide { get; }
        protected SelectGuideButton LeftGuideSelector { get; set; }
        protected SelectGuideButton RightGuideSelector { get; set; }
        protected MultyAtlasUIButton TurnButton { get; set; }

        public int VertexCount { get; private set; }
        public FillerGuide LeftGuide
        {
            get => LeftGuideSelector.Value;
            set
            {
                LeftGuideSelector.Value = value;
                RightGuideSelector.Other = value;
                OnValueChanged?.Invoke(FollowGuide.SelectedObject, LeftGuideSelector.Value, RightGuide);
            }
        }
        public FillerGuide RightGuide
        {
            get => RightGuideSelector.Value;
            set
            {
                RightGuideSelector.Value = value;
                LeftGuideSelector.Other = value;
                OnValueChanged?.Invoke(FollowGuide.SelectedObject, LeftGuide, RightGuideSelector.Value);
            }
        }
        public bool? Follow
        {
            get => FollowGuide.isVisible ? FollowGuide.SelectedObject : null;
            set
            {
                if (value != null)
                {
                    FollowGuide.isVisible = true;
                    FollowGuide.SelectedObject = value.Value;
                }
                else
                    FollowGuide.isVisible = false;

                Refresh();
            }
        }

        public FillerGuidePropertyPanel()
        {
            FollowGuide = Content.AddUIComponent<BoolSegmented>();
            FollowGuide.StopLayout();
            FollowGuide.AutoButtonSize = false;
            FollowGuide.ButtonWidth = 25f;
            FollowGuide.AddItem(true, "I");
            FollowGuide.AddItem(false, "O");
            FollowGuide.StartLayout();
            FollowGuide.OnSelectObjectChanged += FollowChanged;

            LeftGuideSelector = Content.AddUIComponent<SelectGuideButton>();
            LeftGuideSelector.width = 80f;
            LeftGuideSelector.Button.eventClick += ButtonClick;
            LeftGuideSelector.Button.eventMouseEnter += ButtonMouseEnter;
            LeftGuideSelector.Button.eventMouseLeave += ButtonMouseLeave;
            LeftGuideSelector.OnValueChanged += LeftGuideChanged;

            RightGuideSelector = Content.AddUIComponent<SelectGuideButton>();
            RightGuideSelector.width = 80f;
            RightGuideSelector.Button.eventClick += ButtonClick;
            RightGuideSelector.Button.eventMouseEnter += ButtonMouseEnter;
            RightGuideSelector.Button.eventMouseLeave += ButtonMouseLeave;
            RightGuideSelector.OnValueChanged += RightGuideChanged;

            TurnButton = Content.AddUIComponent<MultyAtlasUIButton>();
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
            var isEnabled = !FollowGuide.isVisible || FollowGuide.SelectedObject;
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
