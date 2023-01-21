using ModsCommon.Utilities;
using NodeMarkup.Utilities;
using System.Collections.Generic;

namespace NodeMarkup.Manager
{
    public interface IDeletable
    {
        string DeleteCaptionDescription { get; }
        string DeleteMessageDescription { get; }
        Dependences GetDependences();
    }

    public interface ISupport
    {
        Marking.SupportType Support { get; }
    }
    public interface IUpdate
    {
        void Update(bool onlySelfUpdate = false);
    }
    public interface IUpdate<Type>
        where Type : IUpdate
    {
        void Update(Type item, bool recalculate = false, bool recalcDependences = false);
    }
    public interface IUpdatePoints : IUpdate<MarkingPoint> { }
    public interface IUpdateLines : IUpdate<MarkingLine> { }
    public interface IUpdateFillers : IUpdate<MarkingFiller> { }
    public interface IUpdateCrosswalks : IUpdate<MarkingCrosswalk> { }

    public interface IItem : IUpdate, IDeletable, IOverlay { }
    public interface IStyleItem : IItem
    {
        void RecalculateStyleData();
    }

    public interface IStyleData
    {
        IEnumerable<IDrawData> GetDrawData();
        MarkingLOD LOD { get; }
        MarkingLODType LODType { get; }
    }
}
