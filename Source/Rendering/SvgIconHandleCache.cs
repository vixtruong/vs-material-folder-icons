using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace MaterialFolderIcons.VisualStudio.Rendering
{
    internal sealed class SvgIconHandleCache : IDisposable
    {
        private const int IconSize = 16;
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
                context.PushTransform(new ScaleTransform(IconSize / 24d, IconSize / 24d));
                context.DrawDrawing(drawing);
                context.Pop();
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
                foreach (var path in root.Descendants(SvgName("path")))
                {
                    var data = (string?)path.Attribute("d");
                    if (string.IsNullOrWhiteSpace(data))
                    {
                        continue;
                    }

                    var fill = ReadFill(path);
                    if (fill == null)
                    {
                        continue;
                    }

                    var geometry = Geometry.Parse(data);
                    if (scale != null)
                    {
                        geometry.Transform = scale;
                    }

                    context.DrawGeometry(fill, null, geometry);
                }
            }

            return group;
        }

        private static Transform? GetViewBoxScale(XElement root)
        {
            var viewBox = ((string?)root.Attribute("viewBox"))?.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (viewBox == null || viewBox.Length != 4)
            {
                return null;
            }

            if (!double.TryParse(viewBox[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var width) ||
                !double.TryParse(viewBox[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var height) ||
                width <= 0 ||
                height <= 0)
            {
                return null;
            }

            return new ScaleTransform(24d / width, 24d / height);
        }

        private static System.Windows.Media.Brush? ReadFill(XElement path)
        {
            var fill = (string?)path.Attribute("fill");
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

        private static XName SvgName(string localName)
        {
            return XName.Get(localName, "http://www.w3.org/2000/svg");
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);
    }
}
