namespace NodeMarkup.Utils
{
    public struct ObjectId
    {
        private int Id;

        public ushort Segment
        {
            get => (Id & (int)ObjectType.Segment) == 0 ? (ushort)0 : (ushort)(Id & (int)ObjectType.Data);
            set => Id = (int)ObjectType.Segment | value;
        }
        public int Point
        {
            get => (Id & (int)ObjectType.Point) == 0 ? 0 : (Id & (int)ObjectType.Data);
            set => Id = (int)ObjectType.Point | value;
        }
        public ObjectType Type => (ObjectType)(Id & (int)ObjectType.Type);

        public static bool operator ==(ObjectId x, ObjectId y) => x.Id == y.Id;
        public static bool operator !=(ObjectId x, ObjectId y) => x.Id != y.Id;

        public override bool Equals(object obj) => obj is ObjectId objectId && objectId == this;
        public override int GetHashCode() => Id.GetHashCode();
        public override string ToString() => $"{Type}: {Id}";
    }
    public enum ObjectType : int
    {
        Data = 0xFFFFFF,
        Type = 0xFF <<24,
        Segment = 1 << 24,
        Point = 2 << 24,

    }
}
