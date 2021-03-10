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
        private static string RoadsOptionPanel => nameof(RoadsOptionPanel);
        private static int ButtonSize => 31;
        private static Vector2 ButtonPosition => new Vector3(64, 38);

        public static void GeneratedScrollPanelCreateOptionPanelPostfix(string templateName, ref OptionPanelBase __result)
        {
            if (__result == null || templateName != RoadsOptionPanel)
                return;

            Mod.Logger.Debug($"Create buton");
            __result.component.AddUIComponent<NodeMarkupButton>();
            Mod.Logger.Debug($"Button created");
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

        protected override void OnClick(UIMouseEventParameter p)
        {
            Mod.Logger.Debug($"On button click");

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
