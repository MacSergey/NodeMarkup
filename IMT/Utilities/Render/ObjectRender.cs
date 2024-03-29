﻿using ColossalFramework;
using ColossalFramework.Math;
using IMT.Manager;
using UnityEngine;

namespace IMT.Utilities
{
    public struct MarkingObjectItemData
    {
        public Vector3 position;
        public float absoluteAngle;
        public float angle;
        public float tilt;
        public float slope;
        public float scale;
        public Color32 color;
        public bool wind;
    }
    public abstract class BaseMarkingPrefabData<PrefabType> : IStyleData
        where PrefabType : PrefabInfo
    {
        public MarkingLOD LOD => MarkingLOD.NoLOD;
        public abstract MarkingLODType LODType { get; }
        public abstract int RenderLayer { get; }
        public PrefabType Info { get; private set; }
        protected MarkingObjectItemData[] Items { get; private set; }

        public BaseMarkingPrefabData(PrefabType info, MarkingObjectItemData[] items)
        {
            Info = info;
            Items = items;
        }

        public abstract void Render(RenderManager.CameraInfo cameraInfo, RenderManager.Instance data, bool infoView);
        public abstract bool CalculateGroupData(int layer, ref int vertexCount, ref int triangleCount, ref int objectCount, ref RenderGroup.VertexArrays vertexArrays);
        public abstract void PopulateGroupData(int layer, ref int vertexIndex, ref int triangleIndex, Vector3 groupPosition, RenderGroup.MeshData data, ref Vector3 min, ref Vector3 max, ref float maxRenderDistance, ref float maxInstanceDistance, ref bool requireSurfaceMaps);
    }
    public class MarkingPropData : BaseMarkingPrefabData<PropInfo>
    {
        public override MarkingLODType LODType => MarkingLODType.Prop;
        public override int RenderLayer => Info.m_prefabDataLayer;
        public MarkingPropData(PropInfo info, MarkingObjectItemData[] items) : base(info, items) { }

        public override void Render(RenderManager.CameraInfo cameraInfo, RenderManager.Instance data, bool infoView)
        {
            var instance = new InstanceID();

            for(var i = 0; i < Items.Length; i += 1)
            {
                RenderInstance(cameraInfo, Info, instance, ref Items[i], new Vector4(), true);
            }
        }

        public static void RenderInstance(RenderManager.CameraInfo cameraInfo, PropInfo info, InstanceID id, ref MarkingObjectItemData data, Vector4 objectIndex, bool active)
        {
            if (!info.m_prefabInitialized)
                return;

            if (info.m_hasEffects && (active || info.m_alwaysActive))
            {
                var matrix = default(Matrix4x4);
                matrix.SetTRS(data.position, Quaternion.AngleAxis((data.absoluteAngle + data.angle) * Mathf.Rad2Deg, Vector3.down), new Vector3(data.scale, data.scale, data.scale));
                var simulationTimeDelta = Singleton<SimulationManager>.instance.m_simulationTimeDelta;
                for (int i = 0; i < info.m_effects.Length; i++)
                {
                    var effectPos = matrix.MultiplyPoint(info.m_effects[i].m_position);
                    var effectDir = matrix.MultiplyVector(info.m_effects[i].m_direction);
                    EffectInfo.SpawnArea area = new EffectInfo.SpawnArea(effectPos, effectDir, 0f);
                    info.m_effects[i].m_effect.RenderEffect(id, area, Vector3.zero, 0f, 1f, -1f, simulationTimeDelta, cameraInfo);
                }
            }
            if (!info.m_hasRenderer || (cameraInfo.m_layerMask & (1 << info.m_prefabDataLayer)) == 0)
                return;

            if (Singleton<InfoManager>.instance.CurrentMode == InfoManager.InfoMode.None)
            {
                if (!active && !info.m_alwaysActive)
                    objectIndex.z = 0f;
                else if (info.m_illuminationOffRange.x < 1000f || info.m_illuminationBlinkType != 0)
                {
                    var lightSystem = Singleton<RenderManager>.instance.lightSystem;
                    var randomizer = new Randomizer(id.Index);
                    var illumination = info.m_illuminationOffRange.x + randomizer.Int32(100000u) * 1E-05f * (info.m_illuminationOffRange.y - info.m_illuminationOffRange.x);
                    objectIndex.z = MathUtils.SmoothStep(illumination + 0.01f, illumination - 0.01f, lightSystem.DayLightIntensity);
                    if (info.m_illuminationBlinkType != 0)
                    {
                        var blinkVector = LightEffect.GetBlinkVector(info.m_illuminationBlinkType);
                        illumination = illumination * 3.71f + Singleton<SimulationManager>.instance.m_simulationTimer / blinkVector.w;
                        illumination = (illumination - Mathf.Floor(illumination)) * blinkVector.w;
                        var illuminationX = MathUtils.SmoothStep(blinkVector.x, blinkVector.y, illumination);
                        var illuminationY = MathUtils.SmoothStep(blinkVector.w, blinkVector.z, illumination);
                        objectIndex.z *= 1f - illuminationX * illuminationY;
                    }
                }
                else
                    objectIndex.z = 1f;
            }

            if (cameraInfo == null || cameraInfo.CheckRenderDistance(data.position, Settings.PropLODDistance))
            {
                var matrix = default(Matrix4x4);
                var rotation = Quaternion.AngleAxis(data.absoluteAngle * Mathf.Rad2Deg, Vector3.down);
                rotation *= Quaternion.AngleAxis(data.slope * Mathf.Rad2Deg, Vector3.forward);
                rotation *= Quaternion.AngleAxis(data.tilt * Mathf.Rad2Deg, Vector3.right);
                rotation *= Quaternion.AngleAxis(data.angle * Mathf.Rad2Deg, Vector3.down);
                matrix.SetTRS(data.position, rotation, new Vector3(data.scale, data.scale, data.scale));
                var instance = Singleton<PropManager>.instance;
                var materialBlock = instance.m_materialBlock;
                materialBlock.Clear();
                materialBlock.SetColor(instance.ID_Color, data.color);
                materialBlock.SetVector(instance.ID_ObjectIndex, objectIndex);
                if (info.m_rollLocation != null)
                {
                    info.m_material.SetVectorArray(instance.ID_RollLocation, info.m_rollLocation);
                    info.m_material.SetVectorArray(instance.ID_RollParams, info.m_rollParams);
                }
                instance.m_drawCallData.m_defaultCalls++;
                Graphics.DrawMesh(info.m_mesh, matrix, info.m_material, RenderHelper.RoadLayer, null, 0, materialBlock);
            }
            else if (info.m_lodMaterialCombined == null)
            {
                var matrix = default(Matrix4x4);
                var rotation = Quaternion.AngleAxis(data.absoluteAngle * Mathf.Rad2Deg, Vector3.down);
                rotation *= Quaternion.AngleAxis(data.slope * Mathf.Rad2Deg, Vector3.forward);
                rotation *= Quaternion.AngleAxis(data.tilt * Mathf.Rad2Deg, Vector3.right);
                rotation *= Quaternion.AngleAxis(data.angle * Mathf.Rad2Deg, Vector3.down);
                matrix.SetTRS(data.position, rotation, new Vector3(data.scale, data.scale, data.scale));
                var instance = Singleton<PropManager>.instance;
                var materialBlock = instance.m_materialBlock;
                materialBlock.Clear();
                materialBlock.SetColor(instance.ID_Color, data.color);
                materialBlock.SetVector(instance.ID_ObjectIndex, objectIndex);
                if (info.m_rollLocation != null)
                {
                    info.m_material.SetVectorArray(instance.ID_RollLocation, info.m_rollLocation);
                    info.m_material.SetVectorArray(instance.ID_RollParams, info.m_rollParams);
                }
                instance.m_drawCallData.m_defaultCalls++;
                Graphics.DrawMesh(info.m_lodMesh, matrix, info.m_lodMaterial, RenderHelper.RoadLayer, null, 0, materialBlock);
            }
            else
            {
                objectIndex.w = data.scale;
                info.m_lodLocations[info.m_lodCount] = new Vector4(data.position.x, data.position.y, data.position.z, data.absoluteAngle + data.angle);
                info.m_lodObjectIndices[info.m_lodCount] = objectIndex;
                info.m_lodColors[info.m_lodCount] = ((Color)data.color).linear;
                info.m_lodMin = Vector3.Min(info.m_lodMin, data.position);
                info.m_lodMax = Vector3.Max(info.m_lodMax, data.position);
                if (++info.m_lodCount == info.m_lodLocations.Length)
                    PropInstance.RenderLod(cameraInfo, info);
            }
        }

        public override bool CalculateGroupData(int layer, ref int vertexCount, ref int triangleCount, ref int objectCount, ref RenderGroup.VertexArrays vertexArrays)
        {
            foreach (var item in Items)
            {
                PropInstance.CalculateGroupData(Info, layer, ref vertexCount, ref triangleCount, ref objectCount, ref vertexArrays);
            }

            return true;
        }

        public override void PopulateGroupData(int layer, ref int vertexIndex, ref int triangleIndex, Vector3 groupPosition, RenderGroup.MeshData data, ref Vector3 min, ref Vector3 max, ref float maxRenderDistance, ref float maxInstanceDistance, ref bool requireSurfaceMaps)
        {
            var instance = new InstanceID();

            foreach (var item in Items)
            {
                PropInstance.PopulateGroupData(Info, layer, instance, item.position, item.scale, item.absoluteAngle + item.angle, item.color, ref vertexIndex, ref triangleIndex, groupPosition, data, ref min, ref max, ref maxRenderDistance, ref maxInstanceDistance);
            }
        }
    }
    public class MarkingTreeData : BaseMarkingPrefabData<TreeInfo>
    {
        public override MarkingLODType LODType => MarkingLODType.Tree;
        public override int RenderLayer => Info.m_prefabDataLayer;

        public MarkingTreeData(TreeInfo info, MarkingObjectItemData[] items) : base(info, items) { }

        public override void Render(RenderManager.CameraInfo cameraInfo, RenderManager.Instance data, bool infoView)
        {
            for (var i = 0; i < Items.Length; i += 1)
            {
                RenderInstance(cameraInfo, Info, ref Items[i], 1f, new Vector4());
            }
        }

        public static void RenderInstance(RenderManager.CameraInfo cameraInfo, TreeInfo info, ref MarkingObjectItemData data, float brightness, Vector4 objectIndex)
        {
            if (!info.m_prefabInitialized)
                return;

            if (cameraInfo == null || info.m_lodMesh1 == null || cameraInfo.CheckRenderDistance(data.position, Settings.TreeLODDistance))
            {
                var instance = Singleton<TreeManager>.instance;
                var materialBlock = instance.m_materialBlock;
                var matrix = default(Matrix4x4);
                var rotation = Quaternion.AngleAxis(data.absoluteAngle * Mathf.Rad2Deg, Vector3.down);
                rotation *= Quaternion.AngleAxis(data.slope * Mathf.Rad2Deg, Vector3.forward);
                rotation *= Quaternion.AngleAxis(data.tilt * Mathf.Rad2Deg, Vector3.right);
                rotation *= Quaternion.AngleAxis(data.angle * Mathf.Rad2Deg, Vector3.down);
                matrix.SetTRS(data.position, rotation, new Vector3(data.scale, data.scale, data.scale));
                var color = info.m_defaultColor * brightness;
                color.a = data.wind ? Singleton<WeatherManager>.instance.GetWindSpeed(data.position) : 0f;
                materialBlock.Clear();
                materialBlock.SetColor(instance.ID_Color, color);
                materialBlock.SetVector(instance.ID_ObjectIndex, objectIndex);
                instance.m_drawCallData.m_defaultCalls++;
                Graphics.DrawMesh(info.m_mesh, matrix, info.m_material, RenderHelper.RoadLayer, null, 0, materialBlock);
            }
            else
            {
                var position = data.position;
                position.y += info.m_generatedInfo.m_center.y * (data.scale - 1f);
                var color = info.m_defaultColor * brightness;
                color.a = data.wind ? Singleton<WeatherManager>.instance.GetWindSpeed(position) : 0f;
                info.m_lodLocations[info.m_lodCount] = new Vector4(position.x, position.y, position.z, data.scale);
                info.m_lodColors[info.m_lodCount] = color.linear;
                info.m_lodObjectIndices[info.m_lodCount] = objectIndex;
                info.m_lodMin = Vector3.Min(info.m_lodMin, position);
                info.m_lodMax = Vector3.Max(info.m_lodMax, position);
                if (++info.m_lodCount == info.m_lodLocations.Length)
                {
                    TreeInstance.RenderLod(cameraInfo, info);
                }
            }
        }

        public override bool CalculateGroupData(int layer, ref int vertexCount, ref int triangleCount, ref int objectCount, ref RenderGroup.VertexArrays vertexArrays)
        {
            foreach (var item in Items)
            {
                TreeInstance.CalculateGroupData(ref vertexCount, ref triangleCount, ref objectCount, ref vertexArrays);
            }

            return true;
        }

        public override void PopulateGroupData(int layer, ref int vertexIndex, ref int triangleIndex, Vector3 groupPosition, RenderGroup.MeshData data, ref Vector3 min, ref Vector3 max, ref float maxRenderDistance, ref float maxInstanceDistance, ref bool requireSurfaceMaps)
        {
            foreach (var item in Items)
            {
                TreeInstance.PopulateGroupData(Info, item.position, item.scale, 1f, RenderManager.DefaultColorLocation, ref vertexIndex, ref triangleIndex, groupPosition, data, ref min, ref max, ref maxRenderDistance, ref maxInstanceDistance);
            }
        }
    }
}
