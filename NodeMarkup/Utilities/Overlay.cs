using UnityEngine;

namespace NodeMarkup.Utilities
{
    public struct OverlayData
    {
        public RenderManager.CameraInfo CameraInfo;
        public Color? Color;
        public float? Width;
        public bool? AlphaBlend;
        public bool? CutStart;
        public bool? CutEnd;
        public bool SplitPoint;

        public bool Cut
        {
            set
            {
                CutStart = value;
                CutEnd = value;
            }
        }

        public OverlayData(RenderManager.CameraInfo cameraInfo)
        {
            CameraInfo = cameraInfo;
            Color = null;
            Width = null;
            AlphaBlend = null;
            CutStart = null;
            CutEnd = null;
            SplitPoint = false;
        }
    }
}
