using ColossalFramework.Math;
using IMT.Utilities;
using ModsCommon;
using ModsCommon.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace IMT.Manager
{
    public class MarkingLaneLine : MarkingRegularLine
    {
        public override LineType Type => LineType.Lane;

        public MarkingLaneLine(Marking marking, MarkingPointPair pointPair, RegularLineStyle style = null) : base(marking, pointPair, style) { }

        public override void Render(OverlayData data)
        {
            var lanePointS = PointPair.First as MarkingLanePoint;
            var lanePointE = PointPair.Second as MarkingLanePoint;

            ITrajectory[] trajectories;
            if (lanePointS != null && lanePointE != null)
            {
                lanePointS.Source.GetPoints(out var leftPointS, out var rightPointS);
                lanePointE.Source.GetPoints(out var leftPointE, out var rightPointE);
                trajectories = new ITrajectory[]
                {
                    new BezierTrajectory(leftPointS.Position, leftPointS.Direction, rightPointE.Position, rightPointE.Direction),
                    new StraightTrajectory(rightPointE.Position, leftPointE.Position),
                    new BezierTrajectory(leftPointE.Position, leftPointE.Direction, rightPointS.Position, rightPointS.Direction),
                    new StraightTrajectory(rightPointS.Position, leftPointS.Position),
                };
            }
            else if (lanePointS != null)
            {
                lanePointS.Source.GetPoints(out var leftPointS, out var rightPointS);
                trajectories = new ITrajectory[]
                {
                    new BezierTrajectory(leftPointS.Position, leftPointS.Direction, PointPair.Second.Position, PointPair.Second.Direction),
                    new BezierTrajectory(PointPair.Second.Position, PointPair.Second.Direction, rightPointS.Position, rightPointS.Direction),
                    new StraightTrajectory(rightPointS.Position, leftPointS.Position),
                };
            }
            else if (lanePointE != null)
            {
                lanePointE.Source.GetPoints(out var leftPointE, out var rightPointE);
                trajectories = new ITrajectory[]
                {
                    new BezierTrajectory(PointPair.First.Position, PointPair.First.Direction, rightPointE.Position, rightPointE.Direction),
                    new StraightTrajectory(rightPointE.Position, leftPointE.Position),
                    new BezierTrajectory(leftPointE.Position, leftPointE.Direction, PointPair.First.Position, PointPair.First.Direction),
                };
            }
            else
                return;

            data.AlphaBlend = false;
            var triangles = Triangulator.TriangulateSimple(trajectories, out var points, minAngle: 5, maxLength: 10f);
            points.RenderArea(triangles, data);
        }
        public override void RenderRule(MarkingLineRawRule rule, OverlayData data)
        {
            if (!rule.GetT(out var fromT, out var toT) || fromT == toT)
                return;

            var lanePointS = PointPair.First as MarkingLanePoint;
            var lanePointE = PointPair.Second as MarkingLanePoint;

            ITrajectory[] trajectories;
            if (lanePointS != null && lanePointE != null)
            {
                lanePointS.Source.GetPoints(out var leftPointS, out var rightPointS);
                lanePointE.Source.GetPoints(out var leftPointE, out var rightPointE);

                trajectories = new ITrajectory[4];
                trajectories[0] = new BezierTrajectory(leftPointS.Position, leftPointS.Direction, rightPointE.Position, rightPointE.Direction).Cut(fromT, toT);
                trajectories[2] = new BezierTrajectory(leftPointE.Position, leftPointE.Direction, rightPointS.Position, rightPointS.Direction).Cut(1f - toT, 1f - fromT);
                trajectories[1] = new StraightTrajectory(trajectories[0].EndPosition, trajectories[2].StartPosition);
                trajectories[3] = new StraightTrajectory(trajectories[2].EndPosition, trajectories[0].StartPosition);
            }
            //else if (lanePointA != null)
            //{
            //    lanePointA.Source.GetPoints(out var leftPointA, out var rightPointA);
            //    trajectories = new List<ITrajectory>()
            //    {
            //        new BezierTrajectory(leftPointA.Position, leftPointA.Direction, PointPair.Second.Position, PointPair.Second.Direction),
            //        new BezierTrajectory(PointPair.Second.Position, PointPair.Second.Direction, rightPointA.Position, rightPointA.Direction),
            //        new StraightTrajectory(rightPointA.Position, leftPointA.Position),
            //    };
            //}
            //else if (lanePointB != null)
            //{
            //    lanePointB.Source.GetPoints(out var leftPointB, out var rightPointB);
            //    trajectories = new List<ITrajectory>()
            //    {
            //        new BezierTrajectory(PointPair.First.Position, PointPair.First.Direction, rightPointB.Position, rightPointB.Direction),
            //        new StraightTrajectory(rightPointB.Position, leftPointB.Position),
            //        new BezierTrajectory(leftPointB.Position, leftPointB.Direction, PointPair.First.Position, PointPair.First.Direction),
            //    };
            //}
            else
                return;

            data.AlphaBlend = false;
            var triangles = Triangulator.TriangulateSimple(trajectories, out var points, minAngle: 5, maxLength: 10f);
            points.RenderArea(triangles, data);
        }
    }
}
