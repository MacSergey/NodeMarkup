using ColossalFramework.UI;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace NodeMarkup.Utilities
{
    public static class NodeMarkupTextures
    {
        public static UITextureAtlas Atlas;
        public static Texture2D Texture => Atlas.texture;

        public static string TurnLeftButton => nameof(TurnLeftButton);
        public static string FlipButton => nameof(FlipButton);
        public static string TurnRightButton => nameof(TurnRightButton);
        public static string ApplyButton => nameof(ApplyButton);
        public static string NotApplyButton => nameof(NotApplyButton);
        public static string ResetButton => nameof(ResetButton);

        public static string AddTemplate => nameof(AddTemplate);
        public static string ApplyTemplate => nameof(ApplyTemplate);
        public static string Copy => nameof(Copy);
        public static string Paste => nameof(Paste);
        public static string Duplicate => nameof(Duplicate);
        public static string SetDefault => nameof(SetDefault);
        public static string UnsetDefault => nameof(UnsetDefault);
        public static string Apply => nameof(Apply);
        public static string Package => nameof(Package);
        public static string Clear => nameof(Clear);
        public static string Edit => nameof(Edit);
        public static string Save => nameof(Save);
        public static string NotSave => nameof(NotSave);
        public static string Offset => nameof(Offset);
        public static string EdgeLines => nameof(EdgeLines);
        public static string Cut => nameof(Cut);
        public static string BeetwenIntersections => nameof(BeetwenIntersections);
        public static string WholeStreet => nameof(WholeStreet);

        public static string ListItemSprite { get; } = nameof(ListItemSprite);

        public static string ButtonNormal => nameof(ButtonNormal);
        public static string ButtonActive => nameof(ButtonActive);
        public static string ButtonHover => nameof(ButtonHover);
        public static string Icon => nameof(Icon);
        public static string IconActive => nameof(IconActive);
        public static string IconHover => nameof(IconHover);

        public static string UUINormal => nameof(UUINormal);
        public static string UUIHovered => nameof(UUIHovered);
        public static string UUIPressed => nameof(UUIPressed);
        //public static string UUIDisabled => nameof(UUIDisabled);

        public static string ArrowDown { get; } = nameof(ArrowDown);
        public static string ArrowRight { get; } = nameof(ArrowRight);

        private static Dictionary<string, TextureHelper.SpriteParamsGetter> Files { get; } = new Dictionary<string, TextureHelper.SpriteParamsGetter>
        {
            {nameof(OrderButtons), OrderButtons},
            {nameof(StylesLines), StylesLines},
            {nameof(StylesStopLines), StylesStopLines},
            {nameof(StylesCrosswalks), StylesCrosswalks},
            {nameof(StylesFillers), StylesFillers},
            {nameof(HeaderButtons), HeaderButtons},
            {nameof(ListItem), ListItem},
            {nameof(Button), Button},
            {nameof(UUIButton), UUIButton},
            {nameof(Arrows), Arrows},
        };

        static NodeMarkupTextures()
        {
            Atlas = TextureHelper.CreateAtlas(nameof(NodeMarkup), Files);
        }

        private static UITextureAtlas.SpriteInfo[] OrderButtons(int texWidth, int texHeight, Rect rect) => TextureHelper.GetSpritesInfo(texWidth, texHeight, rect, 50, 50, TurnLeftButton, FlipButton, TurnRightButton, ApplyButton, NotApplyButton, ResetButton).ToArray();

        private static UITextureAtlas.SpriteInfo[] Styles<TypeStyle>(int texWidth, int texHeight, Rect rect)
            where TypeStyle : Enum
        {
            var sprites = EnumExtension.GetEnumValues<TypeStyle>().Select(v => ((Style.StyleType)(object)v).ToString()).ToArray();
            return TextureHelper.GetSpritesInfo(texWidth, texHeight, rect, 19, 19, sprites).ToArray();
        }

        private static UITextureAtlas.SpriteInfo[] StylesLines(int texWidth, int texHeight, Rect rect) => Styles<RegularLineStyle.RegularLineType>(texWidth, texHeight, rect);

        private static UITextureAtlas.SpriteInfo[] StylesStopLines(int texWidth, int texHeight, Rect rect) => Styles<StopLineStyle.StopLineType>(texWidth, texHeight, rect);

        private static UITextureAtlas.SpriteInfo[] StylesCrosswalks(int texWidth, int texHeight, Rect rect) => Styles<CrosswalkStyle.CrosswalkType>(texWidth, texHeight, rect);

        private static UITextureAtlas.SpriteInfo[] StylesFillers(int texWidth, int texHeight, Rect rect) => Styles<FillerStyle.FillerType>(texWidth, texHeight, rect);

        private static UITextureAtlas.SpriteInfo[] HeaderButtons(int texWidth, int texHeight, Rect rect) => TextureHelper.GetSpritesInfo(texWidth, texHeight, rect, 25, 25, new RectOffset(4, 4, 4, 4), 2, AddTemplate, ApplyTemplate, Copy, Paste, Duplicate, SetDefault, UnsetDefault, Apply, Package, Clear, Edit, Save, NotSave, Offset, EdgeLines, Cut, BeetwenIntersections, WholeStreet).ToArray();

        private static UITextureAtlas.SpriteInfo[] ListItem(int texWidth, int texHeight, Rect rect) => TextureHelper.GetSpritesInfo(texWidth, texHeight, rect, new RectOffset(2, 2, 2, 2), 1, ListItemSprite).ToArray();

        private static UITextureAtlas.SpriteInfo[] Button(int texWidth, int texHeight, Rect rect) => TextureHelper.GetSpritesInfo(texWidth, texHeight, rect, 31, 31, ButtonNormal, ButtonActive, ButtonHover, Icon, IconActive, IconHover).ToArray();

        private static UITextureAtlas.SpriteInfo[] UUIButton(int texWidth, int texHeight, Rect rect) => TextureHelper.GetSpritesInfo(texWidth, texHeight, rect, 40, 40, UUINormal, UUIHovered, UUIPressed/*, UUIDisabled*/).ToArray();

        private static UITextureAtlas.SpriteInfo[] Arrows(int texWidth, int texHeight, Rect rect) => TextureHelper.GetSpritesInfo(texWidth, texHeight, rect, 32, 32, ArrowDown, ArrowRight).ToArray();


    }
}
