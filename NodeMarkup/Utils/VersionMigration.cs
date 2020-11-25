using IMT.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace IMT.Utils
{
    public static class VersionMigration
    {
        public static ObjectsMap Befor1_2(NodeMarkup markup, ObjectsMap map)
        {
            if (map == null)
                map = new ObjectsMap();

            foreach (var enter in markup.Enters)
            {
                foreach (var point in enter.Points.Skip(1).Take(enter.PointCount - 2))
                {
                    switch (point.Location)
                    {
                        case MarkupPoint.LocationType.LeftEdge:
                            map.AddPoint(point.Id, point.Id - (1 << 16));
                            break;
                        case MarkupPoint.LocationType.RightEdge:
                            map.AddPoint(point.Id, point.Id + (1 << 16));
                            break;
                    }
                }
            }

            return map;
        }
        private static Dictionary<byte, byte> Correction01Dic { get; } = new Dictionary<byte, byte>();
        public static Color32 CorrectColor01(Color32 color)
        {
            if (!Correction01Dic.TryGetValue(color.a, out byte newAlpha))
            {
                newAlpha = (byte)(Mathf.Pow(Mathf.Max(Mathf.Pow((float)color.a / byte.MaxValue, 2f) - 0.015f, 0f) / 0.985f, 0.25f) * byte.MaxValue);
                Correction01Dic[color.a] = newAlpha;
            }

            color.a = newAlpha;
            return color;
        }
    }
}
