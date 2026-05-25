using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;

namespace MaterialFolderIcons.VisualStudio.Rendering
{
    internal sealed class SvgIconHandleCache : IDisposable
    {
        private const int IconSize = 16;
        private const double IconPadding = 0.00d;
        private const double SvgCoordinateSize = 24d;
        private readonly Dictionary<string, IntPtr> handles = new Dictionary<string, IntPtr>(StringComparer.OrdinalIgnoreCase);

        public IntPtr GetOrCreateIconHandle(string svgPath)
        {
            if (handles.TryGetValue(svgPath, out var handle))
            {
                return handle;
            }

            handle = RenderSvgToIconHandle(svgPath);
            handles.Add(svgPath, handle);
            return handle;
        }

        public void Dispose()
        {
            foreach (var handle in handles.Values)
            {
                if (handle != IntPtr.Zero)
                {
                    DestroyIcon(handle);
                }
            }

            handles.Clear();
        }

        private static IntPtr RenderSvgToIconHandle(string svgPath)
        {
            var drawing = LoadSvgDrawing(svgPath);
            var visual = new DrawingVisual();

            using (var context = visual.RenderOpen())
            {
                context.DrawDrawing(drawing);
            }

            var bitmap = new RenderTargetBitmap(IconSize, IconSize, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);

            using (var stream = new MemoryStream())
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(stream);
                stream.Position = 0;

                using (var drawingBitmap = new Bitmap(stream))
                {
                    return drawingBitmap.GetHicon();
                }
            }
        }

        private static DrawingGroup LoadSvgDrawing(string svgPath)
        {
            var document = XDocument.Load(svgPath);
            var root = document.Root ?? throw new InvalidDataException($"SVG root is missing: {svgPath}");
            var scale = GetViewBoxScale(root);
            var group = new DrawingGroup();

            using (var context = group.Open())
            {
                foreach (var element in EnumerateRenderableElements(root))
                {
                    var fill = ReadFill(element);
                    if (fill == null)
                    {
                        continue;
                    }

                    var geometry = CreateGeometry(element);
                    if (geometry == null)
                    {
                        continue;
                    }

                    geometry.Transform = CombineTransforms(GetSvgTransform(element), scale);

                    context.DrawGeometry(fill, null, geometry);
                }
            }

            return group;
        }

        private static IEnumerable<XElement> EnumerateRenderableElements(XElement root)
        {
            foreach (var element in root.Descendants())
            {
                if (IsRenderableElement(element) && ReadFill(element) != null && CreateGeometry(element) != null)
                {
                    yield return element;
                }
            }
        }

        private static bool IsRenderableElement(XElement element)
        {
            var name = element.Name.LocalName;
            return string.Equals(name, "path", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "rect", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "circle", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "ellipse", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "polygon", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "polyline", StringComparison.OrdinalIgnoreCase);
        }

        private static Geometry? CreateGeometry(XElement element)
        {
            switch (element.Name.LocalName.ToLowerInvariant())
            {
                case "path":
                    var data = (string?)element.Attribute("d");
                    return string.IsNullOrWhiteSpace(data) ? null : Geometry.Parse(data).Clone();

                case "rect":
                    var width = ReadDouble(element, "width");
                    var height = ReadDouble(element, "height");
                    if (width <= 0d || height <= 0d)
                    {
                        return null;
                    }

                    var rx = ReadDouble(element, "rx");
                    var ry = ReadDouble(element, "ry", rx);
                    return new RectangleGeometry(new Rect(
                        ReadDouble(element, "x"),
                        ReadDouble(element, "y"),
                        width,
                        height),
                        rx,
                        ry);

                case "circle":
                    var radius = ReadDouble(element, "r");
                    if (radius <= 0d)
                    {
                        return null;
                    }

                    return new EllipseGeometry(new Point(ReadDouble(element, "cx"), ReadDouble(element, "cy")), radius, radius);

                case "ellipse":
                    var radiusX = ReadDouble(element, "rx");
                    var radiusY = ReadDouble(element, "ry");
                    if (radiusX <= 0d || radiusY <= 0d)
                    {
                        return null;
                    }

                    return new EllipseGeometry(new Point(ReadDouble(element, "cx"), ReadDouble(element, "cy")), radiusX, radiusY);

                case "polygon":
                    return CreatePointGeometry((string?)element.Attribute("points"), true);

                case "polyline":
                    return CreatePointGeometry((string?)element.Attribute("points"), false);

                default:
                    return null;
            }
        }

        private static double ReadDouble(XElement element, string attributeName, double defaultValue = 0d)
        {
            var value = ((string?)element.Attribute(attributeName))?.Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            if (value!.EndsWith("px", StringComparison.OrdinalIgnoreCase))
            {
                value = value.Substring(0, value.Length - 2);
            }

            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : defaultValue;
        }

        private static Geometry? CreatePointGeometry(string? pointsText, bool closed)
        {
            if (string.IsNullOrWhiteSpace(pointsText))
            {
                return null;
            }

            var values = pointsText!
                .Split(new[] { ' ', ',', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(value => double.Parse(value, CultureInfo.InvariantCulture))
                .ToArray();
            if (values.Length < 4)
            {
                return null;
            }

            var points = new List<Point>();
            for (var index = 0; index + 1 < values.Length; index += 2)
            {
                points.Add(new Point(values[index], values[index + 1]));
            }

            var geometry = new StreamGeometry();
            using (var context = geometry.Open())
            {
                context.BeginFigure(points[0], true, closed);
                for (var index = 1; index < points.Count; index++)
                {
                    context.LineTo(points[index], true, false);
                }
            }

            return geometry;
        }

        private static Transform GetViewBoxScale(XElement root)
        {
            var viewBox = ((string?)root.Attribute("viewBox"))?.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (viewBox == null || viewBox.Length != 4)
            {
                var defaultScale = (IconSize - (IconPadding * 2d)) / SvgCoordinateSize;
                return new MatrixTransform(defaultScale, 0d, 0d, defaultScale, IconPadding, IconPadding);
            }

            if (!double.TryParse(viewBox[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var minX) ||
                !double.TryParse(viewBox[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var minY) ||
                !double.TryParse(viewBox[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var width) ||
                !double.TryParse(viewBox[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var height) ||
                width <= 0 ||
                height <= 0)
            {
                var defaultScale = (IconSize - (IconPadding * 2d)) / SvgCoordinateSize;
                return new MatrixTransform(defaultScale, 0d, 0d, defaultScale, IconPadding, IconPadding);
            }

            var xScale = (IconSize - (IconPadding * 2d)) / width;
            var yScale = (IconSize - (IconPadding * 2d)) / height;
            return new MatrixTransform(xScale, 0d, 0d, yScale, IconPadding - (minX * xScale), IconPadding - (minY * yScale));
        }

        private static Transform CombineTransforms(Matrix svgTransform, Transform viewBoxTransform)
        {
            var matrix = Matrix.Multiply(svgTransform, viewBoxTransform.Value);
            return new MatrixTransform(matrix);
        }

        private static Matrix GetSvgTransform(XElement element)
        {
            var matrix = Matrix.Identity;
            for (var current = element; current != null; current = current.Parent)
            {
                var transform = (string?)current.Attribute("transform");
                if (!string.IsNullOrWhiteSpace(transform))
                {
                    matrix = Matrix.Multiply(matrix, ParseTransform(transform!));
                }
            }

            return matrix;
        }

        private static Matrix ParseTransform(string transform)
        {
            var matrix = Matrix.Identity;
            foreach (Match match in Regex.Matches(transform, @"(matrix|translate|scale)\s*\(([^)]*)\)", RegexOptions.IgnoreCase))
            {
                var values = match.Groups[2].Value
                    .Split(new[] { ' ', ',', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(value => double.Parse(value, CultureInfo.InvariantCulture))
                    .ToArray();

                Matrix next;
                switch (match.Groups[1].Value.ToLowerInvariant())
                {
                    case "matrix" when values.Length >= 6:
                        next = new Matrix(values[0], values[1], values[2], values[3], values[4], values[5]);
                        break;
                    case "translate" when values.Length >= 1:
                        next = new Matrix(1d, 0d, 0d, 1d, values[0], values.Length > 1 ? values[1] : 0d);
                        break;
                    case "scale" when values.Length >= 1:
                        next = new Matrix(values[0], 0d, 0d, values.Length > 1 ? values[1] : values[0], 0d, 0d);
                        break;
                    default:
                        continue;
                }

                matrix = Matrix.Multiply(next, matrix);
            }

            return matrix;
        }

        private static System.Windows.Media.Brush? ReadFill(XElement element)
        {
            var fill = (string?)element.Attribute("fill");
            if (string.IsNullOrWhiteSpace(fill))
            {
                fill = ReadStyleValue(element, "fill");
            }

            if (string.IsNullOrWhiteSpace(fill))
            {
                fill = element.Ancestors()
                    .Select(ancestor => (string?)ancestor.Attribute("fill") ?? ReadStyleValue(ancestor, "fill"))
                    .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
            }

            if (string.IsNullOrWhiteSpace(fill) || string.Equals(fill, "none", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (!fill!.StartsWith("#", StringComparison.Ordinal))
            {
                return null;
            }

            var color = (Color)System.Windows.Media.ColorConverter.ConvertFromString(fill);
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        private static string? ReadStyleValue(XElement element, string propertyName)
        {
            var style = (string?)element.Attribute("style");
            if (string.IsNullOrWhiteSpace(style))
            {
                return null;
            }

            foreach (var declaration in style!.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = declaration.Split(new[] { ':' }, 2);
                if (parts.Length == 2 && string.Equals(parts[0].Trim(), propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    return parts[1].Trim();
                }
            }

            return null;
        }

        private static XName SvgName(string localName)
        {
            return XName.Get(localName, "http://www.w3.org/2000/svg");
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);
    }
}
