using ColossalFramework.UI;
using IMT.Tools;
using IMT.Utilities;
using ModsCommon;
using ModsCommon.UI;
using UnityEngine;

namespace IMT.UI
{
    public class NodeMarkingButton : UUINetToolButton<Mod, IntersectionMarkingTool>
    {
        protected override Vector2 ButtonPosition => new Vector3(59, 38);

        protected override UITextureAtlas DefaultAtlas => IMTTextures.Atlas;
        protected override SpriteSet DefaultBgSprite => new SpriteSet(IMTTextures.ActivationButtonNormal, IMTTextures.ActivationButtonHover, IMTTextures.ActivationButtonHover, IMTTextures.ActivationButtonActive, string.Empty);
        protected override SpriteSet DefaultFgSprite => new SpriteSet(IMTTextures.ActivationButtonIconNormal, IMTTextures.ActivationButtonIconHover, IMTTextures.ActivationButtonIconNormal, IMTTextures.ActivationButtonIconNormal, string.Empty);
    }
}
