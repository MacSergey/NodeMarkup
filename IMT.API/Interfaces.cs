//using ColossalFramework.Plugins;
//using System;
//using System.Collections.Generic;

//namespace NodeMarkup.API
//{
//    public static class Helper
//    {
//        public static IDataProviderV1 GetProvider(string id)
//        {
//            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
//            {
//                try
//                {
//                    foreach (Type type in assembly.GetExportedTypes())
//                    {
//                        if (type.IsClass && typeof(IDataProviderFactory).IsAssignableFrom(type))
//                        {
//                            var plugin = PluginManager.instance.FindPluginInfo(type.Assembly);
//                            if (plugin != null && plugin.isEnabled)
//                            {
//                                var factory = (IDataProviderFactory)Activator.CreateInstance(type);
//                                var provider = factory.GetProvider(id);
//                                return provider;
//                            }
//                        }
//                    }
//                }
//                catch { }
//            }

//            return null;
//        }
//    }
//    public interface IDataProviderFactory
//    {
//        IDataProviderV1 GetProvider(string id);
//    }
//    public interface IDataProviderV1
//    {
//        Version ModVersion { get; }
//        bool IsBeta { get; }

//        IEnumerable<string> RegularLineStyles { get; }
//        IEnumerable<string> NormalLineStyles { get; }
//        IEnumerable<string> StopLineStyles { get; }
//        IEnumerable<string> LaneLineStyles { get; }
//        IEnumerable<string> CrosswalkStyles { get; }
//        IEnumerable<string> FillerStyles { get; }

//        bool GetNodeMarking(ushort id, out INodeMarkingData nodeMarking);
//        bool GetSegmentMarking(ushort id, out ISegmentMarkingData segmentMarking);

//        bool NodeMarkingExist(ushort id);
//        bool SegmentMarkingExist(ushort id);

//        IRegularLineStyleData GetRegularLineStyle(string name);
//        INormalLineStyleData GetNormalLineStyle(string name);
//        IStopLineStyleData GetStopLineStyle(string name);
//        ILaneLineStyleData GetLaneLineStyle(string name);
//        ICrosswalkStyleData GetCrosswalkStyle(string name);
//        IFillerStyleData GetFillerStyle(string name);

//        IRegularLineStyleData GetRegularLineStyle(RegularLineStyleType style);
//        INormalLineStyleData GetNormalLineStyle(NormalLineStyleType style);
//        IStopLineStyleData GetStopLineStyle(StopLineStyleType style);
//        ILaneLineStyleData GetLaneLineStyle(LaneLineStyleType style);
//        ICrosswalkStyleData GetCrosswalkStyle(CrosswalkStyleType style);
//        IFillerStyleData GetFillerStyle(FillerStyleType style);
//    }

//    public interface IMarkingData
//    {
//        ushort Id { get; }
//        int EntranceCount { get; }

//        IRegularLineData AddRegularLine(IEntrancePointData startPoint, IEntrancePointData endPoint, IRegularLineStyleData style);
//        ILaneLineData AddLaneLine(ILanePointData startPoint, ILanePointData endPoint, ILaneLineStyleData style);
//        IFillerData AddFiller(IEnumerable<IEntrancePointData> pointDatas, IFillerStyleData style);

//        bool RemoveRegularLine(IEntrancePointData startPoint, IEntrancePointData endPoint);
//        bool RemoveLaneLine(ILanePointData startPoint, ILanePointData endPoint);
//        bool RemoveFiller(IFillerData filler);

//        bool RegularLineExist(IEntrancePointData startPoint, IEntrancePointData endPoint);
//        bool LaneLineExist(ILanePointData startPoint, ILanePointData endPoint);
//    }
//    public interface INodeMarkingData : IMarkingData
//    {
//        IEnumerable<ISegmentEntranceData> Entrances { get; }

//        bool GetEntrance(ushort id, out ISegmentEntranceData entrance);

//        IStopLineData AddStopLine(IEntrancePointData startPoint, IEntrancePointData endPoint, IStopLineStyleData style);
//        INormalLineData AddNormalLine(IEntrancePointData startPoint, INormalPointData endPoint, INormalLineStyleData style);
//        ICrosswalkData AddCrosswalk(ICrosswalkPointData startPoint, ICrosswalkPointData endPoint, ICrosswalkStyleData style);

//        bool RemoveStopLine(IEntrancePointData startPoint, IEntrancePointData endPoint);
//        bool RemoveNormalLine(IEntrancePointData startPoint, INormalPointData endPoint);
//        bool RemoveCrosswalk(ICrosswalkPointData startPoint, ICrosswalkPointData endPoint);

//        bool NormalLineExist(IEntrancePointData startPoint, INormalPointData endPoint);
//        bool StopLineExist(IEntrancePointData startPoint, IEntrancePointData endPoint);
//        bool CrosswalkExist(ICrosswalkPointData startPoint, ICrosswalkPointData endPoint);
//    }
//    public interface ISegmentMarkingData : IMarkingData
//    {
//        IEnumerable<INodeEntranceData> Entrances { get; }

//        bool GetEntrance(ushort id, out INodeEntranceData entrance);
//    }
//    public interface IEntranceData
//    {
//        ushort Id { get; }
//        int PointCount { get; }
//        IEnumerable<IPointData> Points { get; }
//    }
//    public interface ISegmentEntranceData : IEntranceData
//    {
//        IEnumerable<IEntrancePointData> EntrancePoints { get; }
//        IEnumerable<INormalPointData> NormalPoints { get; }
//        IEnumerable<ICrosswalkPointData> CrosswalkPoints { get; }
//        IEnumerable<ILanePointData> LanePoints { get; }

//        bool GetEntrancePoint(byte index, out IEntrancePointData point);
//        bool GetNormalPoint(byte index, out INormalPointData point);
//        bool GetCrosswalkPoint(byte index, out ICrosswalkPointData point);
//        bool GetLanePoint(byte index, out ILanePointData point);
//    }
//    public interface INodeEntranceData : IEntranceData
//    {
//        bool GetEntrancePoint(byte index, out IEntrancePointData point);
//        bool GetLanePoint(byte index, out ILanePointData point);

//        IEnumerable<IEntrancePointData> EntrancePoints { get; }
//        IEnumerable<ILanePointData> LanePoints { get; }
//    }

//    public interface IPointData
//    {
//        byte Index { get; }
//        ushort EntranceId { get; }
//        ushort MarkingId { get; }
//    }
//    public interface IEntrancePointData : IPointData
//    {
//        IPointSourceData Source { get; }
//        float Offset { get; set; }
//    }
//    public interface INormalPointData : IPointData
//    {
//        IEntrancePointData SourcePoint { get; }
//    }
//    public interface ICrosswalkPointData : IPointData
//    {
//        IEntrancePointData SourcePoint { get; }
//    }
//    public interface ILanePointData : IPointData
//    {
//        IEntrancePointData SourcePointA { get; }
//        IEntrancePointData SourcePointB { get; }
//    }
//    public interface IPointSourceData
//    {
//        uint LeftLaneId { get; }
//        int LeftIndex { get; }
//        uint RightLaneId { get; }
//        int RightIndex { get; }
//        PointLocation Location { get; }
//    }
//    public enum PointLocation
//    {
//        None,
//        Left,
//        Rigth,
//        Between,
//    }

//    public interface ILineData
//    {
//        public ulong Id { get; }
//    }
//    public interface IRegularLineData : ILineData
//    {
//        IEntrancePointData StartPoint { get; }
//        IEntrancePointData EndPoint { get; }
//    }
//    public interface IStopLineData : ILineData
//    {
//        IEntrancePointData StartPoint { get; }
//        IEntrancePointData EndPoint { get; }
//    }
//    public interface INormalLineData : ILineData
//    {
//        IEntrancePointData StartPoint { get; }
//        INormalPointData EndPoint { get; }
//    }
//    public interface ICrosswalkLineData : ILineData
//    {
//        ICrosswalkData Crosswalk { get; }
//        ICrosswalkPointData StartPoint { get; }
//        ICrosswalkPointData EndPoint { get; }
//    }
//    public interface ILaneLineData : ILineData
//    {
//        ILanePointData StartPoint { get; }
//        ILanePointData EndPoint { get; }
//    }

//    public interface IStyleData
//    {
//        string Name { get; }
//        IEnumerable<IStylePropertyData> Properties { get; }
//        object GetValue(string propertyName);
//        void SetValue(string propertyName, object value);
//    }
//    public interface IStylePropertyData
//    {
//        Type Type { get; }
//        string Name { get; }
//        object Value { get; set; }
//    }
//    public interface IRegularLineStyleData : IStyleData
//    {

//    }
//    public interface INormalLineStyleData : IStyleData
//    {

//    }
//    public interface IStopLineStyleData : IStyleData
//    {

//    }
//    public interface ILaneLineStyleData : IStyleData
//    {

//    }
//    public interface ICrosswalkStyleData : IStyleData
//    {

//    }
//    public interface IFillerStyleData : IStyleData
//    {

//    }

//    public interface IRuleData
//    {

//    }
//    public interface IFillerData
//    {
//        ushort MarkingId { get; }
//        int Id { get; }
//    }
//    public interface ICrosswalkData
//    {
//        public ushort MarkingId { get; }
//        public ICrosswalkLineData Line { get; }
//    }

//    public enum RegularLineStyleType
//    {
//        Solid,
//        Dashed,
//        DoubleSolid,
//        DoubleDashed,
//        SolidAndDashed,
//        SharkTeeth,
//        DoubleDashedAsym,
//        ZigZag,
//        Pavement,
//        Prop,
//        Tree,
//        Text,
//        Network,
//    }
//    public enum NormalLineStyleType
//    {
//        Solid,
//        Dashed,
//        DoubleSolid,
//        DoubleDashed,
//        SolidAndDashed,
//        SharkTeeth,
//        DoubleDashedAsym,
//        ZigZag,
//        Pavement,
//        Prop,
//        Tree,
//        Text,
//        Network,
//    }
//    public enum LaneLineStyleType
//    {
//        Prop,
//        Tree,
//        Text,
//        Network,
//    }
//    public enum StopLineStyleType
//    {
//        Solid,
//        Dashed,
//        DoubleSolid,
//        DoubleDashed,
//        SolidAndDashed,
//        SharkTeeth,
//        Pavement,
//    }
//    public enum CrosswalkStyleType
//    {
//        Existent,
//        Zebra,
//        DoubleZebra,
//        ParallelSolidLines,
//        ParallelDashedLines,
//        Ladder,
//        Solid,
//        ChessBoard,
//    }
//    public enum FillerStyleType
//    {
//        Stripe,
//        Grid,
//        Solid,
//        Chevron,
//        Pavement,
//        Grass,
//        Gravel,
//        Ruined,
//        Cliff,
//    }
//}