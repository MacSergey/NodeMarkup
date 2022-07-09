using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.UI;
using NodeMarkup.Tools;
using NodeMarkup.Utilities;
using UnityEngine;

namespace NodeMarkup.UI
{
    public class NodeMarkupButton : UUINetToolButton<Mod, NodeMarkupTool>
    {
        protected override Vector2 ButtonPosition => new Vector3(59, 38);
        protected override UITextureAtlas Atlas => NodeMarkupTextures.Atlas;

        protected override string NormalBgSprite => NodeMarkupTextures.ActivationButtonNormal;
        protected override string HoveredBgSprite => NodeMarkupTextures.ActivationButtonHover;
        protected override string PressedBgSprite => NodeMarkupTextures.ActivationButtonHover;
        protected override string FocusedBgSprite => NodeMarkupTextures.ActivationButtonActive;
        protected override string NormalFgSprite => NodeMarkupTextures.ActivationButtonIconNormal;
        protected override string HoveredFgSprite => NodeMarkupTextures.ActivationButtonIconHover;
        protected override string PressedFgSprite => NodeMarkupTextures.ActivationButtonIconNormal;
        protected override string FocusedFgSprite => NodeMarkupTextures.ActivationButtonIconNormal;
    }
}
