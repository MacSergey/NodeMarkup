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

        static Dictionary<string, Action<Texture2D, Rect>> Files { get; } = new Dictionary<string, Action<Texture2D, Rect>>
        {
            {nameof(OrderButtons), OrderButtons},
            {nameof(Styles), Styles},
            {nameof(HeaderButtons), HeaderButtons},
            {nameof(ListItem), ListItem},
            {nameof(Button), Button},
            {nameof(Resize), Resize},
            {nameof(TextFieldPanel), TextFieldPanel},
            {nameof(OpacitySlider), OpacitySlider},
            {nameof(ColorPicker), ColorPicker},
            {nameof(CloseButton), CloseButton},
            {nameof(Arrows), Arrows},
        };

        static TextureUtil()
        {
            var textures = Files.Select(f => LoadTextureFromAssembly(f.Key)).ToArray();
            var rects = CreateAtlas(textures);
            var actions = Files.Values.ToArray();

            for (var i = 0; i < actions.Length; i += 1)
                actions[i](textures[i], rects[i]);
        }

        public static Texture2D LoadTextureFromAssembly(string textureFile)
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            //var path = $"{nameof(NodeMarkup)}.Resources.{textureFile}";
            var path = executingAssembly.GetManifestResourceNames().FirstOrDefault(n => n.Contains(textureFile));
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

        static void OrderButtons(Texture2D texture, Rect rect)
            => AddSprites(texture, rect, 50, 50, TurnLeftButton, FlipButton, TurnRightButton, ApplyButton, NotApplyButton, ResetButton);

        static void Styles(Texture2D texture, Rect rect) => AddSprites(texture, rect, 19, 19, StyleNames);

        static void HeaderButtons(Texture2D texture, Rect rect)
            => AddSprites(texture, rect, 25, 25, new RectOffset(), 2, HeaderHovered, AddTemplate, ApplyTemplate, Copy, Paste, Duplicate, SetDefault, UnsetDefault, Package, Clear, Edit, Offset, EdgeLines, Additionally, Cut);

        static void ListItem(Texture2D texture, Rect rect) => AddSprites(texture, rect, new RectOffset(1, 1, 1, 1), 0, ListItemSprite);

        static void Button(Texture2D texture, Rect rect)
            => AddSprites(texture, rect, 31, 31, ButtonNormal, ButtonActive, ButtonHover, Icon, IconActive, IconHover);

        static void Resize(Texture2D texture, Rect rect) => AddSprites(texture, rect, ResizeSprite);

        static void TextFieldPanel(Texture2D texture, Rect rect)
            => AddSprites(texture, rect, 32, 32, new RectOffset(4, 4, 4, 4), 2, FieldNormal, FieldHovered, FieldFocused, FieldDisabled, FieldEmpty);

        static void OpacitySlider(Texture2D texture, Rect rect) => AddSprites(texture, rect, new RectOffset(), 0, OpacitySliderSprite);

        static void ColorPicker(Texture2D texture, Rect rect)
            => AddSprites(texture, rect, 43, 49, ColorPickerNormal, ColorPickerHover, ColorPickerColor);

        static void CloseButton(Texture2D texture, Rect rect)
            => AddSprites(texture, rect, 32, 32, DeleteNormal, DeleteHover, DeletePressed);

        static void Arrows(Texture2D texture, Rect rect)
            => AddSprites(texture, rect, 32, 32, ArrowDown, ArrowRight);


        static void AddSprites(Texture2D texture, Rect rect, string sprite)
            => AddSprites(texture, rect, new RectOffset(), 0, sprite);

        static void AddSprites(Texture2D texture, Rect rect, RectOffset border, int space, string sprite)
            => AddSprites(texture, rect, texture.width, texture.height, border, space, sprite);

        static void AddSprites(Texture2D texture, Rect rect, int spriteWidth, int spriteHeight, params string[] sprites)
            => AddSprites(texture, rect, spriteWidth, spriteHeight, new RectOffset(), 0, sprites);

        static void AddSprites(Texture2D texture, Rect rect, int spriteWidth, int spriteHeight, RectOffset border, int space, params string[] sprites)
        {
            var width = spriteWidth / (float)texture.width * rect.width;
            var height = spriteHeight / (float)texture.height * rect.height;
            var spaceWidth = space / (float)texture.width * rect.width;
            var spaceHeight = space / (float)texture.height * rect.height;

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
        public static string Package => nameof(Package);
        public static string Clear => nameof(Clear);
        public static string Edit => nameof(Edit);
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

        public static string ResizeSprite { get; } = nameof(ResizeSprite);

        public static string FieldNormal => nameof(FieldNormal);
        public static string FieldHovered => nameof(FieldHovered);
        public static string FieldFocused => nameof(FieldFocused);
        public static string FieldDisabled => nameof(FieldDisabled);
        public static string FieldEmpty => nameof(FieldEmpty);

        public static string OpacitySliderSprite { get; } = nameof(OpacitySliderSprite);

        public static string ColorPickerNormal { get; } = nameof(ColorPickerNormal);
        public static string ColorPickerHover { get; } = nameof(ColorPickerHover);
        public static string ColorPickerColor { get; } = nameof(ColorPickerColor);

        public static string DeleteNormal { get; } = nameof(DeleteNormal);
        public static string DeleteHover { get; } = nameof(DeleteHover);
        public static string DeletePressed { get; } = nameof(DeletePressed);
        public static string ArrowDown { get; } = nameof(ArrowDown);
        public static string ArrowRight { get; } = nameof(ArrowRight);
    }
}
