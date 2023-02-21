using ColossalFramework;
using ColossalFramework.Math;
using IMT.Manager;
using System.Collections.Generic;
using UnityEngine;

namespace IMT.Utilities
{
    public struct MarkingPropItemData
    {
        public Vector3 position;
        public float absoluteAngle;
        public float angle;
        public float tilt;
        public float slope;
        public float scale;
        public Color32 color;
    }
    public abstract class BaseMarkingPrefabData<PrefabType> : IStyleData
        where PrefabType : PrefabInfo
    {
        public MarkingLOD LOD => MarkingLOD.NoLOD;
        public abstract MarkingLODType LODType { get; }
        public PrefabType Info { get; private set; }
        protected MarkingPropItemData[] Items { get; private set; }

        public BaseMarkingPrefabData(PrefabType info, MarkingPropItemData[] items)
        {
            Info = info;
            Items = items;
        }

        public abstract void Draw(RenderManager.CameraInfo cameraInfo, RenderManager.Instance data, bool infoView);
    }
    public class MarkingPropData : BaseMarkingPrefabData<PropInfo>
    {
        public override MarkingLODType LODType => MarkingLODType.Prop;

        public MarkingPropData(PropInfo info, MarkingPropItemData[] items) : base(info, items) { }

        public override void Draw(RenderManager.CameraInfo cameraInfo, RenderManager.Instance data, bool infoView)
        {
            var instance = new InstanceID() { };

            foreach (var item in Items)
                RenderInstance(cameraInfo, Info, instance, item.position, item.scale, item.absoluteAngle, item.angle, item.tilt, item.slope, item.color, new Vector4(), true);
        }

        public static void RenderInstance(RenderManager.CameraInfo cameraInfo, PropInfo info, InstanceID id, Vector3 position, float scale, float absoluteAngle, float angle, float tilt, float slope, Color color, Vector4 objectIndex, bool active)
        {
            if (!info.m_prefabInitialized)
                return;

            if (info.m_hasEffects && (active || info.m_alwaysActive))
            {
                var matrix = default(Matrix4x4);
                matrix.SetTRS(position, Quaternion.AngleAxis((absoluteAngle + angle) * Mathf.Rad2Deg, Vector3.down), new Vector3(scale, scale, scale));
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

            if (cameraInfo == null || cameraInfo.CheckRenderDistance(position, Settings.PropLODDistance))
            {
                var matrix = default(Matrix4x4);
                var rotation = Quaternion.AngleAxis(absoluteAngle * Mathf.Rad2Deg, Vector3.down);
                rotation *= Quaternion.AngleAxis(slope * Mathf.Rad2Deg, Vector3.forward);
                rotation *= Quaternion.AngleAxis(tilt * Mathf.Rad2Deg, Vector3.right);
                rotation *= Quaternion.AngleAxis(angle * Mathf.Rad2Deg, Vector3.down);
                matrix.SetTRS(position, rotation, new Vector3(scale, scale, scale));
                var instance = Singleton<PropManager>.instance;
                var materialBlock = instance.m_materialBlock;
                materialBlock.Clear();
                materialBlock.SetColor(instance.ID_Color, color);
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
                var rotation = Quaternion.AngleAxis(absoluteAngle * Mathf.Rad2Deg, Vector3.down);
                rotation *= Quaternion.AngleAxis(slope * Mathf.Rad2Deg, Vector3.forward);
                rotation *= Quaternion.AngleAxis(tilt * Mathf.Rad2Deg, Vector3.right);
                rotation *= Quaternion.AngleAxis(angle * Mathf.Rad2Deg, Vector3.down);
                matrix.SetTRS(position, rotation, new Vector3(scale, scale, scale));
                var instance = Singleton<PropManager>.instance;
                var materialBlock = instance.m_materialBlock;
                materialBlock.Clear();
                materialBlock.SetColor(instance.ID_Color, color);
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
                objectIndex.w = scale;
                info.m_lodLocations[info.m_lodCount] = new Vector4(position.x, position.y, position.z, absoluteAngle + angle);
                info.m_lodObjectIndices[info.m_lodCount] = objectIndex;
                info.m_lodColors[info.m_lodCount] = color.linear;
                info.m_lodMin = Vector3.Min(info.m_lodMin, position);
                info.m_lodMax = Vector3.Max(info.m_lodMax, position);
                if (++info.m_lodCount == info.m_lodLocations.Length)
                    PropInstance.RenderLod(cameraInfo, info);
            }
        }
    }
    public class MarkingTreeData : BaseMarkingPrefabData<TreeInfo>
    {
        public override MarkingLODType LODType => MarkingLODType.Tree;

        public MarkingTreeData(TreeInfo info, MarkingPropItemData[] items) : base(info, items) { }

        public override void Draw(RenderManager.CameraInfo cameraInfo, RenderManager.Instance data, bool infoView)
        {
            foreach (var item in Items)
                RenderInstance(cameraInfo, Info, item.position, item.scale, item.absoluteAngle, item.angle, item.tilt, item.slope, 1f, new Vector4());
        }

        public static void RenderInstance(RenderManager.CameraInfo cameraInfo, TreeInfo info, Vector3 position, float scale, float absoluteAngle, float angle, float tilt, float slope, float brightness, Vector4 objectIndex)
        {
            if (!info.m_prefabInitialized)
                return;

            if (cameraInfo == null || info.m_lodMesh1 == null || cameraInfo.CheckRenderDistance(position, Settings.TreeLODDistance))
            {
                var instance = Singleton<TreeManager>.instance;
                var materialBlock = instance.m_materialBlock;
                var matrix = default(Matrix4x4);
                var rotation = Quaternion.AngleAxis(absoluteAngle * Mathf.Rad2Deg, Vector3.down);
                rotation *= Quaternion.AngleAxis(slope * Mathf.Rad2Deg, Vector3.forward);
                rotation *= Quaternion.AngleAxis(tilt * Mathf.Rad2Deg, Vector3.right);
                rotation *= Quaternion.AngleAxis(angle * Mathf.Rad2Deg, Vector3.down);
                matrix.SetTRS(position, rotation, new Vector3(scale, scale, scale));
                var color = info.m_defaultColor * brightness;
                color.a = Singleton<WeatherManager>.instance.GetWindSpeed(position);
                materialBlock.Clear();
                materialBlock.SetColor(instance.ID_Color, color);
                materialBlock.SetVector(instance.ID_ObjectIndex, objectIndex);
                instance.m_drawCallData.m_defaultCalls++;
                Graphics.DrawMesh(info.m_mesh, matrix, info.m_material, RenderHelper.RoadLayer, null, 0, materialBlock);
            }
            else
            {
                position.y += info.m_generatedInfo.m_center.y * (scale - 1f);
                var color = info.m_defaultColor * brightness;
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
