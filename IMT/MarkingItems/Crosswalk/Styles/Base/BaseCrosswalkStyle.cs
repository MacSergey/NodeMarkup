using IMT.UI.Editors;
using IMT.Utilities;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

namespace IMT.Manager
{
    public abstract class BaseCrosswalkStyle : Style<BaseCrosswalkStyle>
    {
        public static float DefaultCrosswalkWidth { get; } = 2f;
        public static float DefaultCrosswalkDashLength { get; } = 0.4f;
        public static float DefaultCrosswalkSpaceLength { get; } = 0.6f;
        public static float DefaultCrosswalkOffset { get; } = 0.3f;

        public static float DefaultCrosswalkSquareSide { get; } = 1f;
        public static int DefaultCrosswalkLineCount { get; } = 2;

        public static int DefaulCrosswalkGapPeriod => 2;

        protected static string Gap => string.Empty;

        private static Dictionary<CrosswalkType, BaseCrosswalkStyle> Defaults { get; } = new Dictionary<CrosswalkType, BaseCrosswalkStyle>()
        {
            {CrosswalkType.Existent, new ExistCrosswalkStyle(DefaultCrosswalkWidth) },
            {CrosswalkType.Zebra, new ZebraCrosswalkStyle(DefaultMarkingColor, DefaultMarkingColor, false, DefaultCrosswalkWidth, DefaultEffect, DefaultEffect, DefaultTexture, DefaultCrosswalkOffset, DefaultCrosswalkOffset, DefaultCrosswalkDashLength, DefaultCrosswalkSpaceLength, false, DefaultCrosswalkSpaceLength, DefaulCrosswalkGapPeriod, ZebraCrosswalkStyle.DashEnd.ParallelStraight) },
            {CrosswalkType.DoubleZebra, new DoubleZebraCrosswalkStyle(DefaultMarkingColor, DefaultMarkingColor, false, DefaultCrosswalkWidth, DefaultEffect, DefaultEffect, DefaultTexture, DefaultCrosswalkOffset, DefaultCrosswalkOffset, DefaultCrosswalkDashLength, DefaultCrosswalkSpaceLength, false, DefaultCrosswalkSpaceLength, DefaulCrosswalkGapPeriod, ZebraCrosswalkStyle.DashEnd.ParallelStraight, DefaultCrosswalkOffset) },
            {CrosswalkType.ParallelSolidLines, new ParallelSolidLinesCrosswalkStyle(DefaultMarkingColor, DefaultCrosswalkWidth, DefaultEffect, DefaultEffect, DefaultTexture, DefaultCrosswalkOffset, DefaultCrosswalkOffset, DefaultWidth) },
            {CrosswalkType.ParallelDashedLines, new ParallelDashedLinesCrosswalkStyle(DefaultMarkingColor, DefaultCrosswalkWidth, DefaultEffect, DefaultEffect, DefaultTexture, DefaultCrosswalkOffset, DefaultCrosswalkOffset, DefaultWidth, DefaultDashLength, DefaultSpaceLength) },
            {CrosswalkType.Ladder, new LadderCrosswalkStyle(DefaultMarkingColor, DefaultCrosswalkWidth, DefaultEffect, DefaultEffect, DefaultTexture, DefaultCrosswalkOffset, DefaultCrosswalkOffset, DefaultCrosswalkDashLength, DefaultCrosswalkSpaceLength, DefaultWidth) },
            {CrosswalkType.Solid, new SolidCrosswalkStyle(DefaultMarkingColor, DefaultCrosswalkWidth, DefaultEffect, DefaultEffect, DefaultTexture, DefaultCrosswalkOffset, DefaultCrosswalkOffset) },
            {CrosswalkType.ChessBoard, new ChessBoardCrosswalkStyle(DefaultMarkingColor, DefaultEffect, DefaultEffect, DefaultTexture, DefaultCrosswalkOffset, DefaultCrosswalkOffset, DefaultCrosswalkSquareSide, DefaultCrosswalkLineCount, false) },
            {CrosswalkType.Decal, new DecalCrosswalkStyle(null, null, DefaultCrosswalkWidth, Vector2.one, 0f, DefaultCrosswalkOffset, DefaultCrosswalkOffset) },
        };
        public static BaseCrosswalkStyle GetDefault(CrosswalkType type)
        {
            return Defaults.TryGetValue(type, out var style) ? style.CopyStyle() : null;
        }

        protected override float WidthWheelStep => 0.1f;
        protected override float WidthMinValue => 0.1f;

        public abstract float GetTotalWidth(MarkingCrosswalk crosswalk);

        public BaseCrosswalkStyle(Color32 color, float width, Vector2 cracks, Vector2 voids, float texture) : base(color, width, cracks, voids, texture) { }
        public BaseCrosswalkStyle(Color32 color, float width) : base(color, width) { }

        public sealed override void GetUIComponents(EditorProvider provider)
        {
            base.GetUIComponents(provider);
            if (provider.editor.EditObject is MarkingCrosswalk crosswalk)
                GetUIComponents(crosswalk, provider);
            else
                GetUIComponents(null, provider);
        }
        protected virtual void GetUIComponents(MarkingCrosswalk crosswalk, EditorProvider provider) { }

        public void Calculate(MarkingCrosswalk crosswalk, Action<IStyleData> addData)
        {
            foreach (var lod in EnumExtension.GetEnumValues<MarkingLOD>())
            {
                if ((SupportLOD & lod) != 0)
                    CalculateImpl(crosswalk, lod, addData);
            }
        }
        protected abstract void CalculateImpl(MarkingCrosswalk crosswalk, MarkingLOD lod, Action<IStyleData> addData);

        protected void AddCrosswalkLengthProperty(Vector2PropertyPanel lengthProperty, EditorProvider provider)
        {
            if (this is IDashedCrosswalk dashedStyle)
            {
                lengthProperty.Text = Localize.StyleOption_Length;
                lengthProperty.FieldsWidth = 50f;
                lengthProperty.SetLabels(Localize.StyleOption_Dash, Localize.StyleOption_Space);
                lengthProperty.Format = Localize.NumberFormat_Meter;
                lengthProperty.UseWheel = true;
                lengthProperty.WheelStep = new Vector2(0.1f, 0.1f);
                lengthProperty.WheelTip = Settings.ShowToolTip;
                lengthProperty.CheckMin = true;
                lengthProperty.MinValue = new Vector2(0.1f, 0.1f);
                lengthProperty.Init(0, 1);
                lengthProperty.Value = new Vector2(dashedStyle.DashLength, dashedStyle.SpaceLength);
                lengthProperty.OnValueChanged += (value) =>
                    {
                        dashedStyle.DashLength.Value = value.x;
                        dashedStyle.SpaceLength.Value = value.y;
                    };
            }
            else
                throw new NotSupportedException();
        }

        protected void CalculateCrosswalkPart(ITrajectory trajectory, StyleHelper.PartT part, Vector3 direction, Contour crosswalkContour, Color32 color, MarkingLOD lod, Action<IStyleData> addData)
        {
            var startPos = trajectory.Position(part.start);
            var startLine = new StraightTrajectory(startPos, startPos + direction, false);

            var endPos = trajectory.Position(part.end);
            var endLine = new StraightTrajectory(endPos, endPos + direction, false);

            CalculateCrosswalkPart(crosswalkContour, startLine, endLine, color, lod, addData);
        }
        protected void CalculateCrosswalkPart(Contour crosswalkContour, ITrajectory startBorder, ITrajectory endBorder, Color32 color, MarkingLOD lod, Action<IStyleData> addData)
        {
            var cutContours = new Queue<Contour>();
            cutContours.Enqueue(crosswalkContour);

            cutContours.Process(startBorder, Intersection.Side.Left);
            if (cutContours.Count == 0)
                return;

            cutContours.Process(endBorder, Intersection.Side.Right);
            if (cutContours.Count == 0)
                return;

            foreach (var contour in cutContours)
            {
                var trajectories = contour.Select(e => e.trajectory).ToArray();
                var datas = DecalData.GetData(DecalData.DecalType.Crosswalk, lod, trajectories, StyleHelper.SplitParams.Default, color, DecalData.TextureData.Default, new DecalData.EffectData(this as IEffectStyle));

                foreach (var data in datas)
                    addData(data);
            }
        }

        public enum CrosswalkType
        {
            [Description(nameof(Localize.CrosswalkStyle_Existent))]
            Existent = StyleType.CrosswalkExistent,

            [Description(nameof(Localize.CrosswalkStyle_Zebra))]
            Zebra = StyleType.CrosswalkZebra,

            [Description(nameof(Localize.CrosswalkStyle_DoubleZebra))]
            DoubleZebra = StyleType.CrosswalkDoubleZebra,

            [Description(nameof(Localize.CrosswalkStyle_ParallelSolidLines))]
            ParallelSolidLines = StyleType.CrosswalkParallelSolidLines,

            [Description(nameof(Localize.CrosswalkStyle_ParallelDashedLines))]
            ParallelDashedLines = StyleType.CrosswalkParallelDashedLines,

            [Description(nameof(Localize.CrosswalkStyle_Ladder))]
            Ladder = StyleType.CrosswalkLadder,

            [Description(nameof(Localize.CrosswalkStyle_Solid))]
            Solid = StyleType.CrosswalkSolid,

            [Description(nameof(Localize.CrosswalkStyle_ChessBoard))]
            ChessBoard = StyleType.CrosswalkChessBoard,

            [Description(nameof(Localize.CrosswalkStyle_Decal))]
            Decal = StyleType.CrosswalkDecal,

            [Description(nameof(Localize.Style_FromClipboard))]
            [NotVisible]
            Buffer = StyleType.CrosswalkBuffer,
        }
    }
}
