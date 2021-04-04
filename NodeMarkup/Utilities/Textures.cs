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

        private static string[] StyleNames { get; } = new string[]
            {
                nameof(Style.StyleType.LineSolid),
                nameof(Style.StyleType.LineDashed),
                nameof(Style.StyleType.LineDoubleSolid),
                nameof(Style.StyleType.LineDoubleDashed),
                nameof(Style.StyleType.LineSolidAndDashed),
                nameof(Style.StyleType.LineSharkTeeth),
                nameof(Style.StyleType.LinePavement),

                nameof(Style.StyleType.StopLineSolid),
                nameof(Style.StyleType.StopLineDashed),
                nameof(Style.StyleType.StopLineDoubleSolid),
                nameof(Style.StyleType.StopLineDoubleDashed),
                nameof(Style.StyleType.StopLineSolidAndDashed),
                nameof(Style.StyleType.StopLineSharkTeeth),

                nameof(Style.StyleType.FillerStripe),
                nameof(Style.StyleType.FillerGrid),
                nameof(Style.StyleType.FillerSolid),
                nameof(Style.StyleType.FillerChevron),
                nameof(Style.StyleType.FillerPavement),
                nameof(Style.StyleType.FillerGrass),

                nameof(Style.StyleType.CrosswalkExistent),
                nameof(Style.StyleType.CrosswalkZebra),
                nameof(Style.StyleType.CrosswalkDoubleZebra),
                nameof(Style.StyleType.CrosswalkParallelSolidLines),
                nameof(Style.StyleType.CrosswalkParallelDashedLines),
                nameof(Style.StyleType.CrosswalkLadder),
                nameof(Style.StyleType.CrosswalkSolid),
                nameof(Style.StyleType.CrosswalkChessBoard),
            };

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
        public static string Additionally => nameof(Additionally);
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

        public static string ArrowDown { get; } = nameof(ArrowDown);
        public static string ArrowRight { get; } = nameof(ArrowRight);

        private static Dictionary<string, TextureHelper.SpriteParamsGetter> Files { get; } = new Dictionary<string, TextureHelper.SpriteParamsGetter>
        {
            {nameof(OrderButtons), OrderButtons},
            {nameof(Styles), Styles},
            {nameof(HeaderButtons), HeaderButtons},
            {nameof(ListItem), ListItem},
            {nameof(Button), Button},
            {nameof(Arrows), Arrows},
        };

        static NodeMarkupTextures()
        {
            Atlas = TextureHelper.CreateAtlas(nameof(NodeMarkup), Files);
        }

        private static SpriteParams[] OrderButtons(int texWidth, int texHeight, Rect rect) => TextureHelper.GetSpritesParams(texWidth, texHeight, rect, 50, 50, TurnLeftButton, FlipButton, TurnRightButton, ApplyButton, NotApplyButton, ResetButton).ToArray();

        private static SpriteParams[] Styles(int texWidth, int texHeight, Rect rect) => TextureHelper.GetSpritesParams(texWidth, texHeight, rect, 19, 19, StyleNames).ToArray();

        private static SpriteParams[] HeaderButtons(int texWidth, int texHeight, Rect rect) => TextureHelper.GetSpritesParams(texWidth, texHeight, rect, 25, 25, new RectOffset(4, 4, 4, 4), 2, AddTemplate, ApplyTemplate, Copy, Paste, Duplicate, SetDefault, UnsetDefault, Apply, Package, Clear, Edit, Save, NotSave, Offset, EdgeLines, Additionally, Cut, BeetwenIntersections, WholeStreet).ToArray();

        private static SpriteParams[] ListItem(int texWidth, int texHeight, Rect rect) => TextureHelper.GetSpritesParams(texWidth, texHeight, rect, new RectOffset(2, 2, 2, 2), 1, ListItemSprite).ToArray();

        private static SpriteParams[] Button(int texWidth, int texHeight, Rect rect) => TextureHelper.GetSpritesParams(texWidth, texHeight, rect, 31, 31, ButtonNormal, ButtonActive, ButtonHover, Icon, IconActive, IconHover).ToArray();


        private static SpriteParams[] Arrows(int texWidth, int texHeight, Rect rect) => TextureHelper.GetSpritesParams(texWidth, texHeight, rect, 32, 32, ArrowDown, ArrowRight).ToArray();


    }
}
