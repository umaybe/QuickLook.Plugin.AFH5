// A script to extract the node coords of a msh.h5 file to file.
// usage: dotnet extract-coords.cs <path to msh.h5>
// You need .net 10 or later to run this.

#:package PureHDF@1.0.1

using System.IO;
using PureHDF;

using var file = H5File.OpenRead(args[0]);

var dimension = file.Dataset("/meshes/1/nodes/zoneTopology/dimension").Read<long>();
var coords = file.Dataset("/meshes/1/nodes/coords/1").Read<double[]>();
var node_count = coords.Length / dimension;

using var writer = new StreamWriter("coords.txt");

for (int i = 0; i < node_count; i++)
{
    var base_index = i * dimension;

    if (dimension == 2)
    {
        var x = coords[base_index];
        var y = coords[base_index + 1];
        writer.WriteLine($"{x} {y}");
    }
    else if (dimension == 3)
    {
        var x = coords[base_index];
        var y = coords[base_index + 1];
        var z = coords[base_index + 2];
        writer.WriteLine($"{x} {y} {z}");
    }
}
