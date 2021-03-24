using NodeMarkup.Utilities;
using System.Collections.Generic;

namespace NodeMarkup.Manager
{
    public interface IOverlay
    {
        void Render(OverlayData data);
    }
    public interface IDeletable
    {
        string DeleteCaptionDescription { get; }
        string DeleteMessageDescription { get; }
        Dependences GetDependences();
    }
    public interface ISupport { }
    public interface ISupport<Type> where Type : ISupport { }
    public interface ISupportPoints : ISupport<MarkupEnterPoint> { }
    public interface ISupportEnters : ISupport<Enter> { }
    public interface ISupportLines : ISupport<MarkupLine> { }
    public interface ISupportFillers : ISupport<MarkupFiller> { }
    public interface ISupportCrosswalks : ISupport<MarkupCrosswalk> { }
    public interface ISupportStyleTemplate : ISupport<StyleTemplate> { }
    public interface ISupportIntersectionTemplate : ISupport<IntersectionTemplate> { }

    public interface IUpdate : ISupport
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
    }
}
