using ColossalFramework;
using ColossalFramework.Math;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace NodeMarkup.Utilities
{
    public class MarkupNetworkData : BaseMarkupMeshData
    {
        private struct Data
        {
            public NetInfo.Segment segment;
            public Vector3 position;
            public Matrix4x4 left;
            public Matrix4x4 right;
            public Color color;
            public Texture surfaceTexA;
            public Texture surfaceTexB;
            public Texture heightMap;
            public Vector4 heightMapping;
            public Vector4 surfaceMapping;
        }

        public override MarkupLODType LODType => MarkupLODType.Network;
        Data[] Datas { get; set; }

        public MarkupNetworkData(NetInfo info, ITrajectory[] trajectories, float width, float length, float scale, float elevation) : base(MarkupLOD.NoLOD, width, length)
        {
            var count = info.m_segments.Count(s => s.CheckFlags(NetSegment.Flags.None, out _));
            Datas = new Data[trajectories.Length * count];

            for (int i = 0; i < trajectories.Length; i += 1)
            {
                var position = (trajectories[i].StartPosition + trajectories[i].EndPosition) / 2;
                CalculateMatrix(trajectories[i], width * 0.5f * scale, position, out var left, out var right);
                position += Vector3.up * elevation;

                int j = 0;
                foreach (var segment in info.m_segments)
                {
                    if (segment.CheckFlags(NetSegment.Flags.None, out _))
                    {
                        int index = i * count + j;

                        Datas[index].segment = segment;
                        Datas[index].position = position;
                        Datas[index].left = left;
                        Datas[index].right = right;
                        Datas[index].color = info.m_color;
                        Datas[index].color.a = 0f;

                        if (segment.m_requireSurfaceMaps)
                            Singleton<TerrainManager>.instance.GetSurfaceMapping(Datas[index].position, out Datas[index].surfaceTexA, out Datas[i].surfaceTexB, out Datas[index].surfaceMapping);
                        else if (segment.m_requireHeightMap)
                            Singleton<TerrainManager>.instance.GetHeightMapping(Datas[index].position, out Datas[index].heightMap, out Datas[i].heightMapping, out Datas[index].surfaceMapping);

                        j += 1;
                    }
                }
            }
        }

        public override IEnumerable<IDrawData> GetDrawData()
        {
            yield return this;
        }

        public override void Draw(RenderManager.CameraInfo cameraInfo, RenderManager.Instance renderData, bool infoView)
        {
            var instance = Singleton<NetManager>.instance;

            foreach (var data in Datas)
            {
                if (cameraInfo.CheckRenderDistance(renderData.m_position, Settings.NetworkLODDistance))
                {
                    instance.m_materialBlock.Clear();
                    instance.m_materialBlock.SetMatrix(instance.ID_LeftMatrix, data.left);
                    instance.m_materialBlock.SetMatrix(instance.ID_RightMatrix, data.right);
                    instance.m_materialBlock.SetVector(instance.ID_MeshScale, Scale);
                    instance.m_materialBlock.SetColor(instance.ID_Color, data.color);
                    if (data.segment.m_requireSurfaceMaps)
                    {
                        if (data.surfaceTexA != null)
                            instance.m_materialBlock.SetTexture(instance.ID_SurfaceTexA, data.surfaceTexA);
                        if (data.surfaceTexB != null)
                            instance.m_materialBlock.SetTexture(instance.ID_SurfaceTexB, data.surfaceTexB);
                        instance.m_materialBlock.SetVector(instance.ID_SurfaceMapping, data.surfaceMapping);
                    }
                    else if (data.segment.m_requireHeightMap)
                    {
                        instance.m_materialBlock.SetTexture(instance.ID_HeightMap, data.heightMap);
                        instance.m_materialBlock.SetVector(instance.ID_HeightMapping, data.heightMapping);
                        instance.m_materialBlock.SetVector(instance.ID_SurfaceMapping, data.surfaceMapping);
                    }

                    instance.m_drawCallData.m_defaultCalls++;
                    Graphics.DrawMesh(data.segment.m_segmentMesh, data.position, Quaternion.identity, data.segment.m_segmentMaterial, RenderHelper.RoadLayer, null, 0, instance.m_materialBlock);
                }
                else if (data.segment.m_combinedLod is NetInfo.LodValue combinedLod)
                {
                    if (data.segment.m_requireHeightMap && data.heightMap != combinedLod.m_heightMap)
                    {
                        if (combinedLod.m_lodCount != 0)
                            NetSegment.RenderLod(cameraInfo, combinedLod);

                        combinedLod.m_heightMap = data.heightMap;
                        combinedLod.m_heightMapping = data.heightMapping;
                        combinedLod.m_surfaceMapping = data.surfaceMapping;
                    }

                    combinedLod.m_leftMatrices[combinedLod.m_lodCount] = data.left;
                    combinedLod.m_rightMatrices[combinedLod.m_lodCount] = data.right;
                    combinedLod.m_meshScales[combinedLod.m_lodCount] = Scale;
                    combinedLod.m_meshLocations[combinedLod.m_lodCount] = data.position;
                    combinedLod.m_lodMin = Vector3.Min(combinedLod.m_lodMin, data.position);
                    combinedLod.m_lodMax = Vector3.Max(combinedLod.m_lodMax, data.position);

                    if (++combinedLod.m_lodCount == combinedLod.m_leftMatrices.Length)
                        NetSegment.RenderLod(cameraInfo, combinedLod);
                }
            }
        }
    }
}
