using System;
using UnityEngine;

namespace NodeMarkup.UI
{
    public class GUIButton
    {
        private float Size => 50;
        private float Padding => 5;

        public event Action OnClick;

        private int IndexX { get; }
        private int OfX { get; }
        private int IndexY { get; }
        private int OfY { get; }
        private Texture2D Texture { get; }
        private Rect Coords { get; }
        private Rect Position { get; set; }


        public GUIButton(int indexX, int ofX, int indexY, int ofY, Texture2D texture, Rect coords)
        {
            IndexX = indexX;
            OfX = ofX;
            IndexY = indexY;
            OfY = ofY;
            Texture = texture;
            Coords = coords;
        }

        public void Update(Vector2 screenPos)
        {
            Position = GetPosition(screenPos, IndexX, OfX, IndexY, OfY);
        }
        public void CheckClick(Vector2 mouse)
        {
            if (CheckHover(mouse))
                OnClick?.Invoke();
        }
        public bool CheckHover(Vector2 mouse) => Position.Contains(mouse);

        public void OnGUI(Event e) => GUI.DrawTextureWithTexCoords(Position, Texture, Coords);

        private Rect GetPosition(Vector2 centre, int iX, int ofX, int iY, int ofY)
        {
            var sumX = ofX * Size + (ofX - 1) * Padding;
            var x = centre.x - sumX / 2 + (iX - 1) * (Size + Padding);
            var sumY = ofY * Size + (ofY - 1) * Padding;
            var y = centre.y - sumY / 2 + (iY - 1) * (Size + Padding);
            return new Rect(x, y, Size, Size);
        }
    }
}
