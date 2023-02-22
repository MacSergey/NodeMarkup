using IMT.API;
using IMT.Utilities;
using IMT.Utilities.API;
using System.Collections.Generic;
using UnityEngine;

namespace IMT.Manager
{
    public class GrassFillerStyle : ThemeFillerStyle
    {
        public override StyleType Type => StyleType.FillerGrass;
        public override MarkingLOD SupportLOD => MarkingLOD.LOD0 | MarkingLOD.LOD1;
        protected override ThemeHelper.TextureType TextureType => ThemeHelper.TextureType.Grass;

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Theme);
                yield return nameof(Elevation);
                yield return nameof(CornerRadius);
                yield return nameof(CurbSize);
                yield return nameof(Offset);
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;
        public override IEnumerable<IStylePropertyData> Properties
        {
            get
            {
                yield return new StylePropertyDataProvider<float>(nameof(Elevation), Elevation);
                //yield return new StylePropertyDataProvider<float>(nameof(CornerRadius), CornerRadius);
                //yield return new StylePropertyDataProvider<float>(nameof(MedianCornerRadius), MedianCornerRadius);
                //yield return new StylePropertyDataProvider<float>(nameof(CurbSize), CurbSize);
                //yield return new StylePropertyDataProvider<float>(nameof(MedianCurbSize), MedianCurbSize);
                //yield return new StylePropertyDataProvider<float>(nameof(LineOffset), LineOffset);
                //yield return new StylePropertyDataProvider<float>(nameof(MedianOffset), MedianOffset);
            }
        }

        public GrassFillerStyle(ThemeHelper.IThemeData theme, Vector2 offset, float elevation, Vector2 cornerRadius, Vector2 curbSize) : base(theme, offset, elevation, cornerRadius, curbSize) { }

        public override BaseFillerStyle CopyStyle() => new GrassFillerStyle(Theme.Value, Offset, Elevation, CornerRadius, CurbSize);

        protected override FillerMeshData.TextureData GetTopTexture()
        {
            if (Theme.Value is ThemeHelper.IThemeData themeData)
            {
                var grass = themeData.Grass;
                var textureData = new FillerMeshData.TextureData(grass.texture, UnityEngine.Color.white, grass.tiling, 0f);
                return textureData;
            }
            else
            {
                var texture = (Texture2D)Shader.GetGlobalTexture("_TerrainGrassDiffuse");
                var size = Shader.GetGlobalVector("_TerrainTextureTiling2");
                var tiling = new Vector2(size.x, size.x);
                var textureData = new FillerMeshData.TextureData(texture, UnityEngine.Color.white, tiling, 0f);
                return textureData;
            }
        }
    }
}
