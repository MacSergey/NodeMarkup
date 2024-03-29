﻿using IMT.Manager;
using ModsCommon;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace IMT.Utilities
{
    public static class RenderHelper
    {
        private static AssetBundle Bundle { get; set; }
        private static Material[] DecalMaterials { get; set; }
        public static Mesh DecalMesh { get; private set; }
        public static Dictionary<MaterialType, Material> MaterialLib { get; }
        public static Dictionary<MaterialType, Texture2D> SurfaceALib { get; }
        public static Dictionary<MaterialType, Texture2D> SurfaceBLib { get; }
        public static Material ThemeTexture { get; private set; }
        public static int RoadLayer => 9;

        static RenderHelper()
        {
            MaterialLib = new Dictionary<MaterialType, Material>()
            {
            { MaterialType.Pavement, CreateRoadMaterial(MaterialType.Pavement, TextureHelper.CreateTexture(128,128,Color.white), TextureHelper.CreateTexture(128,128,Color.black)) },
            { MaterialType.FillerGrass, CreateRoadMaterial(MaterialType.FillerGrass, TextureHelper.CreateTexture(128,128,Color.white), TextureHelper.CreateTexture(128,128,new Color32(255, 0, 0, 255)))},
            { MaterialType.FillerGravel, CreateRoadMaterial(MaterialType.FillerGravel, TextureHelper.CreateTexture(128,128,Color.white), TextureHelper.CreateTexture(128,128,Color.black))},
            { MaterialType.FillerAsphalt, CreateRoadMaterial(MaterialType.FillerAsphalt, TextureHelper.CreateTexture(128,128,Color.white), TextureHelper.CreateTexture(128,128,new Color32(0, 255, 255, 255)))},
            { MaterialType.FillerRuined, CreateRoadMaterial(MaterialType.FillerRuined, TextureHelper.CreateTexture(128,128,Color.white), TextureHelper.CreateTexture(128,128,Color.black))},
            { MaterialType.FillerCliff, CreateRoadMaterial(MaterialType.FillerCliff, TextureHelper.CreateTexture(128,128,Color.white), TextureHelper.CreateTexture(128,128,Color.black))},
            };

            SurfaceALib = new Dictionary<MaterialType, Texture2D>()
            {
            { MaterialType.Pavement, TextureHelper.CreateTexture(512, 512, new Color32(255, 255, 127, 127)) },
            { MaterialType.FillerGrass, TextureHelper.CreateTexture(512, 512, new Color32(255, 255, 127, 127)) },
            { MaterialType.FillerGravel, TextureHelper.CreateTexture(512, 512, new Color32(0, 0, 127, 127)) },
            { MaterialType.FillerAsphalt, TextureHelper.CreateTexture(512, 512, new Color32(0, 0, 0, 127)) },
            { MaterialType.FillerRuined, TextureHelper.CreateTexture(512, 512, new Color32(0, 0, 191, 127)) },
            { MaterialType.FillerCliff, TextureHelper.CreateTexture(512, 512, new Color32(0, 0, 127, 191)) },
            };

            SurfaceBLib = new Dictionary<MaterialType, Texture2D>()
            {
            { MaterialType.Pavement, TextureHelper.CreateTexture(512, 512, new Color32(0, 0, 0, 0)) },
            { MaterialType.FillerGrass, TextureHelper.CreateTexture(512, 512, new Color32(0, 0, 0, 0)) },
            { MaterialType.FillerGravel, TextureHelper.CreateTexture(512, 512, new Color32(0, 0, 255, 0)) },
            { MaterialType.FillerAsphalt, TextureHelper.CreateTexture(512, 512, new Color32(0, 0, 0, 0)) },
            { MaterialType.FillerRuined, TextureHelper.CreateTexture(512, 512, new Color32(0, 0, 0, 0)) },
            { MaterialType.FillerCliff, TextureHelper.CreateTexture(512, 512, new Color32(0, 0, 0, 0)) },
            };
        }

        public static void LoadBundle()
        {
            if (Bundle != null)
                UnloadBundle();

            try
            {
                SingletonMod<Mod>.Logger.Debug($"Start loading bundle for {Application.platform}:{SystemInfo.graphicsDeviceType}");

                var file = Application.platform switch
                {
                    RuntimePlatform.WindowsPlayer when SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D9 => "imt-win",
                    RuntimePlatform.WindowsPlayer when SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11 => "imt-win",
                    RuntimePlatform.WindowsPlayer when SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore => "imt-win",
                    RuntimePlatform.OSXPlayer when SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal => "imt-macos",
                    RuntimePlatform.OSXPlayer when SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore => "imt-macos",
                    RuntimePlatform.LinuxPlayer when SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore => "imt-linux",
                    _ => throw new PlatformNotSupportedException()
                };

                var data = ResourceUtility.LoadResource($"{nameof(IMT)}.Resources.{file}");
                Bundle = AssetBundle.LoadFromMemory(data);

                var materials = new Material[]
                {
                    Bundle.LoadAsset<Material>("FillerZero.mat"),
                    Bundle.LoadAsset<Material>("FillerUpTo4.mat"),
                    Bundle.LoadAsset<Material>("FillerUpTo8.mat"),
                    Bundle.LoadAsset<Material>("FillerUpTo12.mat"),
                    Bundle.LoadAsset<Material>("FillerUpTo16.mat"),
                    Bundle.LoadAsset<Material>("FillerIslandZero.mat"),
                    Bundle.LoadAsset<Material>("FillerIslandUpTo4.mat"),
                    Bundle.LoadAsset<Material>("FillerIslandUpTo8.mat"),
                    Bundle.LoadAsset<Material>("FillerIslandUpTo12.mat"),
                    Bundle.LoadAsset<Material>("FillerIslandUpTo16.mat"),
                    Bundle.LoadAsset<Material>("CrosswalkZero.mat"),
                    Bundle.LoadAsset<Material>("CrosswalkUpTo4.mat"),
                    Bundle.LoadAsset<Material>("CrosswalkUpTo8.mat"),
                    Bundle.LoadAsset<Material>("CrosswalkUpTo12.mat"),
                    Bundle.LoadAsset<Material>("CrosswalkUpTo16.mat"),
                    Bundle.LoadAsset<Material>("Dash.mat"),
                    Bundle.LoadAsset<Material>("Triangle.mat"),
                    Bundle.LoadAsset<Material>("Text.mat"),
                    Bundle.LoadAsset<Material>("Filler3D.mat"),
                    Bundle.LoadAsset<Material>("ThemeTexture.mat"),
                };
                DecalMaterials = materials;

                MaterialLib[MaterialType.FillerZero] = materials[0];
                MaterialLib[MaterialType.FillerUpTo4] = materials[1];
                MaterialLib[MaterialType.FillerUpTo8] = materials[2];
                MaterialLib[MaterialType.FillerUpTo12] = materials[3];
                MaterialLib[MaterialType.FillerUpTo16] = materials[4];
                MaterialLib[MaterialType.FillerIslandZero] = materials[5];
                MaterialLib[MaterialType.FillerIslandUpTo4] = materials[6];
                MaterialLib[MaterialType.FillerIslandUpTo8] = materials[7];
                MaterialLib[MaterialType.FillerIslandUpTo12] = materials[8];
                MaterialLib[MaterialType.FillerIslandUpTo16] = materials[9];
                MaterialLib[MaterialType.CrosswalkZero] = materials[10];
                MaterialLib[MaterialType.CrosswalkUpTo4] = materials[11];
                MaterialLib[MaterialType.CrosswalkUpTo8] = materials[12];
                MaterialLib[MaterialType.CrosswalkUpTo12] = materials[13];
                MaterialLib[MaterialType.CrosswalkUpTo16] = materials[14];
                MaterialLib[MaterialType.Dash] = materials[15];
                MaterialLib[MaterialType.Triangle] = materials[16];
                MaterialLib[MaterialType.Text] = materials[17];
                MaterialLib[MaterialType.FillerTexture] = materials[18];
                ThemeTexture = materials[19];

                DecalMesh = Bundle.LoadAsset<Mesh>("Cube.fbx");
                DecalMesh.bounds = new Bounds(DecalMesh.bounds.center, Vector3.one * 100f);

                SingletonMod<Mod>.Logger.Debug("Bundle loaded");
            }
            catch (Exception error)
            {
                SingletonMod<Mod>.Logger.Error("Can't load bundle", error);
            }
        }
        public static void UnloadBundle()
        {
            try
            {
                SingletonMod<Mod>.Logger.Debug("Start unloading bundle");

                if (Bundle != null)
                    Bundle.Unload(true);

                DecalMaterials = null;
                DecalMesh = null;

                SingletonMod<Mod>.Logger.Debug("Bundle unloaded");
            }
            catch (Exception error)
            {
                SingletonMod<Mod>.Logger.Error("Can't unload bundle", error);
            }
        }

        public static Material CreateRoadMaterial(MaterialType type, Texture2D texture, Texture2D apr = null, int renderQueue = 2465)
        {
            var material = new Material(Shader.Find("Custom/Net/Road"))
            {
                name = type.ToString(),
                mainTexture = texture,
                color = new Color(0.6f, 0.6f, 0.6f, 0f),
                renderQueue = renderQueue,
            };

            if (apr != null)
                material.SetTexture("_APRMap", apr);

            material.EnableKeyword("NET_SEGMENT");

            return material;
        }

        public static Texture2D CreateTextTexture(string font, string text, float scale, Vector2 spacing, out float textWidth, out float textHeight)
        {
            var renderer = new TextRenderHelper.TextRenderer(font)
            {
                TextScale = scale,
                Spacing = spacing,
            };

            var texture = renderer.Render(text, out textWidth, out textHeight);
            return texture;
        }
    }

    public enum MaterialType
    {
        Dash,
        Triangle,
        Text,
        FillerZero,
        FillerUpTo4,
        FillerUpTo8,
        FillerUpTo12,
        FillerUpTo16,
        FillerIslandZero,
        FillerIslandUpTo4,
        FillerIslandUpTo8,
        FillerIslandUpTo12,
        FillerIslandUpTo16,
        CrosswalkZero,
        CrosswalkUpTo4,
        CrosswalkUpTo8,
        CrosswalkUpTo12,
        CrosswalkUpTo16,
        Pavement,
        FillerGrass,
        FillerGravel,
        FillerAsphalt,
        FillerRuined,
        FillerCliff,
        FillerTexture,
    }
    public enum MarkingLOD
    {
        NoLOD = 1,
        LOD0 = 2,
        LOD1 = 4,
    }
    public enum MarkingLODType
    {
        Dash,
        Mesh,
        Network,
        Prop,
        Tree,
    }

    public abstract class EnumDictionary<EnumType, Type> : Dictionary<EnumType, Type>
        where EnumType : Enum
    {
        public EnumDictionary()
        {
            Clear();
        }
        public new void Clear()
        {
            foreach (var item in EnumExtension.GetEnumValues<EnumType>())
                this[item] = GetDefault(item);
        }
        protected abstract Type GetDefault(EnumType value);
    }
    public class MarkingRenderData : EnumDictionary<MarkingLODType, MarkingGroupRenderData>
    {
        protected override MarkingGroupRenderData GetDefault(MarkingLODType value) => value switch
        {
            MarkingLODType.Dash => new MarkingDashGroupRenderData(),
            MarkingLODType.Mesh => new MarkingMeshGroupRenderData(),
            MarkingLODType.Network => new MarkingNetworkGroupRenderData(),
            MarkingLODType.Prop => new MarkingPropGroupRenderData(),
            MarkingLODType.Tree => new MarkingTreeGroupRenderData(),
            _ => throw new NotSupportedException(),
        };

        public int GetRenderLayers()
        {
            var renderLayers = 0;

            foreach (var lodType in Values)
                renderLayers |= lodType.GetRenderLayers();

            return renderLayers;
        }
    }
    public abstract class MarkingGroupRenderData : EnumDictionary<MarkingLOD, List<IStyleData>>
    {
        public virtual float LODDistance => Settings.LODDistance;
        public virtual float MaxRenderDistance => Settings.RenderDistance;
        public void Render(RenderManager.CameraInfo cameraInfo, RenderManager.Instance data, bool infoView)
        {
            if (cameraInfo.CheckRenderDistance(data.m_position, MaxRenderDistance))
            {
                foreach (var renderData in this[MarkingLOD.NoLOD])
                {
                    renderData.Render(cameraInfo, data, infoView);
                }

                if (cameraInfo.CheckRenderDistance(data.m_position, LODDistance))
                {
                    foreach (var renderData in this[MarkingLOD.LOD0])
                    {
                        renderData.Render(cameraInfo, data, infoView);
                    }
                }
                else
                {
                    foreach (var renderData in this[MarkingLOD.LOD1])
                    {
                        renderData.Render(cameraInfo, data, infoView);
                    }
                }
            }
        }

        public bool CalculateGroupData(int layer, ref int vertexCount, ref int triangleCount, ref int objectCount, ref RenderGroup.VertexArrays vertexArrays)
        {
            bool result = false;

            foreach (var renderData in this[MarkingLOD.NoLOD])
            {
                if (renderData.RenderLayer == layer)
                {
                    result |= renderData.CalculateGroupData(layer, ref vertexCount, ref triangleCount, ref objectCount, ref vertexArrays);
                }
            }

            foreach (var renderData in this[MarkingLOD.LOD1])
            {
                if (renderData.RenderLayer == layer)
                {
                    result |= renderData.CalculateGroupData(layer, ref vertexCount, ref triangleCount, ref objectCount, ref vertexArrays);
                }
            }

            return result;
        }

        public void PopulateGroupData(int layer, ref int vertexIndex, ref int triangleIndex, Vector3 groupPosition, RenderGroup.MeshData data, ref Vector3 min, ref Vector3 max, ref float maxRenderDistance, ref float maxInstanceDistance, ref bool requireSurfaceMaps)
        {
            foreach (var renderData in this[MarkingLOD.NoLOD])
            {
                if (renderData.RenderLayer == layer)
                {
                    renderData.PopulateGroupData(layer, ref vertexIndex, ref triangleIndex, groupPosition, data, ref min, ref max, ref maxRenderDistance, ref maxInstanceDistance, ref requireSurfaceMaps);
                }
            }

            foreach (var renderData in this[MarkingLOD.LOD1])
            {
                if (renderData.RenderLayer == layer)
                {
                    renderData.PopulateGroupData(layer, ref vertexIndex, ref triangleIndex, groupPosition, data, ref min, ref max, ref maxRenderDistance, ref maxInstanceDistance, ref requireSurfaceMaps);
                }
            }
        }
        public int GetRenderLayers()
        {
            var renderLayers = 0;

            foreach (var renderData in this[MarkingLOD.NoLOD])
                renderLayers |= 1 << renderData.RenderLayer;

            foreach (var renderData in this[MarkingLOD.LOD1])
                renderLayers |= 1 << renderData.RenderLayer;

            return renderLayers;
        }

        protected override List<IStyleData> GetDefault(MarkingLOD value) => new List<IStyleData>();
    }
    public class MarkingDashGroupRenderData : MarkingGroupRenderData
    {
        public override float LODDistance => Settings.LODDistance;
    }
    public class MarkingMeshGroupRenderData : MarkingGroupRenderData
    {
        public override float LODDistance => Settings.MeshLODDistance;
    }
    public class MarkingNetworkGroupRenderData : MarkingGroupRenderData
    {
        public override float LODDistance => Settings.NetworkLODDistance;
        public override float MaxRenderDistance => 100000f;
    }
    public class MarkingPropGroupRenderData : MarkingGroupRenderData
    {
        public override float LODDistance => Settings.PropLODDistance;
        public override float MaxRenderDistance => 100000f;
    }
    public class MarkingTreeGroupRenderData : MarkingGroupRenderData
    {
        public override float LODDistance => Settings.TreeLODDistance;
        public override float MaxRenderDistance => 100000f;
    }
}
