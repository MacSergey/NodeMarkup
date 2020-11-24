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
            var ret = ModsCommon.UI.UIHelper.FindComponent<UIComponent>(CONTAINING_PANEL_NAME, null, ModsCommon.UI.UIHelper.FindOptions.NameContains);
            return ret ?? throw new Exception($"Could not find {CONTAINING_PANEL_NAME}");
        }
        public override void Start()
        {
            atlas = TextureUtil.Atlas;

            normalBgSprite = TextureUtil.ButtonNormal;
            hoveredBgSprite = TextureUtil.ButtonHover;
            pressedBgSprite = TextureUtil.ButtonHover;
            focusedBgSprite = TextureUtil.ButtonActive;

            normalFgSprite = TextureUtil.Icon;
            hoveredFgSprite = TextureUtil.IconHover;
            pressedFgSprite = TextureUtil.Icon;
            focusedFgSprite = TextureUtil.Icon;

            relativePosition = ButtonPosition;
            size = new Vector2(ButtonSize, ButtonSize);
        }
        public override void Update()
        {
            base.Update();

            var enable = NodeMarkupTool.Instance?.enabled == true;

            if (enable && state == (ButtonState.Normal | ButtonState.Hovered))
                state = ButtonState.Focused;
            else if (!enable && state == ButtonState.Focused)
                state = ButtonState.Normal;
        }
        public static void CreateButton()
        {
            Mod.Logger.Debug($"{nameof(NodeMarkupButton)}.{nameof(CreateButton)}");
            Instance = GetContainingPanel().AddUIComponent<NodeMarkupButton>();
            Mod.Logger.Debug($"Button created");
        }
        public static void RemoveButton()
        {
            Mod.Logger.Debug($"{nameof(NodeMarkupButton)}.{nameof(RemoveButton)}");

            if (Instance != null)
            {
                GetContainingPanel().RemoveUIComponent(Instance);
                Destroy(Instance);
                Instance = null;
                Mod.Logger.Debug($"Button removed");
            }
        }

        protected override void OnClick(UIMouseEventParameter p)
        {
            Mod.Logger.Debug($"{nameof(NodeMarkupButton)}.{nameof(OnClick)}");

            base.OnClick(p);
            NodeMarkupTool.Instance.ToggleTool();
        }
        protected override void OnTooltipEnter(UIMouseEventParameter p)
        {
            tooltip = $"{Mod.ShortName} ({NodeMarkupTool.ActivationShortcut})";
            base.OnTooltipEnter(p);
        }
    }
}
