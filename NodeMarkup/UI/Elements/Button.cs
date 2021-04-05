using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.UI;
using NodeMarkup.Tools;
using NodeMarkup.Utilities;
using UnityEngine;

namespace NodeMarkup.UI
{
    public class NodeMarkupButton : CustomUIButton
    {
        private static string RoadsOptionPanel => nameof(RoadsOptionPanel);
        private static int ButtonSize => 31;
        private static Vector2 ButtonPosition => new Vector3(59, 38);

        public static void GeneratedScrollPanelCreateOptionPanelPostfix(string templateName, ref OptionPanelBase __result)
        {
            if (__result == null || templateName != RoadsOptionPanel || __result.component.Find<NodeMarkupButton>(nameof(NodeMarkupButton)) != null)
                return;

            SingletonMod<Mod>.Logger.Debug($"Create button");
            __result.component.AddUIComponent<NodeMarkupButton>();
            SingletonMod<Mod>.Logger.Debug($"Button created");
        }

        public override void Start()
        {
            atlas = NodeMarkupTextures.Atlas;

            normalBgSprite = NodeMarkupTextures.ButtonNormal;
            hoveredBgSprite = NodeMarkupTextures.ButtonHover;
            pressedBgSprite = NodeMarkupTextures.ButtonHover;
            focusedBgSprite = NodeMarkupTextures.ButtonActive;

            normalFgSprite = NodeMarkupTextures.Icon;
            hoveredFgSprite = NodeMarkupTextures.IconHover;
            pressedFgSprite = NodeMarkupTextures.Icon;
            focusedFgSprite = NodeMarkupTextures.Icon;

            relativePosition = ButtonPosition;
            size = new Vector2(ButtonSize, ButtonSize);
        }
        public override void Update()
        {
            base.Update();

            var enable = SingletonTool<NodeMarkupTool>.Instance?.enabled == true;

            if (enable && state == (ButtonState.Normal | ButtonState.Hovered))
                state = ButtonState.Focused;
            else if (!enable && state == ButtonState.Focused)
                state = ButtonState.Normal;
        }

        protected override void OnClick(UIMouseEventParameter p)
        {
            SingletonMod<Mod>.Logger.Debug($"On button click");

            base.OnClick(p);
            SingletonTool<NodeMarkupTool>.Instance.Toggle();
        }
        protected override void OnTooltipEnter(UIMouseEventParameter p)
        {
            tooltip = $"{SingletonMod<Mod>.Instance.Name} ({NodeMarkupTool.ActivationShortcut})";
            base.OnTooltipEnter(p);
        }
    }
}
