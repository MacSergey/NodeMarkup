using System;

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

	public class EntranceNotExist : IntersectionMarkingToolException
	{
		public ushort Id { get; }
		public ushort MarkingId { get; }
		public EntranceNotExist(ushort id, ushort markingId) : base($"Entrance #{id} does not exist in marking #{markingId}")
		{
			Id = id;
			MarkingId = markingId;
		}
	}
	public class PointNotExist : IntersectionMarkingToolException
	{
		public byte Id { get; }
		public ushort EntranceId { get; }
		public PointNotExist(byte id, ushort entranceId) : base($"Point #{id} does not exist in enter #{entranceId}")
		{
			Id = id;
			EntranceId = entranceId;
		}
	}
}
