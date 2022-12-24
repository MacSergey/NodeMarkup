using ColossalFramework.UI;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace NodeMarkup.Utilities
{
    public static class RenderHelper
    {
        public static Dictionary<MaterialType, Material> MaterialLib { get; private set; } = new Dictionary<MaterialType, Material>()
        {
            { MaterialType.RectangleLines, CreateDecalMaterial(TextureHelper.CreateTexture(1,1,Color.white))},
            { MaterialType.RectangleFillers, CreateDecalMaterial(TextureHelper.CreateTexture(1,1,Color.white), renderQueue: 2459)},
            { MaterialType.Triangle, CreateDecalMaterial(TextureHelper.CreateTexture(64,64,Color.white), Assembly.GetExecutingAssembly().LoadTextureFromAssembly("SharkTooth"))},
            { MaterialType.Pavement, CreateRoadMaterial(MaterialType.Pavement, TextureHelper.CreateTexture(128,128,Color.white), TextureHelper.CreateTexture(128,128,Color.black)) },
            { MaterialType.Grass, CreateRoadMaterial(MaterialType.Grass, TextureHelper.CreateTexture(128,128,Color.white), TextureHelper.CreateTexture(128,128,new Color32(255, 0, 0, 255)))},
            { MaterialType.Gravel, CreateRoadMaterial(MaterialType.Gravel, TextureHelper.CreateTexture(128,128,Color.white), TextureHelper.CreateTexture(128,128,Color.black))},
            { MaterialType.Asphalt, CreateRoadMaterial(MaterialType.Asphalt, TextureHelper.CreateTexture(128,128,Color.white), TextureHelper.CreateTexture(128,128,new Color32(0, 255, 255, 255)))},
            { MaterialType.Ruined, CreateRoadMaterial(MaterialType.Ruined, TextureHelper.CreateTexture(128,128,Color.white), TextureHelper.CreateTexture(128,128,Color.black))},
            { MaterialType.Cliff, CreateRoadMaterial(MaterialType.Cliff, TextureHelper.CreateTexture(128,128,Color.white), TextureHelper.CreateTexture(128,128,Color.black))},
        };
        public static Dictionary<MaterialType, Texture2D> SurfaceALib { get; } = new Dictionary<MaterialType, Texture2D>()
        {
            { MaterialType.Pavement, TextureHelper.CreateTexture(512, 512, new Color32(255, 255, 127, 127)) },
            { MaterialType.Grass, TextureHelper.CreateTexture(512, 512, new Color32(255, 255, 127, 127)) },
            { MaterialType.Gravel, TextureHelper.CreateTexture(512, 512, new Color32(0, 0, 127, 127)) },
            { MaterialType.Asphalt, TextureHelper.CreateTexture(512, 512, new Color32(0, 0, 0, 127)) },
            { MaterialType.Ruined, TextureHelper.CreateTexture(512, 512, new Color32(0, 0, 191, 127)) },
            { MaterialType.Cliff, TextureHelper.CreateTexture(512, 512, new Color32(0, 0, 127, 191)) },
        };
        public static Dictionary<MaterialType, Texture2D> SurfaceBLib { get; } = new Dictionary<MaterialType, Texture2D>()
        {
            { MaterialType.Pavement, TextureHelper.CreateTexture(512, 512, new Color32(0, 0, 0, 0)) },
            { MaterialType.Grass, TextureHelper.CreateTexture(512, 512, new Color32(0, 0, 0, 0)) },
            { MaterialType.Gravel, TextureHelper.CreateTexture(512, 512, new Color32(0, 0, 255, 0)) },
            { MaterialType.Asphalt, TextureHelper.CreateTexture(512, 512, new Color32(0, 0, 0, 0)) },
            { MaterialType.Ruined, TextureHelper.CreateTexture(512, 512, new Color32(0, 0, 0, 0)) },
            { MaterialType.Cliff, TextureHelper.CreateTexture(512, 512, new Color32(0, 0, 0, 0)) },
        };


        public static int ID_DecalSize { get; } = Shader.PropertyToID("_DecalSize");
        private static int[] VerticesIdxs { get; } = new int[]
{
            1,3,2,0,// Bottom
            5,7,4,6,// Top
            1,4,0,5,// Front
            0,7,3,4,// Left
            3,6,2,7,// Back
            2,5,1,6, // Right
};
        private static int[] TrianglesIdxs { get; } = new int[]
        {
                0,1,2,      1,0,3,      // Bottom
                4,5,6,      5,4,7,      // Top
                8,9,10,     9,8,11,     // Front
                12,13,14,   13,12,15,   // Left 
                16,17,18,   17,16,19,   // Back
                20,21,22,   21,20,23,    // Right
        };
        private static int VCount { get; } = 24;
        private static int TCount { get; } = 36;

        public static Mesh CreateMesh(int count, Vector3 size)
        {
            var vertices = new Vector3[VCount * count];
            var triangles = new int[TCount * count];
            var colors32 = new Color32[VCount * count];
            var uv = new Vector2[VCount * count];

            CreateTemp(size.x, size.y, size.z, out Vector3[] tempV, out int[] tempT);

            for (var i = 0; i < count; i += 1)
            {
                var tempColor = VerticesColor(i);

                for (var j = 0; j < VCount; j += 1)
                {
                    vertices[i * VCount + j] = tempV[j];
                    colors32[i * VCount + j] = tempColor;
                    uv[i * VCount + j] = Vector2.zero;
                }
                for (var j = 0; j < TCount; j += 1)
                {
                    triangles[i * TCount + j] = tempT[j] + (VCount * i);
                }
            }

            Bounds bounds = default;
            bounds.SetMinMax(new Vector3(-100000f, -100000f, -100000f), new Vector3(100000f, 100000f, 100000f));

            var mesh = new Mesh();
            mesh.Clear();

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.colors32 = colors32;
            mesh.bounds = bounds;
            mesh.uv = uv;

            return mesh;
        }
        private static void CreateTemp(float length, float height, float width, out Vector3[] vertices, out int[] triangles)
        {
            Vector3[] c = new Vector3[8];

            c[0] = new Vector3(-length * .5f, -height * .5f, width * .5f); //0 --+
            c[1] = new Vector3(length * .5f, -height * .5f, width * .5f);  //1 +-+
            c[2] = new Vector3(length * .5f, -height * .5f, -width * .5f); //2 +--
            c[3] = new Vector3(-length * .5f, -height * .5f, -width * .5f);//3 ---

            c[4] = new Vector3(-length * .5f, height * .5f, width * .5f);  //4 -++
            c[5] = new Vector3(length * .5f, height * .5f, width * .5f);   //5 +++
            c[6] = new Vector3(length * .5f, height * .5f, -width * .5f);  //6 ++-
            c[7] = new Vector3(-length * .5f, height * .5f, -width * .5f); //7 -+-

            vertices = new Vector3[24];
            triangles = new int[36];

            for (var j = 0; j < 24; j += 1)
            {
                vertices[j] = c[VerticesIdxs[j]];
            }
            for (var j = 0; j < 36; j += 1)
            {
                triangles[j] = TrianglesIdxs[j];
            }
        }
        public static Color32 VerticesColor(int i) => new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)(16 * i));

        public static Material CreateDecalMaterial(Texture2D texture, Texture2D aci = null, int renderQueue = 2460)
        {
            var material = new Material(Shader.Find("Custom/Props/Decal/Blend"))
            {
                mainTexture = texture,
                name = "NodeMarkupDecal",
                color = new Color(1f, 1f, 1f, 1f),
                doubleSidedGI = false,
                enableInstancing = false,
                globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack,
                renderQueue = renderQueue,
            };
            if (aci != null)
                material.SetTexture("_ACIMap", aci);

            material.EnableKeyword("MULTI_INSTANCE");

            var tiling = new Vector4(1f, 0f, 1f, 0f);
            material.SetVector("_DecalTiling", tiling);

            return material;
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

        public static Texture2D CreateChessBoardTexture()
        {
            var height = 256;
            var width = 256;
            var texture = new Texture2D(height, width)
            {
                name = "Markup",
            };
            for (var i = 0; i < height * width; i += 1)
            {
                var row = i / height;
                var column = i % width;

                var colorRow = row / (height / 4);
                var colorColumn = column / (width / 4);
                var color = (colorColumn + colorRow) % 2 == 0 ? 0f : 1f;
                texture.SetPixel(row, column, new Color(color, color, color, 1));
            }

            texture.Apply();
            return texture;
        }
        public static Texture2D CreateTextTexture(string text, float scale)
        {
            var renderer = new TextRenderHelper.TextRenderer()
            {
                TextScale = scale,
            };

            var texture = renderer.Render(text);
            return texture;
        }

        public static Texture2D CreateTextTexture2(string text, int width, int height)
        {
            var texture = new Texture2D(width, height)
            {
                name = "Text",
            };
            for (var x = 0; x < width; x += 1)
            {
                for (var y = 0; y < height; y += 1)
                {
                    texture.SetPixel(x, y, Color.red);
                }
            }

            var view = UIView.GetAView();
            var renderData = UIRenderData.Obtain();

            var textRenderer = view.defaultFont.ObtainRenderer();
            textRenderer.wordWrap = false;
            textRenderer.multiLine = true;
            textRenderer.maxSize = Vector2.one * int.MaxValue;
            textRenderer.vectorOffset = new Vector3(5f, -5f, 0f);
            textRenderer.pixelRatio = /*view.PixelsToUnits();*/1f;
            textRenderer.textScale = 10f;
            textRenderer.textAlign = UIHorizontalAlignment.Left;
            textRenderer.opacity = 1f;

            textRenderer.Render(text, renderData);
            var fontTexture = renderData.material.mainTexture.MakeReadable();
            File.WriteAllBytes(@"C:\Users\MacSergey\OneDrive\Рабочий стол\Texture_IMT.png", fontTexture.EncodeToPNG());

            Rect area = new Rect();
            for (var i = 0; i < renderData.vertices.Count; i += 1)
            {
                var vertex = renderData.vertices[i];
                if (i == 0)
                {
                    area.x = vertex.x;
                    area.y = vertex.y;
                }
                else
                {
                    area.min = Vector2.Min(area.min, vertex);
                    area.max = Vector2.Max(area.max, vertex);
                }
            }

            var xRatio = texture.width / area.width;
            var yRatio = texture.height / area.height;

            for (var i = 0; i < renderData.vertices.Count; i += 4)
            {
                var charArea = new Rect();
                var charUV = new Rect();
                for (int j = i; j < i + 4; j += 1)
                {
                    var vertex = renderData.vertices[j];
                    var uv = renderData.uvs[j];
                    if (j == i)
                    {
                        charArea.x = vertex.x;
                        charArea.y = vertex.y;
                        charUV.x = uv.x;
                        charUV.y = uv.y;
                    }
                    else
                    {
                        charArea.min = Vector2.Min(charArea.min, vertex);
                        charArea.max = Vector2.Max(charArea.max, vertex);
                        charUV.min = Vector2.Min(charUV.min, uv);
                        charUV.max = Vector2.Max(charUV.max, uv);
                    }
                }
                charArea.position -= area.position;

                charArea.xMin *= xRatio;
                charArea.xMax *= xRatio;
                charArea.yMin *= yRatio;
                charArea.yMax *= yRatio;

                charUV.xMin *= fontTexture.width;
                charUV.xMax *= fontTexture.width;
                charUV.yMin *= fontTexture.height;
                charUV.yMax *= fontTexture.height;

                var tepmTexture = new Texture2D((int)charUV.width, (int)charUV.height)
                {
                    name = "Text",
                };

                Graphics.CopyTexture(fontTexture, 0, 0, (int)charUV.x, (int)charUV.y, (int)charUV.width, (int)charUV.height, tepmTexture, 0, 0, 0, 0);
                tepmTexture.Apply();
                File.WriteAllBytes(@$"C:\Users\MacSergey\OneDrive\Рабочий стол\Texture_IMT{i}.png", tepmTexture.EncodeToPNG());
                //for (var x = (int)charArea.xMin; x < (int)charArea.xMax; x += 1)
                //{
                //    for (var y = (int)charArea.yMin; y < (int)charArea.yMax; y += 1)
                //    {
                //        if (x >= 0 && x < 256 && y >= 0 && y < 256)
                //        {
                //            var uvx = charUV.xMin + charUV.width * (x / (float)charArea.width);
                //            var uvy = charUV.yMin + charUV.height * (y / (float)charArea.height);
                //            var color = fontTexture.GetPixel((int)(fontTexture.width * uvx), (int)(fontTexture.height * uvy));
                //            texture.SetPixel(x, y, new Color(1f - color.a, 0f, 0f, 1f));
                //        }
                //    }
                //}
            }

            texture.Apply();
            //File.WriteAllBytes(@"C:\Users\MacSergey\OneDrive\Рабочий стол\Texture_IMT.png", texture.EncodeToPNG());
            return texture;
        }
    }

    public enum MaterialType
    {
        RectangleLines,
        RectangleFillers,
        Triangle,
        Pavement,
        Grass,
        Gravel,
        Asphalt,
        Ruined,
        Cliff,
    }
    public enum MarkupLOD
    {
        NoLOD = 1,
        LOD0 = 2,
        LOD1 = 4,
    }
    public enum MarkupLODType
    {
        Dash,
        Mesh,
        Network,
        Prop,
        Tree,
    }

    public interface IDrawData
    {
        public void Draw(RenderManager.CameraInfo cameraInfo, RenderManager.Instance data, bool infoView);
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
    public class MarkupRenderData : EnumDictionary<MarkupLODType, MarkupGroupDrawData>
    {
        protected override MarkupGroupDrawData GetDefault(MarkupLODType value) => value switch
        {
            MarkupLODType.Dash => new MarkupDashGroupDrawData(),
            MarkupLODType.Mesh => new MarkupMeshGroupDrawData(),
            MarkupLODType.Network => new MarkupNetworkGroupDrawData(),
            MarkupLODType.Prop => new MarkupPropGroupDrawData(),
            MarkupLODType.Tree => new MarkupTreeGroupDrawData(),
            _ => new MarkupGroupDrawData(),
        };
    }
    public class MarkupGroupDrawData : EnumDictionary<MarkupLOD, List<IDrawData>>
    {
        public virtual float LODDistance => Settings.LODDistance;
        public void Render(RenderManager.CameraInfo cameraInfo, RenderManager.Instance data, bool infoView)
        {
            foreach (var drawData in this[MarkupLOD.NoLOD])
                drawData.Draw(cameraInfo, data, infoView);

            if (cameraInfo.CheckRenderDistance(data.m_position, LODDistance))
            {
                foreach (var drawData in this[MarkupLOD.LOD0])
                    drawData.Draw(cameraInfo, data, infoView);
            }
            else
            {
                foreach (var drawData in this[MarkupLOD.LOD1])
                    drawData.Draw(cameraInfo, data, infoView);
            }
        }
        protected override List<IDrawData> GetDefault(MarkupLOD value) => new List<IDrawData>();
    }
    public class MarkupDashGroupDrawData : MarkupGroupDrawData
    {
        public override float LODDistance => Settings.LODDistance;
    }
    public class MarkupMeshGroupDrawData : MarkupGroupDrawData
    {
        public override float LODDistance => Settings.MeshLODDistance;
    }
    public class MarkupNetworkGroupDrawData : MarkupGroupDrawData
    {
        public override float LODDistance => Settings.NetworkLODDistance;
    }
    public class MarkupPropGroupDrawData : MarkupGroupDrawData
    {
        public override float LODDistance => Settings.PropLODDistance;
    }
    public class MarkupTreeGroupDrawData : MarkupGroupDrawData
    {
        public override float LODDistance => Settings.TreeLODDistance;
    }
}
