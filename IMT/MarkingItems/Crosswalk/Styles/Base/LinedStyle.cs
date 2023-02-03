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
    public abstract class LinedCrosswalkStyle : CustomCrosswalkStyle, ICrosswalkStyle, ILinedCrosswalk
    {
        public PropertyValue<float> LineWidth { get; }

        public LinedCrosswalkStyle(Color32 color, float width, Vector2 scratches, Vector2 voids, float offsetBefore, float offsetAfter, float lineWidth) : base(color, width, scratches, voids, offsetBefore, offsetAfter)
        {
            LineWidth = GetLineWidthProperty(lineWidth);
        }
        protected override float GetVisibleWidth(MarkingCrosswalk crosswalk) => Width / Mathf.Sin(crosswalk.CornerAndNormalAngle);
        public override void CopyTo(CrosswalkStyle target)
        {
            base.CopyTo(target);
            if (target is ILinedCrosswalk linedTarget)
                linedTarget.LineWidth.Value = LineWidth;
        }
        public override void GetUIComponents(MarkingCrosswalk crosswalk, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(crosswalk, components, parent, isTemplate);
            components.Add(AddLineWidthProperty(this, parent, false));
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            LineWidth.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            LineWidth.FromXml(config, DefaultCrosswalkOffset);
        }
    }
}
