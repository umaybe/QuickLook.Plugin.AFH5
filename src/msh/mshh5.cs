using System.Windows.Media;
using System.Windows.Media.Media3D;

using PureHDF;

namespace QuickLook.Plugin.AFH5;

public class MeshData
{
    public required int Dimension;
    public Point3D[] Nodes { get; set; } = [];
    public Int32Collection Connections { get; set; } = [];
}

public class MshH5
{
    public static MeshData Read(string mshh5_path)
    {
        using var file = H5File.OpenRead(mshh5_path);
        var dimension = (int)file.Dataset("/meshes/1/nodes/zoneTopology/dimension").Read<long>();
        var coords = file.Dataset("/meshes/1/nodes/coords/1").Read<double[]>();
        var node_count = coords.Length / dimension;

        var data = new MeshData { Dimension = dimension };
        var nodes_array = new Point3D[node_count];
        for (int i = 0; i < node_count; i++)
        {
            var base_index = i * dimension;
            nodes_array[i] = new Point3D(
                coords[base_index],
                coords[base_index + 1],
                dimension == 3 ? coords[base_index + 2] : 0.0
            );
        }
        data.Nodes = nodes_array;

        var faces_nodes_group = file.Group("/meshes/1/faces/nodes");
        foreach (var child in faces_nodes_group.Children())
        {
            if (child is IH5Group child_group)
            {
                var raw_indices = child_group.Dataset("nodes").Read<int[]>();
                for (int i = 0; i < raw_indices.Length; i++)
                {
                    // Fluent uses 1-based indexing
                    data.Connections.Add(raw_indices[i] - 1);
                }
            }
        }

        data.Connections.Freeze();
        return data;
    }
}
