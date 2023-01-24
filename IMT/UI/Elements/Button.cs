using ColossalFramework.UI;
using IMT.Tools;
using IMT.Utilities;
using ModsCommon;
using UnityEngine;

namespace IMT.UI
{
    public class NodeMarkingButton : UUINetToolButton<Mod, IntersectionMarkingTool>
    {
        protected override Vector2 ButtonPosition => new Vector3(59, 38);
        protected override UITextureAtlas Atlas => IMTTextures.Atlas;

        protected override string NormalBgSprite => IMTTextures.ActivationButtonNormal;
        protected override string HoveredBgSprite => IMTTextures.ActivationButtonHover;
        protected override string PressedBgSprite => IMTTextures.ActivationButtonHover;
        protected override string FocusedBgSprite => IMTTextures.ActivationButtonActive;
        protected override string NormalFgSprite => IMTTextures.ActivationButtonIconNormal;
        protected override string HoveredFgSprite => IMTTextures.ActivationButtonIconHover;
        protected override string PressedFgSprite => IMTTextures.ActivationButtonIconNormal;
        protected override string FocusedFgSprite => IMTTextures.ActivationButtonIconNormal;
    }
}
