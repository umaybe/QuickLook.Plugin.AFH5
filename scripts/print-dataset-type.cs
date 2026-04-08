// A script to print all dataset type info of a .h5 file.
// usage: dotnet print-dataset-type.cs <path to .h5>
// You need .net 10 or later to run this.

#:package PureHDF@1.0.1

using PureHDF;
using PureHDF.VOL.Native;

using var file = H5File.OpenRead(args[0]);
PrintFileStructure(file);

static void PrintFileStructure(NativeFile file, string path = "/")
{
    var group = file.Group(path);
    Console.WriteLine($"path: {path}");
    
    foreach (var child in group.Children())
    {
        if (child is IH5Dataset dataset)
        {
            var dtype = dataset.Type;
            var space = dataset.Space;
            Console.WriteLine($"  📊 {child.Name}: {dtype.Class}, Size={dtype.Size} bytes, Dimensions={string.Join("x", space.Dimensions)}");
        }
        else if (child is IH5Group subGroup)
        {
            PrintFileStructure(file, $"{path}/{child.Name}".TrimStart('/'));
        }
    }
}
