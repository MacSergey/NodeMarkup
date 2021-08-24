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

        protected override string NormalBgSprite => NodeMarkupTextures.ButtonNormal;
        protected override string HoveredBgSprite => NodeMarkupTextures.ButtonHover;
        protected override string PressedBgSprite => NodeMarkupTextures.ButtonHover;
        protected override string FocusedBgSprite => NodeMarkupTextures.ButtonActive;
        protected override string NormalFgSprite => NodeMarkupTextures.Icon;
        protected override string HoveredFgSprite => NodeMarkupTextures.IconHover;
        protected override string PressedFgSprite => NodeMarkupTextures.Icon;
        protected override string FocusedFgSprite => NodeMarkupTextures.Icon;
    }
}
