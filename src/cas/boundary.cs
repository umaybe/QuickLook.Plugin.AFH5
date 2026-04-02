using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using Untitled.Sexp;

namespace QuickLook.Plugin.AFH5;

public interface IBoundary
{
    string Type { get; }
    string Name { get; init; }
    string Id { get; init; }
}

public class Fluid : IBoundary
{
    public string Type => "fluid";
    public string Name { get; init; }
    public string Id { get; init; }
    public string Material { get; set; }
    public string Sources { get; set; }
    public string SourceTerms { get; set; }
}

public class Solid : IBoundary
{
    public string Type => "solid";
    public string Name { get; init; }
    public string Id { get; init; }
    public string Material { get; set; }
    public string Sources { get; set; }
    public string SourceTerms { get; set; }
}

public class VelocityInlet : IBoundary
{
    public string Type => "velocity-inlet";
    public string Name { get; init; }
    public string Id { get; init; }
    public string Vmag { get; set; }
    public string T { get; set; }
    public string TurbIntensity { get; set; }
    public string TurbHydraulicDiam { get; set; }
    public string TurbViscosityRatio { get; set; }
}

public class MassFlowInlet : IBoundary
{
    public string Type => "mass-flow-inlet";
    public string Name { get; init; }
    public string Id { get; init; }
    public string MassFlow { get; set; }
    public string T { get; set; }
    public string TurbIntensity { get; set; }
    public string TurbHydraulicDiam { get; set; }
    public string TurbViscosityRatio { get; set; }
}

public class PressureOutlet : IBoundary
{
    public string Type => "pressure-outlet";
    public string Name { get; init; }
    public string Id { get; init; }
    public string P { get; set; }
    public string TurbIntensity { get; set; }
    public string TurbHydraulicDiam { get; set; }
    public string TurbViscosityRatio { get; set; }
}

public class Wall : IBoundary
{
    public string Type => "wall";
    public string Name { get; init; }
    public string Id { get; init; }
    public string Material { get; set; }
    public string T { get; set; }
    public string Q { get; set; }
    public string H { get; set; }
}

public class Interior : IBoundary
{
    public string Type => "interior";
    public string Name { get; init; }
    public string Id { get; init; }
}

public class Axis : IBoundary
{
    public string Type => "axis";
    public string Name { get; init; }
    public string Id { get; init; }
}

public static class BoundaryFactory
{
    public static IBoundary Create(string type, string name, string id)
    {
        return type switch
        {
            "fluid" => new Fluid { Name = name, Id = id },
            "solid" => new Solid { Name = name, Id = id },
            "velocity-inlet" => new VelocityInlet { Name = name, Id = id },
            "mass-flow-inlet" => new MassFlowInlet { Name = name, Id = id },
            "pressure-outlet" => new PressureOutlet { Name = name, Id = id },
            "wall" => new Wall { Name = name, Id = id },
            "interior" => new Interior { Name = name, Id = id },
            "axis" => new Axis { Name = name, Id = id },
            _ => throw new NotSupportedException($"Not supported boundary type: {type}"),
        };
    }

    public static Dictionary<string, List<IBoundary>> ExtractBoundaryInfo(string schemeString)
    {
        var result = new Dictionary<string, List<IBoundary>>();

        var values = Sexp.ParseAll(schemeString);

        foreach (var zone in values)
        {
            var zone_data = zone.AsPair().Cdr;

            var metadata = zone_data.AsPair().Car;
            var properties = zone_data.AsPair().Cdr.AsPair().Car;

            var meta_list = metadata.ToList();
            var id = meta_list[0].AsInt().ToString();
            var type = meta_list[1].AsSymbol().Name;
            var name = meta_list[2].AsSymbol().Name;

            var boundary = Create(type, name, id);
            var boundary_type = boundary.GetType();
            var valid_properties = boundary_type.GetProperties()
                .Select(p => p.Name.ToLower())
                .ToList();

            foreach (var property in properties.AsEnumerable())
            {
                string key = null;
                string value = null;

                if (property.IsList)
                {
                    var lst = property.ToList();
                    if (lst.Count >= 2)
                    {
                        key = lst[0].AsSymbol().Name.Replace("-", "").Replace("?", "").ToLower();
                        if (!valid_properties.Contains(key))
                            continue;
                        if (key == "sourceterms")
                        {
                            // e.g. (source-terms (energy ((profile "udf" "energy_source_hv") (constant . 0) (inactive . #f))))
                            var source_type = lst[1].AsPair().Car.ToString();  // energy
                            var cdr_value = lst[1].AsPair().Cdr;
                            if (cdr_value == SValue.Null) continue;
                            var source_value = cdr_value.AsPair().Cdr.ToList();
                            var source_profile = source_value[0].ToList();
                            var profile_type = source_profile[1].AsString();  // udf
                            var profile_name = source_profile[2].AsString();  // energy_source_hv
                            value = $"{source_type}-{profile_type}-{profile_name}";
                        }
                        else
                        {
                            value = lst[1]?.AsPair().Cdr.ToString();
                        }
                    }
                    else continue;
                }
                else if (property.IsPair)
                {
                    var pair = property.AsPair();
                    key = pair.Car.ToString().Replace("-", "").Replace("?", "").ToLower();
                    if (!valid_properties.Contains(key))
                        continue;
                    value = pair.Cdr.ToString();
                }

                var prop = boundary_type.GetProperty(
                    key,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase
                );
                if (prop is not null && prop.CanWrite)
                    prop.SetValue(boundary, value, null);
            }

            if (!result.TryGetValue(type, out var list))
            {
                list = [];
                result[type] = list;
            }
            list.Add(boundary);
        }
        return result;
    }
}
