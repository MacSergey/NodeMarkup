using IMT.Utilities;
using ModsCommon.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace IMT.Manager
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
        MarkingLOD LOD { get; }
        MarkingLODType LODType { get; }
        public int RenderLayer { get; }
        void Render(RenderManager.CameraInfo cameraInfo, RenderManager.Instance data, bool infoView);
        bool CalculateGroupData(int layer, ref int vertexCount, ref int triangleCount, ref int objectCount, ref RenderGroup.VertexArrays vertexArrays);
        void PopulateGroupData(int layer, ref int vertexIndex, ref int triangleIndex, Vector3 groupPosition, RenderGroup.MeshData data, ref Vector3 min, ref Vector3 max, ref float maxRenderDistance, ref float maxInstanceDistance, ref bool requireSurfaceMaps);
    }
}
