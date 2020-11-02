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

            atlas = TextureUtil.Atlas;

            Deactivate();
            hoveredBgSprite = TextureUtil.ButtonHover;
            hoveredFgSprite = TextureUtil.IconHover;

            relativePosition = ButtonPosition;
            size = new Vector2(ButtonSize, ButtonSize);
            Show();
            Unfocus();
            Invalidate();
        }

        public void Activate()
        {
            Logger.LogDebug($"{nameof(NodeMarkupButton)}.{nameof(Activate)}");

            focusedBgSprite = TextureUtil.ButtonActive;
            normalBgSprite = TextureUtil.ButtonActive;
            pressedBgSprite = TextureUtil.ButtonActive;
            disabledBgSprite = TextureUtil.ButtonActive;
            normalFgSprite = TextureUtil.IconActive;
            focusedFgSprite = TextureUtil.IconActive;
            Invalidate();
        }
        public void Deactivate()
        {
            Logger.LogDebug($"{nameof(NodeMarkupButton)}.{nameof(Deactivate)}");

            focusedBgSprite = TextureUtil.ButtonNormal;
            normalBgSprite = TextureUtil.ButtonNormal;
            pressedBgSprite = TextureUtil.ButtonNormal;
            disabledBgSprite = TextureUtil.ButtonNormal;
            normalFgSprite = TextureUtil.Icon;
            focusedFgSprite = TextureUtil.Icon;
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
