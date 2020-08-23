using ColossalFramework.UI;
using NodeMarkup.Utils;
using System;
using UnityEngine;

namespace NodeMarkup.UI
{
    public class NodeMarkupButton : UIButton
    {
        const string CONTAINING_PANEL_NAME = "RoadsOptionPanel";
        const string ButtonBg = "NodeMarkupButtonBg";
        const string ButtonBgActive = "NodeMarkupButtonBgActive";
        const string ButtonBgHovered = "NodeMarkupButtonBgHovered";
        const string Icon = "NodeMarkupIcon";
        const string IconActive = "NodeMarkupIconActived";
        const string IconHovered = "NodeMarkupIconHovered";
        const int buttonSize = 31;
        readonly static Vector2 buttonPosition = new Vector3(64, 38);
        public static string AtlasName = nameof(NodeMarkupButton);
        public static NodeMarkupButton Instance { get; private set; }

        static UIComponent GetContainingPanel()
        {
            var ret = UIUtils.FindComponent<UIComponent>(CONTAINING_PANEL_NAME, null, UIUtils.FindOptions.NameContains);
            return ret ?? throw new Exception($"Could not find {CONTAINING_PANEL_NAME}");
        }

        public override void Start()
        {
            Logger.LogDebug($"{nameof(NodeMarkupButton)}.{nameof(Start)}");

            base.Start();
            name = nameof(NodeMarkupButton);
            playAudioEvents = true;

            if(!(UIUtils.FindComponent<UITabstrip>("ToolMode", GetContainingPanel(), UIUtils.FindOptions.None) is UITabstrip builtinTabstrip))
                return;

            string[] spriteNames = new string[]
            {
                ButtonBg,
                ButtonBgActive,
                ButtonBgHovered,
                Icon,
                IconActive,
                IconHovered
            };

            atlas = TextureUtil.GetAtlas(AtlasName);
            if (atlas == UIView.GetAView().defaultAtlas)
            {
                atlas = TextureUtil.CreateTextureAtlas("sprites.png", AtlasName, buttonSize, buttonSize, spriteNames);
            }

            Deactivate();
            hoveredBgSprite = ButtonBgHovered;
            hoveredFgSprite = IconHovered;

            relativePosition = buttonPosition;
            size = new Vector2(buttonSize, buttonSize);
            Show();
            Unfocus();
            Invalidate();

            Instance = this;
        }

        public void Activate()
        {
            Logger.LogDebug($"{nameof(NodeMarkupButton)}.{nameof(Activate)}");

            focusedBgSprite = ButtonBgActive;
            normalBgSprite = ButtonBgActive;
            pressedBgSprite = ButtonBgActive;
            disabledBgSprite = ButtonBgActive;
            normalFgSprite = IconActive;
            focusedFgSprite = IconActive;
            Invalidate();
        }
        public void Deactivate()
        {
            Logger.LogDebug($"{nameof(NodeMarkupButton)}.{nameof(Deactivate)}");

            focusedBgSprite = ButtonBg;
            normalBgSprite = ButtonBg;
            pressedBgSprite = ButtonBg;
            disabledBgSprite = ButtonBg;
            normalFgSprite = Icon;
            focusedFgSprite = Icon;
            Invalidate();
        }

        public static NodeMarkupButton CreateButton()
        {
            Logger.LogDebug($"{nameof(NodeMarkupButton)}.{nameof(CreateButton)}");
            return GetContainingPanel().AddUIComponent<NodeMarkupButton>();
        }
        public static void RemoveButton()
        {
            Logger.LogDebug($"{nameof(NodeMarkupButton)}.{nameof(RemoveButton)}");

            if (Instance != null)
            {
                GetContainingPanel().RemoveUIComponent(Instance);
                Destroy(Instance);
                Instance = null;
            }
        }

        protected override void OnClick(UIMouseEventParameter p)
        {
            Logger.LogDebug($"{nameof(NodeMarkupButton)}.{nameof(OnClick)}");

            base.OnClick(p);
            NodeMarkupTool.Instance.ToggleTool();
        }
        protected override void OnTooltipEnter(UIMouseEventParameter p)
        {
            tooltip = $"{Mod.StaticName} ({NodeMarkupTool.ActivationShortcut})";
            base.OnTooltipEnter(p);
        }
    }
}
