﻿using ColossalFramework.UI;
using IMT.UI;
using IMT.UI.Editors;
using IMT.Utilities;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace IMT.Manager
{
    public abstract class StopLineStyle : LineStyle<StopLineStyle>
    {
        public static float DefaultStopWidth { get; } = 0.3f;
        public static float DefaultStopOffset { get; } = 0.3f;

        private static Dictionary<StopLineType, StopLineStyle> Defaults { get; } = new Dictionary<StopLineType, StopLineStyle>()
        {
            {StopLineType.Solid, new SolidStopLineStyle(DefaultMarkingColor, DefaultStopWidth, DefaultEffect, DefaultEffect, DefaultTexture)},
            {StopLineType.Dashed, new DashedStopLineStyle(DefaultMarkingColor, DefaultStopWidth, DefaultEffect, DefaultEffect, DefaultTexture, DefaultDashLength, DefaultSpaceLength)},
            {StopLineType.DoubleSolid, new DoubleSolidStopLineStyle(DefaultMarkingColor, DefaultMarkingColor, false, DefaultStopWidth, DefaultEffect, DefaultEffect, DefaultTexture, DefaultStopOffset)},
            {StopLineType.DoubleDashed, new DoubleDashedStopLineStyle(DefaultMarkingColor, DefaultMarkingColor, false, DefaultStopWidth, DefaultEffect, DefaultEffect, DefaultTexture, DefaultDashLength, DefaultSpaceLength, DefaultStopOffset)},
            {StopLineType.SolidAndDashed, new SolidAndDashedStopLineStyle(DefaultMarkingColor, DefaultMarkingColor, false, DefaultWidth, DefaultEffect, DefaultEffect, DefaultTexture, DefaultDashLength, DefaultSpaceLength, DefaultStopOffset)},
            {StopLineType.SharkTeeth, new SharkTeethStopLineStyle(DefaultMarkingColor, DefaultEffect, DefaultEffect, DefaultTexture, DefaultSharkBaseLength, DefaultSharkHeight, DefaultSharkSpaceLength) },
            {StopLineType.Pavement, new PavementStopLineStyle(Default3DWidth, Default3DHeigth) },
        };
        public static StopLineStyle GetDefault(StopLineType type)
        {
            return Defaults.TryGetValue(type, out var style) ? style.CopyLineStyle() : null;
        }

        public StopLineStyle(Color32 color, float width, Vector2 cracks, Vector2 voids, float texture) : base(color, width, cracks, voids, texture) { }
        public StopLineStyle(Color32 color, float width) : base(color, width) { }

        public sealed override void Calculate(MarkingLine line, ITrajectory trajectory, Action<IStyleData> addData)
        {
            if (line is MarkingStopLine stopLine)
            {
                foreach (var lod in EnumExtension.GetEnumValues<MarkingLOD>())
                {
                    if ((SupportLOD & lod) != 0)
                        CalculateImpl(stopLine, trajectory, lod, addData);
                }
            }
        }
        protected abstract void CalculateImpl(MarkingStopLine line, ITrajectory trajectory, MarkingLOD lod, Action<IStyleData> addData);

        public sealed override void GetUIComponents(EditorProvider provider)
        {
            base.GetUIComponents(provider);
            if (provider.editor.EditObject is MarkingLineRawRule rule && rule.Line is MarkingStopLine line)
                GetUIComponents(line, provider);
            else
                GetUIComponents(null, provider);
        }
        protected virtual void GetUIComponents(MarkingStopLine line, EditorProvider provider) { }

        public enum StopLineType
        {
            [Description(nameof(Localize.LineStyle_StopSolid))]
            [NetworkType(NetworkType.Road)]
            [LineType(LineType.Stop)]
            Solid = StyleType.StopLineSolid,

            [Description(nameof(Localize.LineStyle_StopDashed))]
            [NetworkType(NetworkType.Road)]
            [LineType(LineType.Stop)]
            Dashed = StyleType.StopLineDashed,

            [Description(nameof(Localize.LineStyle_StopDouble))]
            [NetworkType(NetworkType.Road)]
            [LineType(LineType.Stop)]
            DoubleSolid = StyleType.StopLineDoubleSolid,

            [Description(nameof(Localize.LineStyle_StopDoubleDashed))]
            [NetworkType(NetworkType.Road)]
            [LineType(LineType.Stop)]
            DoubleDashed = StyleType.StopLineDoubleDashed,

            [Description(nameof(Localize.LineStyle_StopSolidAndDashed))]
            [NetworkType(NetworkType.Road)]
            [LineType(LineType.Stop)]
            SolidAndDashed = StyleType.StopLineSolidAndDashed,

            [Description(nameof(Localize.LineStyle_StopSharkTeeth))]
            [NetworkType(NetworkType.Road)]
            [LineType(LineType.Stop)]
            SharkTeeth = StyleType.StopLineSharkTeeth,

            [Description(nameof(Localize.LineStyle_StopPavement))]
            [NetworkType(NetworkType.Road)]
            [LineType(LineType.Stop)]
            Pavement = StyleType.StopLinePavement,

            [Description(nameof(Localize.Style_FromClipboard))]
            [NotVisible]
            Buffer = StyleType.StopLineBuffer,
        }
    }
}
