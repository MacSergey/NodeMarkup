using ColossalFramework.Importers;
using ColossalFramework.PlatformServices;
using ColossalFramework.UI;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace NodeMarkup.Utils
{
    public static class TextureUtil
    {
        public static UITextureAtlas InGameAtlas { get; } = GetAtlas("Ingame");

        public static UITextureAtlas Atlas;
        public static Texture2D Texture => Atlas.texture;

        static Dictionary<string, Action<int, int, Rect>> Files { get; } = new Dictionary<string, Action<int, int, Rect>>
        {
            {nameof(OrderButtons), OrderButtons},
            {nameof(Styles), Styles},
            {nameof(HeaderButtons), HeaderButtons},
            {nameof(ListItem), ListItem},
            {nameof(Button), Button},
            {nameof(TabButton), TabButton},
            {nameof(DefaultTabButtons), DefaultTabButtons},
            {nameof(Resize), Resize},
            {nameof(TextFieldPanel), TextFieldPanel},
            {nameof(OpacitySlider), OpacitySlider},
            {nameof(ColorPicker), ColorPicker},
            {nameof(CloseButton), CloseButton},
            {nameof(Arrows), Arrows},
            {nameof(Empty), Empty},
        };

        static TextureUtil()
        {
            var textures = Files.Select(f => LoadTextureFromAssembly(f.Key)).ToArray();
            var rects = CreateAtlas(textures);
            var actions = Files.Values.ToArray();

            for (var i = 0; i < actions.Length; i += 1)
                actions[i](textures[i].width, textures[i].height, rects[i]);
        }

        public static Texture2D LoadTextureFromAssembly(string textureFile)
        {
            var search = $".{textureFile}.";
            var executingAssembly = Assembly.GetExecutingAssembly();
            var path = executingAssembly.GetManifestResourceNames().FirstOrDefault(n => n.Contains(search));
            var manifestResourceStream = executingAssembly.GetManifestResourceStream(path);
            var data = new byte[manifestResourceStream.Length];
            manifestResourceStream.Read(data, 0, data.Length);

            var texture = new Image(data).CreateTexture();
            return texture;
        }

        static Rect[] CreateAtlas(Texture2D[] textures)
        {
            Atlas = ScriptableObject.CreateInstance<UITextureAtlas>();
            Atlas.material = UnityEngine.Object.Instantiate(UIView.GetAView().defaultAtlas.material);
            Atlas.material.mainTexture = RenderHelper.CreateTexture(1, 1, Color.white);
            Atlas.name = nameof(NodeMarkup);

            var rects = Atlas.texture.PackTextures(textures, Atlas.padding, 4096, false);
            return rects;
        }
        static UITextureAtlas GetAtlas(string name)
        {
            UITextureAtlas[] atlases = Resources.FindObjectsOfTypeAll(typeof(UITextureAtlas)) as UITextureAtlas[];
            for (int i = 0; i < atlases.Length; i++)
            {
                if (atlases[i].name == name)
                    return atlases[i];
            }
            return UIView.GetAView().defaultAtlas;
        }

        static void OrderButtons(int texWidth, int texHeight, Rect rect)
            => AddSprites(texWidth, texHeight, rect, 50, 50, TurnLeftButton, FlipButton, TurnRightButton, ApplyButton, NotApplyButton, ResetButton);

        static void Styles(int texWidth, int texHeight, Rect rect) => AddSprites(texWidth, texHeight, rect, 19, 19, StyleNames);

        static void HeaderButtons(int texWidth, int texHeight, Rect rect)
            => AddSprites(texWidth, texHeight, rect, 25, 25, new RectOffset(), 2, HeaderHovered, AddTemplate, ApplyTemplate, Copy, Paste, Duplicate, SetDefault, UnsetDefault, Apply, Package, Clear, Edit, Save, NotSave, Offset, EdgeLines, Additionally, Cut);

        static void ListItem(int texWidth, int texHeight, Rect rect) => AddSprites(texWidth, texHeight, rect, new RectOffset(2, 2, 2, 2), 1, ListItemSprite);

        static void Button(int texWidth, int texHeight, Rect rect)
            => AddSprites(texWidth, texHeight, rect, 31, 31, ButtonNormal, ButtonActive, ButtonHover, Icon, IconActive, IconHover);

        static void TabButton(int texWidth, int texHeight, Rect rect) => AddSprites(texWidth, texHeight, rect, new RectOffset(4, 4, 4, 0), 1, Tab);

        static void DefaultTabButtons(int texWidth, int texHeight, Rect rect)
            => AddSprites(texWidth, texHeight, rect, 58, 25, new RectOffset(4, 4, 4, 0), 2, TabNormal, TabHover, TabPressed, TabFocused, TabDisabled);

        static void Resize(int texWidth, int texHeight, Rect rect) => AddSprites(texWidth, texHeight, rect, ResizeSprite);

        static void TextFieldPanel(int texWidth, int texHeight, Rect rect)
            => AddSpritesRows(texWidth, texHeight, rect, 32, 32, new RectOffset(4, 4, 4, 4), 2, 4,
                FieldNormal, FieldHovered, FieldFocused, FieldDisabled,
                FieldNormalLeft, FieldHoveredLeft, FieldFocusedLeft, FieldDisabledLeft,
                FieldNormalRight, FieldHoveredRight, FieldFocusedRight, FieldDisabledRight,
                FieldNormalMiddle, FieldHoveredMiddle, FieldFocusedMiddle, FieldDisabledMiddle);

        static void OpacitySlider(int texWidth, int texHeight, Rect rect) => AddSprites(texWidth, texHeight, rect, 18, 200, new RectOffset(), 2, OpacitySliderBoard, OpacitySliderColor);

        static void ColorPicker(int texWidth, int texHeight, Rect rect)
            => AddSprites(texWidth, texHeight, rect, 43, 49, ColorPickerNormal, ColorPickerHover, ColorPickerDisable, ColorPickerColor, ColorPickerBoard);

        static void CloseButton(int texWidth, int texHeight, Rect rect)
            => AddSprites(texWidth, texHeight, rect, 32, 32, DeleteNormal, DeleteHover, DeletePressed);

        static void Arrows(int texWidth, int texHeight, Rect rect)
            => AddSprites(texWidth, texHeight, rect, 32, 32, ArrowDown, ArrowRight);

        static void Empty(int texWidth, int texHeight, Rect rect)
            => AddSprites(texWidth, texHeight, rect, 32, 32, EmptySprite);


        static void AddSprites(int texWidth, int texHeight, Rect rect, string sprite)
            => AddSprites(texWidth, texHeight, rect, new RectOffset(), 0, sprite);

        static void AddSprites(int texWidth, int texHeight, Rect rect, RectOffset border, int space, string sprite)
            => AddSprites(texWidth, texHeight, rect, texWidth - 2 * space, texHeight - 2 * space, border, space, sprite);

        static void AddSprites(int texWidth, int texHeight, Rect rect, int spriteWidth, int spriteHeight, params string[] sprites)
            => AddSprites(texWidth, texHeight, rect, spriteWidth, spriteHeight, new RectOffset(), 0, sprites);

        static void AddSpritesRows(int texWidth, int texHeight, Rect rect, int spriteWidth, int spriteHeight, RectOffset border, int space, int inRow, params string[] sprites)
        {
            var rows = sprites.Length / inRow + (sprites.Length % inRow == 0 ? 0 : 1);

            var rowHeight = rect.height / texHeight * spriteHeight;
            var spaceHeight = rect.height / texHeight * space;

            for (var i = 0; i < rows; i += 1)
            {
                var rowRect = new Rect(rect.x, rect.y + (spaceHeight + rowHeight) * (rows - i - 1), rect.width, rowHeight + 2 * spaceHeight);
                var rowSprites = sprites.Skip(inRow * i).Take(inRow).ToArray();
                AddSprites(texWidth, spriteHeight + 2 * space, rowRect, spriteWidth, spriteHeight, border, space, rowSprites);
            }
        }
        static void AddSprites(int texWidth, int texHeight, Rect rect, int spriteWidth, int spriteHeight, RectOffset border, int space, params string[] sprites)
        {
            var width = spriteWidth / (float)texWidth * rect.width;
            var height = spriteHeight / (float)texHeight * rect.height;
            var spaceWidth = space / (float)texWidth * rect.width;
            var spaceHeight = space / (float)texHeight * rect.height;

            for (int i = 0; i < sprites.Length; i += 1)
            {
                var x = rect.x + i * width + (i + 1) * spaceWidth;
                var y = rect.y + spaceHeight;
                AddSprite(sprites[i], new Rect(x, y, width, height), border);

            }
        }
        static void AddSprite(string name, Rect region, RectOffset border = null)
        {
            UITextureAtlas.SpriteInfo spriteInfo = new UITextureAtlas.SpriteInfo
            {
                name = name,
                texture = Atlas.material.mainTexture as Texture2D,
                region = region,
                border = border ?? new RectOffset()
            };
            Atlas.AddSprite(spriteInfo);
        }

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

                nameof(Style.StyleType.CrosswalkExistent),
                nameof(Style.StyleType.CrosswalkZebra),
                nameof(Style.StyleType.CrosswalkDoubleZebra),
                nameof(Style.StyleType.CrosswalkParallelSolidLines),
                nameof(Style.StyleType.CrosswalkParallelDashedLines),
                nameof(Style.StyleType.CrosswalkLadder),
                nameof(Style.StyleType.CrosswalkSolid),
                nameof(Style.StyleType.CrosswalkChessBoard),
            };

        public static string HeaderHovered => nameof(HeaderHovered);
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

        public static string ListItemSprite { get; } = nameof(ListItemSprite);

        public static string ButtonNormal => nameof(ButtonNormal);
        public static string ButtonActive => nameof(ButtonActive);
        public static string ButtonHover => nameof(ButtonHover);
        public static string Icon => nameof(Icon);
        public static string IconActive => nameof(IconActive);
        public static string IconHover => nameof(IconHover);

        public static string Tab { get; } = nameof(Tab);

        public static string TabNormal { get; } = nameof(TabNormal);
        public static string TabHover { get; } = nameof(TabHover);
        public static string TabPressed { get; } = nameof(TabPressed);
        public static string TabFocused { get; } = nameof(TabFocused);
        public static string TabDisabled { get; } = nameof(TabDisabled);

        public static string ResizeSprite { get; } = nameof(ResizeSprite);

        public static string FieldNormal => nameof(FieldNormal);
        public static string FieldHovered => nameof(FieldHovered);
        public static string FieldFocused => nameof(FieldFocused);
        public static string FieldDisabled => nameof(FieldDisabled);
        public static string FieldNormalLeft => nameof(FieldNormalLeft);
        public static string FieldHoveredLeft => nameof(FieldHoveredLeft);
        public static string FieldFocusedLeft => nameof(FieldFocusedLeft);
        public static string FieldDisabledLeft => nameof(FieldDisabledLeft);
        public static string FieldNormalRight => nameof(FieldNormalRight);
        public static string FieldHoveredRight => nameof(FieldHoveredRight);
        public static string FieldFocusedRight => nameof(FieldFocusedRight);
        public static string FieldDisabledRight => nameof(FieldDisabledRight);
        public static string FieldNormalMiddle => nameof(FieldNormalMiddle);
        public static string FieldHoveredMiddle => nameof(FieldHoveredMiddle);
        public static string FieldFocusedMiddle => nameof(FieldFocusedMiddle);
        public static string FieldDisabledMiddle => nameof(FieldDisabledMiddle);

        public static string OpacitySliderBoard { get; } = nameof(OpacitySliderBoard);
        public static string OpacitySliderColor { get; } = nameof(OpacitySliderColor);

        public static string ColorPickerNormal { get; } = nameof(ColorPickerNormal);
        public static string ColorPickerHover { get; } = nameof(ColorPickerHover);
        public static string ColorPickerDisable { get; } = nameof(ColorPickerDisable);
        public static string ColorPickerColor { get; } = nameof(ColorPickerColor);
        public static string ColorPickerBoard { get; } = nameof(ColorPickerBoard);

        public static string DeleteNormal { get; } = nameof(DeleteNormal);
        public static string DeleteHover { get; } = nameof(DeleteHover);
        public static string DeletePressed { get; } = nameof(DeletePressed);

        public static string ArrowDown { get; } = nameof(ArrowDown);
        public static string ArrowRight { get; } = nameof(ArrowRight);

        public static string EmptySprite => nameof(EmptySprite);
    }
}
