// A script to extract the scheme strings of a cas.h5 file to multiple files.
// usage: dotnet h5-to-string.cs <path to cas.h5>
// You need .net 10 or later to run this.

#:package PureHDF@1.0.1

using PureHDF;

using var file = H5File.OpenRead(args[0]);
var settings = file.Group("/settings");
var datasets = new string[]
{
    "Cortex Variables",
    "Domain Variables",
    "Origin",
    "Rampant Variables",
    "Solver",
    "TGrid Variables",
    "Thread Variables",
    "Version"
};

foreach (var dataset in datasets)
{
    var data = settings.Dataset(dataset).Read<string>();
    File.WriteAllText($"{dataset}.scm", data, System.Text.Encoding.UTF8);
}
