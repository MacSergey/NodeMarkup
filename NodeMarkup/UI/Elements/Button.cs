using ColossalFramework.UI;
using NodeMarkup.Tools;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI
{
    public class NodeMarkupButton : UIButton
    {
        const string CONTAINING_PANEL_NAME = "RoadsOptionPanel";
        private static string ButtonBg => nameof(ButtonBg);
        private static string ButtonBgActive => nameof(ButtonBgActive);
        private static string ButtonBgHovered => nameof(ButtonBgHovered);
        private static string Icon => nameof(Icon);
        private static string IconActive => nameof(IconActive);
        private static string IconHovered => nameof(IconHovered);

        private static string[] Sprites { get; } = new string[]
        {
                ButtonBg,
                ButtonBgActive,
                ButtonBgHovered,
                Icon,
                IconActive,
                IconHovered
        };
        private static UITextureAtlas Atlas { get; } = TextureUtil.CreateTextureAtlas("Button.png", nameof(NodeMarkupButton), ButtonSize, ButtonSize, Sprites);

        private static int ButtonSize => 31;
        private static Vector2 ButtonPosition => new Vector3(64, 38);
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

            atlas = Atlas;

            Deactivate();
            hoveredBgSprite = ButtonBgHovered;
            hoveredFgSprite = IconHovered;

            relativePosition = ButtonPosition;
            size = new Vector2(ButtonSize, ButtonSize);
            Show();
            Unfocus();
            Invalidate();
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
            Instance = GetContainingPanel().AddUIComponent<NodeMarkupButton>();
            Logger.LogDebug($"Button created");
            return Instance;
        }
        public static void RemoveButton()
        {
            Logger.LogDebug($"{nameof(NodeMarkupButton)}.{nameof(RemoveButton)}");

            if (Instance != null)
            {
                GetContainingPanel().RemoveUIComponent(Instance);
                Destroy(Instance);
                Instance = null;
                Logger.LogDebug($"Button removed");
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
