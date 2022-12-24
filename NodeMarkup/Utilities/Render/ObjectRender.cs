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
    public struct MarkupPropItemData
    {
        public Vector3 Position;
        public float Angle;
        public float Tilt;
        public float Slope;
        public float Scale;
        public Color32 Color;
    }
    public abstract class BaseMarkupPrefabData<PrefabType> : IStyleData, IDrawData
        where PrefabType : PrefabInfo
    {
        public MarkupLOD LOD => MarkupLOD.NoLOD;
        public abstract MarkupLODType LODType { get; }
        public PrefabType Info { get; private set; }
        protected MarkupPropItemData[] Items { get; private set; }

        public BaseMarkupPrefabData(PrefabType info, MarkupPropItemData[] items)
        {
            Info = info;
            Items = items;
        }

        public IEnumerable<IDrawData> GetDrawData()
        {
            yield return this;
        }

        public abstract void Draw(RenderManager.CameraInfo cameraInfo, RenderManager.Instance data, bool infoView);
    }
    public class MarkupPropData : BaseMarkupPrefabData<PropInfo>
    {
        public override MarkupLODType LODType => MarkupLODType.Prop;

        public MarkupPropData(PropInfo info, MarkupPropItemData[] items) : base(info, items) { }

        public override void Draw(RenderManager.CameraInfo cameraInfo, RenderManager.Instance data, bool infoView)
        {
            var instance = new InstanceID() { };

            foreach (var item in Items)
                RenderInstance(cameraInfo, Info, instance, item.Position, item.Scale, item.Angle, item.Tilt, item.Slope, item.Color, new Vector4(), true);

            //foreach (var item in Items)
            //    PropInstance.RenderInstance(cameraInfo, Info, instance, item.Position, item.Scale, item.Angle, item.Color, new Vector4(), true);
        }

        public static void RenderInstance(RenderManager.CameraInfo cameraInfo, PropInfo info, InstanceID id, Vector3 position, float scale, float angle, float tilt, float slope, Color color, Vector4 objectIndex, bool active)
        {
            if (!info.m_prefabInitialized)
                return;

            if (info.m_hasEffects && (active || info.m_alwaysActive))
            {
                Matrix4x4 matrix = default(Matrix4x4);
                matrix.SetTRS(position, Quaternion.AngleAxis(angle * 57.29578f, Vector3.down), new Vector3(scale, scale, scale));
                float simulationTimeDelta = Singleton<SimulationManager>.instance.m_simulationTimeDelta;
                for (int i = 0; i < info.m_effects.Length; i++)
                {
                    Vector3 position2 = matrix.MultiplyPoint(info.m_effects[i].m_position);
                    Vector3 direction = matrix.MultiplyVector(info.m_effects[i].m_direction);
                    EffectInfo.SpawnArea area = new EffectInfo.SpawnArea(position2, direction, 0f);
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
                    LightSystem lightSystem = Singleton<RenderManager>.instance.lightSystem;
                    Randomizer randomizer = new Randomizer(id.Index);
                    var num = info.m_illuminationOffRange.x + (float)randomizer.Int32(100000u) * 1E-05f * (info.m_illuminationOffRange.y - info.m_illuminationOffRange.x);
                    objectIndex.z = MathUtils.SmoothStep(num + 0.01f, num - 0.01f, lightSystem.DayLightIntensity);
                    if (info.m_illuminationBlinkType != 0)
                    {
                        Vector4 blinkVector = LightEffect.GetBlinkVector(info.m_illuminationBlinkType);
                        float num2 = num * 3.71f + Singleton<SimulationManager>.instance.m_simulationTimer / blinkVector.w;
                        num2 = (num2 - Mathf.Floor(num2)) * blinkVector.w;
                        float num3 = MathUtils.SmoothStep(blinkVector.x, blinkVector.y, num2);
                        float num4 = MathUtils.SmoothStep(blinkVector.w, blinkVector.z, num2);
                        objectIndex.z *= 1f - num3 * num4;
                    }
                }
                else
                    objectIndex.z = 1f;
            }

            if (cameraInfo == null || cameraInfo.CheckRenderDistance(position, Settings.PropLODDistance))
            {
                Matrix4x4 matrix = default(Matrix4x4);
                matrix.SetTRS(position, Quaternion.AngleAxis(angle * Mathf.Rad2Deg, Vector3.down) * Quaternion.AngleAxis(tilt * Mathf.Rad2Deg, Vector3.right) * Quaternion.AngleAxis(slope * Mathf.Rad2Deg, Vector3.forward), new Vector3(scale, scale, scale));
                PropManager instance = Singleton<PropManager>.instance;
                MaterialPropertyBlock materialBlock = instance.m_materialBlock;
                materialBlock.Clear();
                materialBlock.SetColor(instance.ID_Color, color);
                materialBlock.SetVector(instance.ID_ObjectIndex, objectIndex);
                if (info.m_rollLocation != null)
                {
                    info.m_material.SetVectorArray(instance.ID_RollLocation, info.m_rollLocation);
                    info.m_material.SetVectorArray(instance.ID_RollParams, info.m_rollParams);
                }
                instance.m_drawCallData.m_defaultCalls++;
                Graphics.DrawMesh(info.m_mesh, matrix, info.m_material, info.m_prefabDataLayer, null, 0, materialBlock);
            }
            else if (info.m_lodMaterialCombined == null)
            {
                Matrix4x4 matrix = default(Matrix4x4);
                matrix.SetTRS(position, Quaternion.AngleAxis(angle * Mathf.Rad2Deg, Vector3.down) * Quaternion.AngleAxis(tilt * Mathf.Rad2Deg, Vector3.right) * Quaternion.AngleAxis(slope * Mathf.Rad2Deg, Vector3.forward), new Vector3(scale, scale, scale));
                PropManager instance2 = Singleton<PropManager>.instance;
                MaterialPropertyBlock materialBlock2 = instance2.m_materialBlock;
                materialBlock2.Clear();
                materialBlock2.SetColor(instance2.ID_Color, color);
                materialBlock2.SetVector(instance2.ID_ObjectIndex, objectIndex);
                if (info.m_rollLocation != null)
                {
                    info.m_material.SetVectorArray(instance2.ID_RollLocation, info.m_rollLocation);
                    info.m_material.SetVectorArray(instance2.ID_RollParams, info.m_rollParams);
                }
                instance2.m_drawCallData.m_defaultCalls++;
                Graphics.DrawMesh(info.m_lodMesh, matrix, info.m_lodMaterial, info.m_prefabDataLayer, null, 0, materialBlock2);
            }
            else
            {
                objectIndex.w = scale;
                info.m_lodLocations[info.m_lodCount] = new Vector4(position.x, position.y, position.z, angle);
                info.m_lodObjectIndices[info.m_lodCount] = objectIndex;
                info.m_lodColors[info.m_lodCount] = color.linear;
                info.m_lodMin = Vector3.Min(info.m_lodMin, position);
                info.m_lodMax = Vector3.Max(info.m_lodMax, position);
                if (++info.m_lodCount == info.m_lodLocations.Length)
                    PropInstance.RenderLod(cameraInfo, info);
            }
        }
    }
    public class MarkupTreeData : BaseMarkupPrefabData<TreeInfo>
    {
        public override MarkupLODType LODType => MarkupLODType.Tree;

        public MarkupTreeData(TreeInfo info, MarkupPropItemData[] items) : base(info, items) { }

        public override void Draw(RenderManager.CameraInfo cameraInfo, RenderManager.Instance data, bool infoView)
        {
            foreach (var item in Items)
                RenderInstance(cameraInfo, Info, item.Position, item.Scale, item.Angle, item.Tilt, item.Slope, 1f, new Vector4());
        }

        public static void RenderInstance(RenderManager.CameraInfo cameraInfo, TreeInfo info, Vector3 position, float scale, float angle, float tilt, float slope, float brightness, Vector4 objectIndex)
        {
            if (!info.m_prefabInitialized)
                return;

            if (cameraInfo == null || info.m_lodMesh1 == null || cameraInfo.CheckRenderDistance(position, Settings.TreeLODDistance))
            {
                TreeManager instance = Singleton<TreeManager>.instance;
                MaterialPropertyBlock materialBlock = instance.m_materialBlock;
                Matrix4x4 matrix = default(Matrix4x4);
                matrix.SetTRS(position, Quaternion.AngleAxis(angle * Mathf.Rad2Deg, Vector3.down) * Quaternion.AngleAxis(tilt * Mathf.Rad2Deg, Vector3.right) * Quaternion.AngleAxis(slope * Mathf.Rad2Deg, Vector3.forward), new Vector3(scale, scale, scale));
                Color value = info.m_defaultColor * brightness;
                value.a = Singleton<WeatherManager>.instance.GetWindSpeed(position);
                materialBlock.Clear();
                materialBlock.SetColor(instance.ID_Color, value);
                materialBlock.SetVector(instance.ID_ObjectIndex, objectIndex);
                instance.m_drawCallData.m_defaultCalls++;
                Graphics.DrawMesh(info.m_mesh, matrix, info.m_material, info.m_prefabDataLayer, null, 0, materialBlock);
            }
            else
            {
                position.y += info.m_generatedInfo.m_center.y * (scale - 1f);
                Color color = info.m_defaultColor * brightness;
                color.a = Singleton<WeatherManager>.instance.GetWindSpeed(position);
                info.m_lodLocations[info.m_lodCount] = new Vector4(position.x, position.y, position.z, scale);
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
    }
}
