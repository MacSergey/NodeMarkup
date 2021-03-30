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
    public static class TextureUtil
    {
        public static UITextureAtlas Atlas;
        public static Texture2D Texture => Atlas.texture;

        private static Dictionary<string, Action<int, int, Rect>> Files { get; } = new Dictionary<string, Action<int, int, Rect>>
        {
            {nameof(OrderButtons), OrderButtons},
            {nameof(Styles), Styles},
            {nameof(HeaderButtons), HeaderButtons},
            {nameof(ListItem), ListItem},
            {nameof(Button), Button},
            {nameof(Arrows), Arrows},
        };

        static TextureUtil()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var textures = Files.Select(f => assembly.LoadTextureFromAssembly(f.Key)).ToArray();
            Atlas = TextureHelper.CreateAtlas(textures, nameof(NodeMarkup), out Rect[] rects);
            var actions = Files.Values.ToArray();

            for (var i = 0; i < actions.Length; i += 1)
                actions[i](textures[i].width, textures[i].height, rects[i]);
        }

        private static void OrderButtons(int texWidth, int texHeight, Rect rect)
            => Atlas.AddSprites(texWidth, texHeight, rect, 50, 50, TurnLeftButton, FlipButton, TurnRightButton, ApplyButton, NotApplyButton, ResetButton);

        private static void Styles(int texWidth, int texHeight, Rect rect) => Atlas.AddSprites(texWidth, texHeight, rect, 19, 19, StyleNames);

        private static void HeaderButtons(int texWidth, int texHeight, Rect rect)
            => Atlas.AddSprites(texWidth, texHeight, rect, 25, 25, new RectOffset(4, 4, 4, 4), 2, AddTemplate, ApplyTemplate, Copy, Paste, Duplicate, SetDefault, UnsetDefault, Apply, Package, Clear, Edit, Save, NotSave, Offset, EdgeLines, Additionally, Cut, BeetwenIntersections, WholeStreet);

        private static void ListItem(int texWidth, int texHeight, Rect rect) => Atlas.AddSprites(texWidth, texHeight, rect, new RectOffset(2, 2, 2, 2), 1, ListItemSprite);

        private static void Button(int texWidth, int texHeight, Rect rect)
            => Atlas.AddSprites(texWidth, texHeight, rect, 31, 31, ButtonNormal, ButtonActive, ButtonHover, Icon, IconActive, IconHover);


        private static void Arrows(int texWidth, int texHeight, Rect rect)
            => Atlas.AddSprites(texWidth, texHeight, rect, 32, 32, ArrowDown, ArrowRight);

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
    }
}
