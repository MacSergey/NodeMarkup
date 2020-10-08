using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI
{
    public class GUIButton
    {
        private float Size => 50;
        private float Padding => 5;

        public event Action OnClick;

        private int Index { get; }
        private int Of { get; }
        private Texture2D Texture { get; }
        private Rect Coords { get; }
        private Rect Position { get; set; }


        public GUIButton(int index, int of, Texture2D texture, Rect coords)
        {
            Index = index;
            Of = of;
            Texture = texture;
            Coords = coords;
        }

        public void Update(Vector2 screenPos)
        {
            Position = GetPosition(screenPos, Index, Of);
        }
        public void CheckClick(Vector2 mouse)
        {
            if (CheckHover(mouse))
                OnClick?.Invoke();
        }
        public bool CheckHover(Vector2 mouse) => Position.Contains(mouse);

        public void OnGUI(Event e) => GUI.DrawTextureWithTexCoords(Position, Texture, Coords);

        private Rect GetPosition(Vector2 centre, int i, int of)
        {
            var sumWidth = of * Size + (of - 1) * Padding;
            return new Rect(centre.x - sumWidth / 2 + (i - 1) * (Size + Padding), centre.y - Size / 2, Size, Size);
        }
    }
}
