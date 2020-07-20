using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public interface IFillerStyle { }
    public abstract class FillerStyle : Style, IFillerStyle
    {
        public static float DefaultAngle { get; } = 0f;
        public static float DefaultStep { get; } = 1f;
        public static float DefaultOffset { get; } = 1f;

        public static StrokeFillerStyle DefaultStroke => new StrokeFillerStyle(DefaultColor, DefaultWidth, DefaultAngle, DefaultStep, DefaultOffset);

        public static FillerStyle GetDefault(FillerType type)
        {
            switch (type)
            {
                case FillerType.Stroke: return DefaultStroke;
                default: return null;
            }
        }
        public FillerStyle(Color32 color, float width) : base(color, width) { }
        public abstract FillerStyle Copy();
        public abstract IEnumerable<MarkupStyleDash> Calculate(MarkupFiller filler);

        public enum FillerType
        {
            [Description("LineStyle_Solid")]
            Stroke = StyleType.FillerStroke,
        }
    }
}
