﻿using IMT.Manager;
using ModsCommon;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

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
        public static int RoadLayer => 9;

        static RenderHelper()
        {
            MaterialLib = new Dictionary<MaterialType, Material>()
            {
            { MaterialType.Pavement, CreateRoadMaterial(MaterialType.Pavement, TextureHelper.CreateTexture(128,128,Color.white), TextureHelper.CreateTexture(128,128,Color.black)) },
            { MaterialType.Grass, CreateRoadMaterial(MaterialType.Grass, TextureHelper.CreateTexture(128,128,Color.white), TextureHelper.CreateTexture(128,128,new Color32(255, 0, 0, 255)))},
            { MaterialType.Gravel, CreateRoadMaterial(MaterialType.Gravel, TextureHelper.CreateTexture(128,128,Color.white), TextureHelper.CreateTexture(128,128,Color.black))},
            { MaterialType.Asphalt, CreateRoadMaterial(MaterialType.Asphalt, TextureHelper.CreateTexture(128,128,Color.white), TextureHelper.CreateTexture(128,128,new Color32(0, 255, 255, 255)))},
            { MaterialType.Ruined, CreateRoadMaterial(MaterialType.Ruined, TextureHelper.CreateTexture(128,128,Color.white), TextureHelper.CreateTexture(128,128,Color.black))},
            { MaterialType.Cliff, CreateRoadMaterial(MaterialType.Cliff, TextureHelper.CreateTexture(128,128,Color.white), TextureHelper.CreateTexture(128,128,Color.black))},
            };

            SurfaceALib = new Dictionary<MaterialType, Texture2D>()
            {
            { MaterialType.Pavement, TextureHelper.CreateTexture(512, 512, new Color32(255, 255, 127, 127)) },
            { MaterialType.Grass, TextureHelper.CreateTexture(512, 512, new Color32(255, 255, 127, 127)) },
            { MaterialType.Gravel, TextureHelper.CreateTexture(512, 512, new Color32(0, 0, 127, 127)) },
            { MaterialType.Asphalt, TextureHelper.CreateTexture(512, 512, new Color32(0, 0, 0, 127)) },
            { MaterialType.Ruined, TextureHelper.CreateTexture(512, 512, new Color32(0, 0, 191, 127)) },
            { MaterialType.Cliff, TextureHelper.CreateTexture(512, 512, new Color32(0, 0, 127, 191)) },
            };

            SurfaceBLib = new Dictionary<MaterialType, Texture2D>()
            {
            { MaterialType.Pavement, TextureHelper.CreateTexture(512, 512, new Color32(0, 0, 0, 0)) },
            { MaterialType.Grass, TextureHelper.CreateTexture(512, 512, new Color32(0, 0, 0, 0)) },
            { MaterialType.Gravel, TextureHelper.CreateTexture(512, 512, new Color32(0, 0, 255, 0)) },
            { MaterialType.Asphalt, TextureHelper.CreateTexture(512, 512, new Color32(0, 0, 0, 0)) },
            { MaterialType.Ruined, TextureHelper.CreateTexture(512, 512, new Color32(0, 0, 0, 0)) },
            { MaterialType.Cliff, TextureHelper.CreateTexture(512, 512, new Color32(0, 0, 0, 0)) },
            };
        }

        public static void LoadBundle()
        {
            if (Bundle != null)
                UnloadBundle();

            try
            {
                SingletonMod<Mod>.Logger.Debug($"Start loading bundle for {Application.platform}");

                var file = Application.platform switch
                {
                    RuntimePlatform.WindowsPlayer => "imt-win",
                    RuntimePlatform.OSXPlayer => "imt-macos",
                    RuntimePlatform.LinuxPlayer => "imt-linux",
                    _ => throw new PlatformNotSupportedException()
                };

                var data = ResourceUtility.LoadResource($"{nameof(IMT)}.Resources.{file}");
                Bundle = AssetBundle.LoadFromMemory(data);

                var materials = new Material[]
                {
                    Bundle.LoadAsset<Material>("AreaZero.mat"),
                    Bundle.LoadAsset<Material>("AreaUpTo4.mat"),
                    Bundle.LoadAsset<Material>("AreaUpTo8.mat"),
                    Bundle.LoadAsset<Material>("AreaUpTo12.mat"),
                    Bundle.LoadAsset<Material>("AreaUpTo16.mat"),
                    Bundle.LoadAsset<Material>("Dash.mat"),
                    Bundle.LoadAsset<Material>("Triangle.mat"),
                };
                DecalMaterials = materials;

                MaterialLib[MaterialType.AreaZero] = materials[0];
                MaterialLib[MaterialType.AreaUpTo4] = materials[1];
                MaterialLib[MaterialType.AreaUpTo8] = materials[2];
                MaterialLib[MaterialType.AreaUpTo12] = materials[3];
                MaterialLib[MaterialType.AreaUpTo16] = materials[4];
                MaterialLib[MaterialType.Dash] = materials[5];
                MaterialLib[MaterialType.Triangle] = materials[6];

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

        public static Material CreateRoadMaterial(MaterialType type, Texture2D texture, Texture2D apr = null, int renderQueue = 2461)
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
        AreaZero,
        AreaUpTo4,
        AreaUpTo8,
        AreaUpTo12,
        AreaUpTo16,
        Pavement,
        Grass,
        Gravel,
        Asphalt,
        Ruined,
        Cliff,
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

    public interface IDrawData
    {
        void Draw(RenderManager.CameraInfo cameraInfo, RenderManager.Instance data, bool infoView);
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
    public class MarkingRenderData : EnumDictionary<MarkingLODType, MarkingGroupDrawData>
    {
        protected override MarkingGroupDrawData GetDefault(MarkingLODType value) => value switch
        {
            MarkingLODType.Dash => new MarkingDashGroupDrawData(),
            MarkingLODType.Mesh => new MarkingMeshGroupDrawData(),
            MarkingLODType.Network => new MarkingNetworkGroupDrawData(),
            MarkingLODType.Prop => new MarkingPropGroupDrawData(),
            MarkingLODType.Tree => new MarkingTreeGroupDrawData(),
            _ => new MarkingGroupDrawData(),
        };
    }
    public class MarkingGroupDrawData : EnumDictionary<MarkingLOD, List<IDrawData>>
    {
        public virtual float LODDistance => Settings.LODDistance;
        public void Render(RenderManager.CameraInfo cameraInfo, RenderManager.Instance data, bool infoView)
        {
            foreach (var drawData in this[MarkingLOD.NoLOD])
                drawData.Draw(cameraInfo, data, infoView);

            if (cameraInfo.CheckRenderDistance(data.m_position, LODDistance))
            {
                foreach (var drawData in this[MarkingLOD.LOD0])
                    drawData.Draw(cameraInfo, data, infoView);
            }
            else
            {
                foreach (var drawData in this[MarkingLOD.LOD1])
                    drawData.Draw(cameraInfo, data, infoView);
            }
        }
        protected override List<IDrawData> GetDefault(MarkingLOD value) => new List<IDrawData>();
    }
    public class MarkingDashGroupDrawData : MarkingGroupDrawData
    {
        public override float LODDistance => Settings.LODDistance;
    }
    public class MarkingMeshGroupDrawData : MarkingGroupDrawData
    {
        public override float LODDistance => Settings.MeshLODDistance;
    }
    public class MarkingNetworkGroupDrawData : MarkingGroupDrawData
    {
        public override float LODDistance => Settings.NetworkLODDistance;
    }
    public class MarkingPropGroupDrawData : MarkingGroupDrawData
    {
        public override float LODDistance => Settings.PropLODDistance;
    }
    public class MarkingTreeGroupDrawData : MarkingGroupDrawData
    {
        public override float LODDistance => Settings.TreeLODDistance;
    }
    public class RenderGroupData : IStyleData
    {
        private IStyleData[] Datas { get; }
        public MarkingLOD LOD { get; }
        public MarkingLODType LODType { get; }

        public RenderGroupData(MarkingLOD lod, MarkingLODType lodType, IStyleData[] datas)
        {
            LOD = lod;
            LODType = lodType;
            Datas = datas;
        }

        public IEnumerable<IDrawData> GetDrawData()
        {
            foreach (var data in Datas)
            {
                foreach (var drawData in data.GetDrawData())
                    yield return drawData;
            }
        }
    }

}