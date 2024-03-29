﻿using ColossalFramework.UI;
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
    public class PavementFillerStyle : MeshFillerStyle
    {
        public override StyleType Type => StyleType.FillerPavement;
        public override MarkingLOD SupportLOD => MarkingLOD.LOD0 | MarkingLOD.LOD1;

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(PavementTheme);
                yield return nameof(Elevation);
                yield return nameof(CornerRadius);
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
                //yield return new StylePropertyDataProvider<float>(nameof(LineOffset), LineOffset);
                //yield return new StylePropertyDataProvider<float>(nameof(MedianOffset), MedianOffset);
            }
        }

        public PavementFillerStyle(ThemeHelper.IThemeData pavementTheme, Vector2 offset, float elevation, Vector2 cornerRadius) : base(pavementTheme, offset, elevation, cornerRadius) { }

        public override BaseFillerStyle CopyStyle() => new PavementFillerStyle(PavementTheme.Value, Offset, Elevation, CornerRadius);
    }
}
