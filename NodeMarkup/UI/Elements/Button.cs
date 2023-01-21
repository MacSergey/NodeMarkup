using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.UI;
using NodeMarkup.Tools;
using NodeMarkup.Utilities;
using UnityEngine;

namespace NodeMarkup.UI
{
    public class NodeMarkingButton : UUINetToolButton<Mod, IntersectionMarkingTool>
    {
        protected override Vector2 ButtonPosition => new Vector3(59, 38);
        protected override UITextureAtlas Atlas => IntersectionMarkingToolTextures.Atlas;

        protected override string NormalBgSprite => IntersectionMarkingToolTextures.ActivationButtonNormal;
        protected override string HoveredBgSprite => IntersectionMarkingToolTextures.ActivationButtonHover;
        protected override string PressedBgSprite => IntersectionMarkingToolTextures.ActivationButtonHover;
        protected override string FocusedBgSprite => IntersectionMarkingToolTextures.ActivationButtonActive;
        protected override string NormalFgSprite => IntersectionMarkingToolTextures.ActivationButtonIconNormal;
        protected override string HoveredFgSprite => IntersectionMarkingToolTextures.ActivationButtonIconHover;
        protected override string PressedFgSprite => IntersectionMarkingToolTextures.ActivationButtonIconNormal;
        protected override string FocusedFgSprite => IntersectionMarkingToolTextures.ActivationButtonIconNormal;
    }
}
