using ColossalFramework.UI;
using IMT.API;
using IMT.Utilities;
using IMT.Utilities.API;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace IMT.Manager
{
    public class CliffFillerStyle : CurbFillerStyle
    {
        public override StyleType Type => StyleType.FillerCliff;
        public override MarkingLOD SupportLOD => MarkingLOD.LOD0 | MarkingLOD.LOD1;

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
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

        public CliffFillerStyle(Vector2 offset, float elevation, Vector2 cornerRadius, Vector2 curbSize) : base(offset, elevation, cornerRadius, curbSize) { }

        public override BaseFillerStyle CopyStyle() => new CliffFillerStyle(Offset, Elevation, CornerRadius, CurbSize);

        protected override FillerMeshData.TextureData GetTopTexture()
        {
            var texture = (Texture2D)Shader.GetGlobalTexture("_TerrainCliffDiffuse");
            var size = Shader.GetGlobalVector("_TerrainTextureTiling1");
            var tiling = new Vector2(size.w, size.w);
            var textureData = new FillerMeshData.TextureData(texture, UnityEngine.Color.white, tiling, 0f);
            return textureData;
        }
    }
}
