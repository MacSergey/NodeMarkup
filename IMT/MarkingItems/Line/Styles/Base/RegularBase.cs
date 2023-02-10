using ColossalFramework.UI;
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
    public abstract class RegularLineStyle : LineStyle<RegularLineStyle>
    {
        private static Dictionary<RegularLineType, RegularLineStyle> Defaults { get; } = new Dictionary<RegularLineType, RegularLineStyle>()
        {
            {RegularLineType.Solid, new SolidLineStyle(DefaultColor, DefaultWidth, DefaultEffect, DefaultEffect, DefaultTexture)},
            {RegularLineType.Dashed, new DashedLineStyle(DefaultColor, DefaultWidth, DefaultEffect, DefaultEffect, DefaultTexture, DefaultDashLength, DefaultSpaceLength)},
            {RegularLineType.DoubleSolid, new DoubleSolidLineStyle(DefaultColor, DefaultColor, false, DefaultWidth, DefaultEffect, DefaultEffect, DefaultTexture, DefaultDoubleOffset)},
            {RegularLineType.DoubleDashed, new DoubleDashedLineStyle(DefaultColor, DefaultColor, false, DefaultWidth, DefaultEffect, DefaultEffect, DefaultTexture, DefaultDashLength, DefaultSpaceLength, DefaultDoubleOffset)},
            {RegularLineType.DoubleDashedAsym, new DoubleDashedAsymLineStyle(DefaultColor, DefaultColor, false, DefaultWidth, DefaultEffect, DefaultEffect, DefaultTexture, DefaultDashLength, DefaultSpaceLength, DefaultSpaceLength * 2f, DefaultDoubleOffset)},
            {RegularLineType.SolidAndDashed, new SolidAndDashedLineStyle(DefaultColor, DefaultColor, false, DefaultWidth, DefaultEffect, DefaultEffect, DefaultTexture, DefaultDashLength, DefaultSpaceLength, DefaultDoubleOffset)},
            {RegularLineType.SharkTeeth, new SharkTeethLineStyle(DefaultColor, DefaultEffect, DefaultEffect, DefaultTexture, DefaultSharkBaseLength, DefaultSharkHeight, DefaultSharkSpaceLength, DefaultSharkAngle) },
            {RegularLineType.ZigZag, new ZigZagLineStyle(DefaultColor, DefaultWidth, DefaultEffect, DefaultEffect, DefaultTexture, ZigZagStep, ZigZagOffset, true, true) },
            {RegularLineType.Pavement, new PavementLineStyle(Default3DWidth, Default3DHeigth) },
            {RegularLineType.Prop, new PropLineStyle(null, DefaultObjectProbability, PropLineStyle.DefaultColorOption, PropLineStyle.DefaultColor, DefaultObjectStep, new Vector2(DefaultObjectAngle, DefaultObjectAngle), new Vector2(DefaultObjectAngle, DefaultObjectAngle), new Vector2(DefaultObjectAngle, DefaultObjectAngle), new Vector2(DefaultObjectShift,DefaultObjectShift), new Vector2(DefaultObjectScale, DefaultObjectScale), new Vector2(DefaultObjectElevation,DefaultObjectElevation), DefaultObjectOffsetBefore, DefaultObjectOffsetAfter, DistributionType.FixedSpaceFreeEnd) },
            {RegularLineType.Tree, new TreeLineStyle(null, DefaultObjectProbability, DefaultObjectStep, new Vector2(DefaultObjectAngle, DefaultObjectAngle), new Vector2(DefaultObjectAngle, DefaultObjectAngle), new Vector2(DefaultObjectAngle, DefaultObjectAngle), new Vector2(DefaultObjectShift,DefaultObjectShift), new Vector2(DefaultObjectScale, DefaultObjectScale), new Vector2(DefaultObjectElevation,DefaultObjectElevation), DefaultObjectOffsetBefore, DefaultObjectOffsetAfter, DistributionType.FixedSpaceFreeEnd) },
            {RegularLineType.Text, new RegularLineStyleText(DefaultColor, DefaultEffect, DefaultEffect, DefaultTexture, string.Empty, string.Empty, DefaultTextScale, DefaultObjectAngle, DefaultObjectShift, RegularLineStyleText.TextDirection.LeftToRight, Vector2.zero, RegularLineStyleText.TextAlignment.Middle)},
            {RegularLineType.Network, new NetworkLineStyle(null, DefaultObjectShift, DefaultObjectElevation, DefaultNetworkScale, DefaultObjectOffsetBefore, DefaultObjectOffsetAfter, DefaultRepeatDistance, false) },
        };
        public static RegularLineStyle GetDefault(RegularLineType type)
        {
            return Defaults.TryGetValue(type, out var style) ? style.CopyLineStyle() : null;
        }

        public RegularLineStyle(Color32 color, float width, Vector2 cracks, Vector2 voids, float texture) : base(color, width, cracks, voids, texture) { }
        public RegularLineStyle(Color32 color, float width) : base(color, width) { }

        public sealed override void Calculate(MarkingLine line, ITrajectory trajectory, Action<IStyleData> addData)
        {
            if (line is MarkingRegularLine regularLine)
            {
                foreach (var lod in EnumExtension.GetEnumValues<MarkingLOD>())
                {
                    if ((SupportLOD & lod) != 0)
                        CalculateImpl(regularLine, trajectory, lod, addData);
                }
            }
        }
        protected abstract void CalculateImpl(MarkingRegularLine line, ITrajectory trajectory, MarkingLOD lod, Action<IStyleData> addData);

        public sealed override void GetUIComponents(EditorProvider provider)
        {
            base.GetUIComponents(provider);

            if (provider.editor.EditObject is MarkingLineRawRule rule && rule.Line is MarkingRegularLine line)
                GetUIComponents(line, provider);
            else
                GetUIComponents(null, provider);
        }
        protected virtual void GetUIComponents(MarkingRegularLine line, EditorProvider provider) { }

        public enum RegularLineType
        {
            [Description(nameof(Localize.LineStyle_Solid))]
            [NetworkType(NetworkType.Path | NetworkType.Road | NetworkType.Taxiway)]
            [LineType(LineType.Regular | LineType.Crosswalk)]
            [Order(0)]
            Solid = StyleType.LineSolid,

            [Description(nameof(Localize.LineStyle_Dashed))]
            [NetworkType(NetworkType.Path | NetworkType.Road | NetworkType.Taxiway)]
            [LineType(LineType.Regular | LineType.Crosswalk)]
            [Order(1)]
            Dashed = StyleType.LineDashed,

            [Description(nameof(Localize.LineStyle_DoubleSolid))]
            [NetworkType(NetworkType.Path | NetworkType.Road | NetworkType.Taxiway)]
            [LineType(LineType.Regular | LineType.Crosswalk)]
            [Order(2)]
            DoubleSolid = StyleType.LineDoubleSolid,

            [Description(nameof(Localize.LineStyle_DoubleDashed))]
            [NetworkType(NetworkType.Path | NetworkType.Road | NetworkType.Taxiway)]
            [LineType(LineType.Regular | LineType.Crosswalk)]
            [Order(3)]
            DoubleDashed = StyleType.LineDoubleDashed,

            [Description(nameof(Localize.LineStyle_SolidAndDashed))]
            [NetworkType(NetworkType.Path | NetworkType.Road | NetworkType.Taxiway)]
            [LineType(LineType.Regular | LineType.Crosswalk)]
            [Order(5)]
            SolidAndDashed = StyleType.LineSolidAndDashed,

            [Description(nameof(Localize.LineStyle_SharkTeeth))]
            [NetworkType(NetworkType.Path | NetworkType.Road | NetworkType.Taxiway)]
            [LineType(LineType.Regular | LineType.Crosswalk)]
            [Order(6)]
            SharkTeeth = StyleType.LineSharkTeeth,

            [Description(nameof(Localize.LineStyle_DoubleDashedAsym))]
            [NetworkType(NetworkType.Path | NetworkType.Road | NetworkType.Taxiway)]
            [LineType(LineType.Regular | LineType.Crosswalk)]
            [Order(4)]
            DoubleDashedAsym = StyleType.LineDoubleDashedAsym,

            [Description(nameof(Localize.LineStyle_ZigZag))]
            [NetworkType(NetworkType.Road | NetworkType.Path | NetworkType.Taxiway)]
            [LineType(LineType.Regular)]
            [Order(7)]
            ZigZag = StyleType.LineZigZag,

            [Description(nameof(Localize.LineStyle_Pavement))]
            [NetworkType(NetworkType.All)]
            [LineType(LineType.Regular | LineType.Crosswalk)]
            [Order(8)]
            Pavement = StyleType.LinePavement,

            [Description(nameof(Localize.LineStyle_Prop))]
            [NetworkType(NetworkType.All)]
            [LineType(LineType.Regular | LineType.Crosswalk | LineType.Lane)]
            [Order(9)]
            Prop = StyleType.LineProp,

            [Description(nameof(Localize.LineStyle_Tree))]
            [NetworkType(NetworkType.All)]
            [LineType(LineType.Regular | LineType.Crosswalk | LineType.Lane)]
            [Order(10)]
            Tree = StyleType.LineTree,

            [Description(nameof(Localize.LineStyle_Text))]
            [NetworkType(NetworkType.Road | NetworkType.Path | NetworkType.Taxiway)]
            [LineType(LineType.Regular | LineType.Crosswalk | LineType.Lane)]
            Text = StyleType.LineText,

            [Description(nameof(Localize.LineStyle_Network))]
            [NetworkType(NetworkType.All)]
            [LineType(LineType.Regular | LineType.Crosswalk | LineType.Lane)]
            [Order(11)]
            Network = StyleType.LineNetwork,

            [Description(nameof(Localize.LineStyle_Empty))]
            [NetworkType(NetworkType.All)]
            [LineType(LineType.Regular | LineType.Crosswalk | LineType.Lane)]
            [NotVisible]
            Empty = StyleType.EmptyLine,

            [Description(nameof(Localize.Style_FromClipboard))]
            [NetworkType(NetworkType.All)]
            [LineType(LineType.Regular | LineType.Crosswalk | LineType.Lane)]
            [NotVisible]
            Buffer = StyleType.LineBuffer,
        }
    }
}
