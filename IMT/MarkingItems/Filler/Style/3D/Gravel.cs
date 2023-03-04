using IMT.API;
using IMT.Utilities;
using IMT.Utilities.API;
using System.Collections.Generic;
using UnityEngine;

namespace IMT.Manager
{
    public class GravelFillerStyle : ThemeFillerStyle
    {
        public override StyleType Type => StyleType.FillerGravel;
        public override MarkingLOD SupportLOD => MarkingLOD.LOD0 | MarkingLOD.LOD1;
        protected override ThemeHelper.TextureType TextureType => ThemeHelper.TextureType.Gravel;

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(PavementTheme);
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

        public GravelFillerStyle(ThemeHelper.IThemeData pavementTheme, ThemeHelper.IThemeData theme, Vector2 offset, float elevation, Vector2 cornerRadius, Vector2 curbSize) : base(pavementTheme, theme, offset, elevation, cornerRadius, curbSize) { }

        public override BaseFillerStyle CopyStyle() => new GravelFillerStyle(PavementTheme.Value, Theme.Value, Offset, Elevation, CornerRadius, CurbSize);
    }
}
