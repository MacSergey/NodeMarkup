using System;
using UnityEngine;

namespace IMT.UI
{
    public class GUIButton
    {
        private float Size => 50;
        private float Padding => 5;

        public event Action OnClick;

        private int IndexX { get; }
        private int OfX { get; }
        private Texture2D Texture { get; }
        private Rect Coords { get; }
        private Rect Position { get; set; }


        public GUIButton(int indexX, int ofX, Texture2D texture, Rect coords)
        {
            IndexX = indexX;
            OfX = ofX;
            Texture = texture;
            Coords = coords;
        }

        public void Update(Vector2 screenPos)
        {
            Position = GetPosition(screenPos, IndexX, OfX);
        }
        public void CheckClick(Vector2 mouse)
        {
            if (CheckHover(mouse))
                OnClick?.Invoke();
        }
        public bool CheckHover(Vector2 mouse) => Position.Contains(mouse);

        public void OnGUI(Event e) => GUI.DrawTextureWithTexCoords(Position, Texture, Coords);

        private Rect GetPosition(Vector2 centre, int iX, int ofX)
        {
            var sumX = ofX * Size + (ofX - 1) * Padding;
            var x = centre.x - sumX * 0.5f + (iX - 1) * (Size + Padding);
            var y = centre.y - Size - Padding;
            return new Rect(x, y, Size, Size);
        }
    }
}
