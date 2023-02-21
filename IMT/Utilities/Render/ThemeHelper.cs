using ColossalFramework.Packaging;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace IMT.Utilities
{
    public class ThemeHelper
    {
        private static Dictionary<string, ThemeData> Themes { get; } = new Dictionary<string, ThemeData>();
        public static CurrentThemeData DefaultTheme { get; } = new CurrentThemeData();

        public static IEnumerable<string> ThemeNames => Themes.Keys;
        public static IEnumerable<ThemeData> ThemeDatas => Themes.Values;

        public static bool TryGetTheme(string name, out ThemeData data) => Themes.TryGetValue(name, out data);

        public static void LoadThemes()
        {
            foreach (var themeAsset in PackageManager.FilterAssets(UserAssetType.MapThemeMetaData))
            {
                if (themeAsset == null || themeAsset.package == null)
                    continue;

                if (themeAsset.fullName.Contains("CO-Winter-Theme") && !SteamHelper.IsDLCOwned(SteamHelper.DLC.SnowFallDLC))
                    continue;

                var themeData = themeAsset.Instantiate<MapThemeMetaData>();
                themeData.assetRef = themeAsset;

                Themes[themeAsset.fullName] = new ThemeData(themeData, themeAsset.fullName);
            }
        }
        public static void UnloadThemes()
        {
            Themes.Clear();
        }

        public interface IThemeData
        {
            public string Id { get; }
            public string Name { get; }

            public TextureData Pavement { get; }
            public TextureData Grass { get; }
            public TextureData Gravel { get; }
            public TextureData Ruined { get; }
            public TextureData Cliff { get; }
        }
        public class ThemeData : IThemeData
        {
            public readonly MapThemeMetaData metaData;
            public readonly Texture2D screenshot;
            public readonly string fullName;

            public string Id => fullName;
            public string Name => metaData.name;

            private TextureData pavement;
            private TextureData grass;
            private TextureData gravel;
            private TextureData ruined;
            private TextureData cliff;

            public TextureData Pavement => pavement ??= PavementGetter();
            public TextureData Grass => grass ??= GrassGetter();
            public TextureData Gravel => gravel ??= GravelGetter();
            public TextureData Ruined => ruined ??= RuinedGetter();
            public TextureData Cliff => cliff ??= CliffGetter();

            public ThemeData(MapThemeMetaData metaData, string fullName)
            {
                this.metaData = metaData;
                this.fullName = fullName;

                //if (metaData.imageRef != null)
                //    this.screenshot = metaData.imageRef.Instantiate<Texture2D>();
            }
            ~ThemeData()
            {
                if (screenshot != null)
                    UnityEngine.Object.Destroy(screenshot);
            }

            private TextureData PavementGetter()
            {
                var texture = metaData.pavementDiffuseAsset.Instantiate<Texture2D>();
                var textureData = new TextureData(texture, metaData.pavementTiling);
                return textureData;
            }
            private TextureData GrassGetter()
            {
                var texture = metaData.grassDiffuseAsset.Instantiate<Texture2D>();
                var textureData = new TextureData(texture, metaData.grassTiling);
                return textureData;
            }
            private TextureData GravelGetter()
            {
                var texture = metaData.gravelDiffuseAsset.Instantiate<Texture2D>();
                var textureData = new TextureData(texture, metaData.gravelTiling);
                return textureData;
            }
            private TextureData RuinedGetter()
            {
                var texture = metaData.ruinedDiffuseAsset.Instantiate<Texture2D>();
                var textureData = new TextureData(texture, metaData.ruinedTiling);
                return textureData;
            }
            private TextureData CliffGetter()
            {
                var texture = metaData.cliffDiffuseAsset.Instantiate<Texture2D>();
                var textureData = new TextureData(texture, metaData.cliffDiffuseTiling);
                return textureData;
            }

        }

        public class CurrentThemeData : IThemeData
        {
            public string Id => string.Empty;
            public string Name => "Default theme";

            public TextureData Pavement => throw new NotImplementedException();

            public TextureData Grass => new((Texture2D)Shader.GetGlobalTexture("_TerrainGrassDiffuse"), Shader.GetGlobalVector("_TerrainTextureTiling2").x);

            public TextureData Gravel => throw new NotImplementedException();

            public TextureData Ruined => throw new NotImplementedException();

            public TextureData Cliff => throw new NotImplementedException();
        }

        public class TextureData
        {
            public readonly Texture2D texture;
            public readonly Vector2 tiling;

            public TextureData(Texture2D texture, float tiling)
            {
                texture.wrapMode = TextureWrapMode.Repeat;
                texture.filterMode = FilterMode.Trilinear;
                texture.anisoLevel = 8;
                this.texture = texture;
                this.tiling = new Vector2(tiling, tiling);
            }

            ~TextureData()
            {
                if (texture != null)
                    UnityEngine.Object.Destroy(texture);
            }
        }
    }
}
