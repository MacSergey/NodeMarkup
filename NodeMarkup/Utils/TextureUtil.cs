using ColossalFramework.PlatformServices;
using ColossalFramework.UI;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace NodeMarkup.Utils
{
    public static class TextureUtil
    {
        public static UITextureAtlas InGameAtlas { get; } = GetAtlas("Ingame");

        public static string DeleteNormal { get; } = nameof(DeleteNormal);
        public static string DeleteHover { get; } = nameof(DeleteHover);
        public static string DeletePressed { get; } = nameof(DeletePressed);
        public static string ArrowDown { get; } = nameof(ArrowDown);
        public static string ArrowRight { get; } = nameof(ArrowRight);
        private static string[] DeleteSprites { get; } = new string[] { DeleteNormal, DeleteHover, DeletePressed, ArrowDown, ArrowRight };
        public static UITextureAtlas AdditionalAtlas { get; } = CreateTextureAtlas("AdditionalButtons.png", nameof(AdditionalAtlas), 32, 32, DeleteSprites);

        public static UITextureAtlas CreateTextureAtlas(string textureFile, string atlasName, int spriteWidth, int spriteHeight, string[] spriteNames, RectOffset border = null, int space = 0)
        {
            var atlas = GetAtlas(atlasName);

            if (atlas == UIView.GetAView().defaultAtlas)
            {
                var texture = LoadTextureFromAssembly(textureFile, spriteWidth * spriteNames.Length + space * (spriteNames.Length + 1), spriteHeight + 2 * space);

                atlas = ScriptableObject.CreateInstance<UITextureAtlas>();
                var material = Object.Instantiate(UIView.GetAView().defaultAtlas.material);
                material.mainTexture = texture;
                atlas.material = material;
                atlas.name = atlasName;

                var heightRatio = spriteHeight / (float)texture.height;
                var widthRatio = spriteWidth / (float)texture.width;
                var spaceHeightRatio = space / (float)texture.height;
                var spaceWidthRatio = space / (float)texture.width;

                for (int i = 0; i < spriteNames.Length; i += 1)
                    atlas.AddSprite(spriteNames[i], new Rect(i * widthRatio + (i + 1) * spaceWidthRatio, spaceHeightRatio, widthRatio, heightRatio), border);
            }

            return atlas;
        }
        public static void AddSprite(this UITextureAtlas atlas, string name, Rect region, RectOffset border = null)
        {
            UITextureAtlas.SpriteInfo spriteInfo = new UITextureAtlas.SpriteInfo
            {
                name = name,
                texture = atlas.material.mainTexture as Texture2D,
                region = region,
                border = border ?? new RectOffset()
            };
            atlas.AddSprite(spriteInfo);
        }

        public static void AddTexturesInAtlas(UITextureAtlas atlas, Texture2D[] newTextures, bool locked = false)
        {
            Texture2D[] textures = new Texture2D[atlas.count + newTextures.Length];

            for (int i = 0; i < atlas.count; i++)
            {
                Texture2D texture2D = atlas.sprites[i].texture;

                if (locked)
                {
                    // Locked textures workaround
                    RenderTexture renderTexture = RenderTexture.GetTemporary(texture2D.width, texture2D.height, 0);
                    Graphics.Blit(texture2D, renderTexture);

                    RenderTexture active = RenderTexture.active;
                    texture2D = new Texture2D(renderTexture.width, renderTexture.height);
                    RenderTexture.active = renderTexture;
                    texture2D.ReadPixels(new Rect(0f, 0f, (float)renderTexture.width, (float)renderTexture.height), 0, 0);
                    texture2D.Apply();
                    RenderTexture.active = active;

                    RenderTexture.ReleaseTemporary(renderTexture);
                }

                textures[i] = texture2D;
                textures[i].name = atlas.sprites[i].name;
            }

            for (int i = 0; i < newTextures.Length; i++)
                textures[atlas.count + i] = newTextures[i];

            Rect[] regions = atlas.texture.PackTextures(textures, atlas.padding, 4096, false);

            atlas.sprites.Clear();

            for (int i = 0; i < textures.Length; i++)
            {
                UITextureAtlas.SpriteInfo spriteInfo = atlas[textures[i].name];
                atlas.sprites.Add(new UITextureAtlas.SpriteInfo
                {
                    texture = textures[i],
                    name = textures[i].name,
                    border = (spriteInfo != null) ? spriteInfo.border : new RectOffset(),
                    region = regions[i]
                });
            }

            atlas.RebuildIndexes();
        }

        public static UITextureAtlas GetAtlas(string name)
        {
            UITextureAtlas[] atlases = Resources.FindObjectsOfTypeAll(typeof(UITextureAtlas)) as UITextureAtlas[];
            for (int i = 0; i < atlases.Length; i++)
            {
                if (atlases[i].name == name)
                    return atlases[i];
            }
            return UIView.GetAView().defaultAtlas;
        }

        public static Texture2D LoadTextureFromAssembly(string textureFile, int width, int height)
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            var path = $"{nameof(NodeMarkup)}.Resources.{textureFile}";
            var manifestResourceStream = executingAssembly.GetManifestResourceStream(path);
            var array = new byte[manifestResourceStream.Length];
            manifestResourceStream.Read(array, 0, array.Length);

            var texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.LoadImage(array);
            texture.Apply(true, true);

            return texture;
        }
    }
}
