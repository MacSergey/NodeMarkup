using ColossalFramework.UI;
using IMT.API;
using IMT.MarkingItems.Crosswalk.Styles.Base;
using IMT.UI;
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
    public class ExistCrosswalkStyle : CrosswalkStyle, IWidthStyle
    {
        public override StyleType Type => StyleType.CrosswalkExistent;
        public override MarkingLOD SupportLOD => 0;

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Width);
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;
        public override IEnumerable<IStylePropertyData> Properties
        {
            get
            {
                yield return new StylePropertyDataProvider<float>(nameof(Width), Width);
            }
        }

        public override float GetTotalWidth(MarkingCrosswalk crosswalk) => Width;

        public ExistCrosswalkStyle(float width) : base(new Color32(0, 0, 0, 0), width) { }

        protected override void CalculateImpl(MarkingCrosswalk crosswalk, MarkingLOD lod, Action<IStyleData> addData) { }
        public override CrosswalkStyle CopyStyle() => new ExistCrosswalkStyle(Width);

        public override XElement ToXml()
        {
            var config = BaseToXml();
            Width.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            Width.FromXml(config, DefaultCrosswalkWidth);
        }
    }
}
