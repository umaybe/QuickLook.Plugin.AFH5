using System;
using System.Collections.Generic;
using System.Windows.Media.Media3D;

using PureHDF;
using PureHDF.VOL.Native;

namespace QuickLook.Plugin.AFH5;

public class MeshData
{
    public required ulong Dimension;
    public required Point3DCollection Nodes;
    public required List<uint> Connections;
}

public class MshH5
{
    public static MeshData Read(string mshh5_path)
    {
        using var file = H5File.OpenRead(mshh5_path);
        var dimension = file.Dataset("/meshes/1/nodes/zoneTopology/dimension").Read<ulong[]>()[0];
        var data = new MeshData
        {
            Dimension = dimension,
            Nodes = GetCoords(file, dimension),
            Connections = GetConnections(file),
        };
        data.Nodes.Freeze();
        return data;
    }

    private static Point3DCollection GetCoords(NativeFile file, ulong dimension)
    {
        var coords_group = file.Group("/meshes/1/nodes/coords");
        var coords_array = new Point3DCollection();
        foreach (var child in coords_group.Children())
        {
            if (child is IH5Dataset child_dataset)
            {
                var coords_dataset = child_dataset.Read<double[]>();
                for (int i = 0; i < coords_dataset.Length; i++)
                {
                    coords_array.Add(
                        new Point3D(
                            coords_dataset[i],
                            coords_dataset[++i],
                            dimension == 3 ? coords_dataset[++i] : 0.0
                        )
                    );
                }
            }
        }
        return coords_array;
    }

    private static List<uint> GetConnections(NativeFile file)
    {
        var faces_nodes_group = file.Group("/meshes/1/faces/nodes");
        var connections = new List<uint>();

        foreach (var child in faces_nodes_group.Children())
        {
            if (child is IH5Group child_group)
            {
                var nodes_index = 0;
                var nnodes = child_group.Dataset("nnodes").Read<ushort[]>();
                var nodes = child_group.Dataset("nodes").Read<uint[]>();

                ReadOnlySpan<uint> nodes_span = nodes.AsSpan();
                foreach (var nnode in nnodes)
                {
                    ReadOnlySpan<uint> face_nodes_original = nodes_span.Slice(nodes_index, nnode);
                    if (nnode > 2)
                    {
                        var face_nodes = ExpandArray(face_nodes_original);
                        foreach (var node in face_nodes)
                        {
                            connections.Add(node - 1);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < nnode; i++)
                        {
                            connections.Add(face_nodes_original[i] - 1);
                        }
                    }
                    nodes_index += nnode;
                }
            }
        }
        return connections;
    }

    private static uint[] ExpandArray(ReadOnlySpan<uint> original)
    {
        // e.g. [1, 2, 3, 4, 5] -> [1, 2, 2, 3, 3, 4, 4, 5, 5, 1]
        var expanded_array = new uint[original.Length * 2];

        for (var i = 0; i < original.Length - 1; i++)
        {
            expanded_array[i * 2] = original[i];
            expanded_array[i * 2 + 1] = original[i + 1];
        }
        var lastIndex = original.Length - 1;
        expanded_array[lastIndex * 2] = original[lastIndex];
        expanded_array[lastIndex * 2 + 1] = original[0];

        return expanded_array;
    }
}
