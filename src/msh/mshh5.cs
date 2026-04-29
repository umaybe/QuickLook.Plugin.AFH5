using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Media.Media3D;

using PureHDF;
using PureHDF.VOL.Native;

namespace QuickLook.Plugin.AFH5;

public class MeshData
{
    public required int Dimension;
    public required Point3DCollection Nodes;
    public required List<uint> Connections;
}

public class MshH5
{
    public static MeshData Read(string mshh5_path)
    {
        using NativeFile file = H5File.OpenRead(mshh5_path);
        IH5Group root_group = file.Group("/meshes/1");
        int dimension = root_group.Attribute("dimension").Read<int>();
        MeshData data = new()
        {
            Dimension = dimension,
            Nodes = GetCoords(file, dimension),
            Connections = GetConnections(file, dimension),
        };
        data.Nodes.Freeze();
        return data;
    }

    private static Point3DCollection GetCoords(NativeFile file, int dimension)
    {
        Point3DCollection coords_array = [];

        IH5Group zoneTopology = file.Group("/meshes/1/nodes/zoneTopology");
        ulong nZones = zoneTopology.Attribute("nZones").Read<ulong>();

        IH5Group coords_group = file.Group("/meshes/1/nodes/coords");
        for (ulong iZone = 1; iZone <= nZones; iZone++)
        {
            IH5Dataset coords_dataset = coords_group.Dataset(iZone.ToString());
            double[] coords = coords_dataset.Read<double[]>();
            for (int i = 0; i < coords.Length; i += dimension)
            {
                coords_array.Add(
                    new Point3D(
                        coords[i],
                        coords[i + 1],
                        dimension == 3 ? coords[i + 2] : 0.0
                    )
                );
            }
        }
        return coords_array;
    }

    private static List<uint> GetConnections(NativeFile file, int dimension)
    {
        List<uint> connections = [];
        IH5Group faces_nodes_group = file.Group("/meshes/1/faces/nodes");
        ulong nSections = faces_nodes_group.Attribute("nSections").Read<ulong>();

        if (dimension == 2)
        {
            for (ulong iSection = 1; iSection <= nSections; iSection++)
            {
                IH5Group section_group = faces_nodes_group.Group(iSection.ToString());
                connections.AddRange(
                    section_group.Dataset("nodes").Read<uint[]>().Select(x => x - 1)
                );
            }
        }
        else if (dimension == 3)
        {
            // for 3d mesh, only get connections of boundary faces
            IH5Group c1_group = file.Group("/meshes/1/faces/c1");
            for (ulong iSection = 1; iSection <= nSections; iSection++)
            {
                uint[] c1 = c1_group.Dataset(iSection.ToString()).Read<uint[]>();
                // if c1 or c0 == 0, it is a boundary face (3d <-> c1, 2d <-> c0)
                if (c1[0] == 0)
                {
                    IH5Group section_group = faces_nodes_group.Group(iSection.ToString());
                    short[] nnodes = section_group.Dataset("nnodes").Read<short[]>();
                    uint[] nodes = section_group.Dataset("nodes").Read<uint[]>();
                    ReadOnlySpan<uint> nodes_span = nodes.AsSpan();
                    int start_index = 0;
                    foreach (var nnode in nnodes)
                    {
                        ReadOnlySpan<uint> face_nodes_original = nodes_span.Slice(start_index, nnode);
                        uint[] face_nodes = ExpandArray(face_nodes_original);
                        connections.AddRange(face_nodes.Select(x => x - 1));
                        start_index += nnode;
                    }
                }
                else continue;
            }
        }
        return connections;
    }

    private static uint[] ExpandArray(ReadOnlySpan<uint> original)
    {
        // e.g. [1, 2, 3, 4, 5] -> [1, 2, 2, 3, 3, 4, 4, 5, 5, 1]
        uint[] expanded_array = new uint[original.Length * 2];

        for (int i = 0; i < original.Length - 1; i++)
        {
            expanded_array[i * 2] = original[i];
            expanded_array[i * 2 + 1] = original[i + 1];
        }
        int lastIndex = original.Length - 1;
        expanded_array[lastIndex * 2] = original[lastIndex];
        expanded_array[lastIndex * 2 + 1] = original[0];

        return expanded_array;
    }
}
