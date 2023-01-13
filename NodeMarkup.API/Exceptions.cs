using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup.API
{
    public class IntersectionMarkingToolException : Exception 
    {
        public IntersectionMarkingToolException(string message) : base(message) { }
    }
    public class CreateLineException : IntersectionMarkingToolException
    {
        public IPointData StartPoint;
        public IPointData EndPoint;
        public CreateLineException(IPointData startPoint, IPointData endPoint, string message) : base(message) 
        {
            StartPoint = startPoint;
            EndPoint = endPoint;
        }
    }

    public class MarkingIdNotMatchException : IntersectionMarkingToolException
    {
        public ushort ExpectedId { get; }
        public ushort Id { get; }

        public MarkingIdNotMatchException(ushort expectedId, ushort id) : base($"Marking Id #{id} does not match expected Id #{expectedId}")
        {
            ExpectedId = expectedId;
            Id = id;
        }
    }

    public class EnteranceNotExist : IntersectionMarkingToolException
    {
        public ushort Id { get; }
        public ushort MarkingId { get; }
        public EnteranceNotExist(ushort id, ushort markingId) : base($"Enterance #{id} does not exist in marking #{markingId}")
        {
            Id = id;
            MarkingId = markingId;
        }
    }
    public class PointNotExist : IntersectionMarkingToolException
    {
        public byte Id { get; }
        public ushort EnteranceId { get; }
        public PointNotExist(byte id, ushort enteranceId) : base($"Point #{id} does not exist in enter #{enteranceId}")
        {
            Id = id;
            EnteranceId = enteranceId;
        }
    }
}
