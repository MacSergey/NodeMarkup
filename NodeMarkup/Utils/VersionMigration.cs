using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup.Utils
{
    public static class VersionMigration
    {
        public static ObjectsMap Befor1_2(Markup markup, ObjectsMap map)
        {
            if (map == null)
                map = new ObjectsMap();

            foreach(var enter in markup.Enters)
            {
                foreach(var point in enter.Points.Skip(1).Take(enter.PointCount - 2))
                {
                    switch(point.Location)
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
    }
}
