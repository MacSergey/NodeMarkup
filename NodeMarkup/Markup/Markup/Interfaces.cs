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
        Markup.SupportType Support { get; }
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
    public interface IUpdatePoints : IUpdate<MarkupPoint> { }
    public interface IUpdateLines : IUpdate<MarkupLine> { }
    public interface IUpdateFillers : IUpdate<MarkupFiller> { }
    public interface IUpdateCrosswalks : IUpdate<MarkupCrosswalk> { }

    public interface IItem : IUpdate, IDeletable, IOverlay { }
    public interface IStyleItem : IItem
    {
        void RecalculateStyleData();
    }

    public interface IStyleData
    {
        IEnumerable<IDrawData> GetDrawData();
        MarkupLOD LOD { get; }
        MarkupLODType LODType { get; }
    }
}
