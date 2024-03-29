﻿using ColossalFramework.Packaging;
using ModsCommon;
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
        public static int ThemeCount => Themes.Count;

        public static bool TryGetTheme(string name, out ThemeData data) => Themes.TryGetValue(name, out data);

        public static void LoadThemes()
        {
            SingletonMod<Mod>.Logger.Debug($"Start loading map themes");

            foreach (var themeAsset in PackageManager.FilterAssets(UserAssetType.MapThemeMetaData))
            {
                if (themeAsset == null || themeAsset.package == null)
                    continue;

                var themeData = themeAsset.Instantiate<MapThemeMetaData>();
                themeData.assetRef = themeAsset;

                Themes[themeAsset.fullName] = new ThemeData(themeData, themeAsset.fullName);
            }

            SingletonMod<Mod>.Logger.Debug($"{Themes.Count} map themes loaded");
        }
        public static void UnloadThemes()
        {
            SingletonMod<Mod>.Logger.Debug($"Unloading map themes");
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
            public TextureData Asphalt { get; }

            public TextureData GetTexture(TextureType type);
        }
        public class ThemeData : IThemeData
        {
            public readonly MapThemeMetaData metaData;
            public readonly string fullName;

            public string Id => fullName;
            public string Name => metaData.name;

            private TextureData pavement;
            private TextureData grass;
            private TextureData gravel;
            private TextureData ruined;
            private TextureData cliff;
            private TextureData asphalt;

            public TextureData Pavement => pavement ??= PavementGetter();
            public TextureData Grass => grass ??= GrassGetter();
            public TextureData Gravel => gravel ??= GravelGetter();
            public TextureData Ruined => ruined ??= RuinedGetter();
            public TextureData Cliff => cliff ??= CliffGetter();
            public TextureData Asphalt => asphalt ??= AsphaltGetter();

            public ThemeData(MapThemeMetaData metaData, string fullName)
            {
                this.metaData = metaData;
                this.fullName = fullName;
            }

            public TextureData GetTexture(TextureType type) => type switch
            {
                TextureType.Asphalt => Asphalt,
                TextureType.Pavement => Pavement,
                TextureType.Grass => Grass,
                TextureType.Gravel => Gravel,
                TextureType.Cliff => Cliff,
                TextureType.Ruined => Ruined,
                _ => throw new NotSupportedException(),
            };

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
            private TextureData AsphaltGetter()
            {
                var texture = metaData.upwardRoadDiffuse.Instantiate<Texture2D>();
                var textureData = new TextureData(texture, 0.0625f);
                return textureData;
            }

        }

        public class CurrentThemeData : IThemeData
        {
            public string Id => string.Empty;
            public string Name => Localize.StyleOption_DefaultTheme;

            public TextureData Pavement => new((Texture2D)Shader.GetGlobalTexture("_TerrainPavementDiffuse"), Shader.GetGlobalVector("_TerrainTextureTiling1").x, false);

            public TextureData Grass => new((Texture2D)Shader.GetGlobalTexture("_TerrainGrassDiffuse"), Shader.GetGlobalVector("_TerrainTextureTiling2").x, false);

            public TextureData Gravel => new((Texture2D)Shader.GetGlobalTexture("_TerrainGravelDiffuse"), Shader.GetGlobalVector("_TerrainTextureTiling2").y, false);

            public TextureData Ruined => new((Texture2D)Shader.GetGlobalTexture("_TerrainRuinedDiffuse"), Shader.GetGlobalVector("_TerrainTextureTiling1").y, false);

            public TextureData Cliff => new((Texture2D)Shader.GetGlobalTexture("_TerrainCliffDiffuse"), Shader.GetGlobalVector("_TerrainTextureTiling1").w, false);

            public TextureData Asphalt => new((Texture2D)Shader.GetGlobalTexture("_RoadUpwardDiffuse"), 0.0625f, false);

            public TextureData GetTexture(TextureType type) => type switch
            {
                TextureType.Asphalt => Asphalt,
                TextureType.Pavement => Pavement,
                TextureType.Grass => Grass,
                TextureType.Gravel => Gravel,
                TextureType.Cliff => Cliff,
                TextureType.Ruined => Ruined,
                _ => throw new NotSupportedException(),
            };
        }

        public class TextureData
        {
            public readonly Texture2D texture;
            public readonly Vector2 tiling;
            private readonly bool destroyTexture;

            public TextureData(Texture2D texture, float tiling, bool destroyTexture = true)
            {
                if (destroyTexture)
                {
                    texture.wrapMode = TextureWrapMode.Repeat;
                    texture.filterMode = FilterMode.Trilinear;
                    texture.anisoLevel = 8;
                }
                this.texture = texture;
                this.tiling = new Vector2(tiling, tiling);
                this.destroyTexture = destroyTexture;
            }

            ~TextureData()
            {
                if (destroyTexture && texture != null)
                    UnityEngine.Object.Destroy(texture);
            }
        }

        public enum TextureType
        {
            Asphalt,
            Pavement,
            Grass,
            Gravel,
            Cliff,
            Ruined,
        }
    }
}
