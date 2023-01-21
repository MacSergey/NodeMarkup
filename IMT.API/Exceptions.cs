using System;

namespace IMT.API
{
    public class IntersectionMarkingToolException : Exception
    {
        public IntersectionMarkingToolException(string message) : base(message) { }
    }
    public class MarkingNotExistException : Exception
    {
        public ushort Id { get; }
        public MarkingNotExistException() : base($"Marking does not exist") { }
        public MarkingNotExistException(ushort id) : base($"Marking Id #{id} does not exist")
        {
            Id = id;
        }
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
    public class CreateFillerException : IntersectionMarkingToolException
    {
        public CreateFillerException(string message) : base(message)
        {

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

    public class EntranceNotExistException : IntersectionMarkingToolException
    {
        public ushort Id { get; }
        public ushort MarkingId { get; }
        public EntranceNotExistException(ushort id, ushort markingId) : base($"Entrance #{id} does not exist in marking #{markingId}")
        {
            Id = id;
            MarkingId = markingId;
        }
    }
    public class LineNotExistException : IntersectionMarkingToolException
    {
        public ulong Id { get; }
        public ushort MarkingId { get; }
        public LineNotExistException(ulong id, ushort markingId) : base($"Line #{id} does not exist in marking #{markingId}")
        {
            Id = id;
            MarkingId = markingId;
        }
    }
    public class FillerNotExistException : IntersectionMarkingToolException
    {
        public int Id { get; }
        public ushort MarkingId { get; }
        public FillerNotExistException(int id, ushort markingId) : base($"Filler #{id} does not exist in marking #{markingId}")
        {
            Id = id;
            MarkingId = markingId;
        }
    }
    public class PointNotExistException : IntersectionMarkingToolException
    {
        public byte Id { get; }
        public ushort EntranceId { get; }
        public PointNotExistException(byte id, ushort entranceId) : base($"Point #{id} does not exist in enter #{entranceId}")
        {
            Id = id;
            EntranceId = entranceId;
        }
    }
}
