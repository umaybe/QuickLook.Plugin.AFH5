using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;

using QuickLook.Common.Plugin;

namespace QuickLook.Plugin.AFH5;

public class Plugin : IViewer
{
    public int Priority => 0;

    public void Init()
    {
    }

    public bool CanHandle(string path)
    {
        return !Directory.Exists(path) && path.ToLower().EndsWith(".cas.h5");
    }

    public void Prepare(string path, ContextObject context)
    {
        context.PreferredSize = new Size { Width = 600, Height = 600 };
    }

    public void View(string path, ContextObject context)
    {
        string content = CasH5.Read(path);

        var textBox = new TextBox
        {
            Text = content,
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(10),
            FontFamily = new FontFamily("Consolas")
        };

        var scrollViewer = new ScrollViewer
        {
            Content = textBox,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(5)
        };

        context.ViewerContent = scrollViewer;
        context.Title = $"{Path.GetFileName(path)}";
        context.IsBusy = false;
    }

    public void Cleanup()
    {
    }
}
