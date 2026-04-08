using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Media.Media3D;

using HelixToolkit.Wpf;

using QuickLook.Common.Plugin;
using System.Windows.Input;

namespace QuickLook.Plugin.AFH5;

public class Plugin : IViewer
{
    public int Priority => 0;

    public void Init()
    {
    }

    public bool CanHandle(string path)
    {
        return !Directory.Exists(path) && path.ToLower().EndsWith(".cas.h5")
            || !Directory.Exists(path) && path.ToLower().EndsWith(".msh.h5");
    }

    public void Prepare(string path, ContextObject context)
    {
        if (path.ToLower().EndsWith(".cas.h5"))
            context.PreferredSize = new Size { Width = 600, Height = 600 };
        else if (path.ToLower().EndsWith(".msh.h5"))
            context.PreferredSize = new Size { Width = 1000, Height = 500 };
    }

    public void View(string path, ContextObject context)
    {
        if (path.ToLower().EndsWith(".cas.h5"))
        {
            string content = CasH5.Read(path);

            var textBox = new TextBox
            {
                Text = content,
                IsReadOnly = true,
                IsReadOnlyCaretVisible = true,
                Padding = new Thickness(10),
                TextWrapping = TextWrapping.Wrap,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 0, 0, -5)
            };

            var scrollViewer = new ScrollViewer
            {
                Content = textBox,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };

            context.ViewerContent = scrollViewer;
        }
        else if (path.ToLower().EndsWith("msh.h5"))
        {
            context.PreferredSize = new Size { Width = 1200, Height = 600 };

            var mesh_data = MshH5.Read(path);

            var view_point = new HelixViewport3D
            {
                ZoomExtentsWhenLoaded = true,
                ShowCoordinateSystem = true,
                Background = Brushes.AliceBlue,
                Orthographic = true,
                IsRotationEnabled = mesh_data.Dimension == 3,
                PanGesture = new MouseGesture(MouseAction.LeftClick),
            };

            if (mesh_data.Dimension == 2)
            {
                view_point.Camera = new OrthographicCamera
                {
                    Position = new Point3D(0, 0, 100),
                    LookDirection = new Vector3D(0, 0, -100),
                    UpDirection = new Vector3D(0, 1, 0),
                    Width = 20
                };
            }

            view_point.Children.Add(new DefaultLights());

            var render_points = new Point3DCollection();
            foreach (int index in mesh_data.Connections)
            {
                if (index >= 0 && index < mesh_data.Nodes.Length)
                {
                    render_points.Add(mesh_data.Nodes[index]);
                }
            }
            render_points.Freeze();

            var wire_frame = new LinesVisual3D
            {
                Color = Colors.Blue,
                Thickness = 0.8,
                Points = render_points,
            };
            view_point.Children.Add(wire_frame);

            context.ViewerContent = view_point;
        }
        context.Title = $"{Path.GetFileName(path)}";
        context.IsBusy = false;
    }

    public void Cleanup()
    {
    }
}
