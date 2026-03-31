using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using Untitled.Sexp;

namespace QuickLook.Plugin.AFH5;

public interface IBoundary
{
    string Type { get; }
    string Name { get; set; }
    string Id { get; set; }
}

public class Fluid : IBoundary
{
    public string Type => "fluid";
    public string Name { get; set; }
    public string Id { get; set; }
    public string Material { get; set; }
    public string Sources { get; set; }
}

public class VelocityInlet : IBoundary
{
    public string Type => "velocity-inlet";
    public string Name { get; set; }
    public string Id { get; set; }
    public string Vmag { get; set; }
    public string T { get; set; }
    public string TurbIntensity { get; set; }
    public string TurbHydraulicDiam { get; set; }
    public string TurbViscosityRatio { get; set; }
}

public class MassFlowInlet : IBoundary
{
    public string Type => "mass-flow-inlet";
    public string Name { get; set; }
    public string Id { get; set; }
    public string MassFlow { get; set; }
    public string T { get; set; }
    public string TurbIntensity { get; set; }
    public string TurbHydraulicDiam { get; set; }
    public string TurbViscosityRatio { get; set; }
}

public class PressureOutlet : IBoundary
{
    public string Type => "pressure-outlet";
    public string Name { get; set; }
    public string Id { get; set; }
    public string P { get; set; }
    public string TurbIntensity { get; set; }
    public string TurbHydraulicDiam { get; set; }
    public string TurbViscosityRatio { get; set; }
}

public class Wall : IBoundary
{
    public string Type => "wall";
    public string Name { get; set; }
    public string Id { get; set; }
    public string Material { get; set; }
    public string T { get; set; }
    public string Q { get; set; }
    public string H { get; set; }
}

public class Interior : IBoundary
{
    public string Type => "interior";
    public string Name { get; set; }
    public string Id { get; set; }
}

public static class BoundaryFactory
{
    public static IBoundary Create(string type)
    {
        return type switch
        {
            "fluid" => new Fluid(),
            "velocity-inlet" => new VelocityInlet(),
            "mass-flow-inlet" => new MassFlowInlet(),
            "pressure-outlet" => new PressureOutlet(),
            "wall" => new Wall(),
            "interior" => new Interior(),
            _ => throw new NotSupportedException($"Unknown boundary type: {type}"),
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

            var boundary = Create(type);
            boundary.Id = id;
            boundary.Name = name;
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
                        value = lst[1].AsPair().Cdr.ToString();
                    }
                    else continue;
                }
                else if (property.IsPair)
                {
                    var pair = property.AsPair();
                    key = pair.Car?.ToString().Replace("-", "").Replace("?", "").ToLower();
                    if (!valid_properties.Contains(key))
                        continue;
                    value = pair.Cdr.ToString();
                }

                var prop = boundary_type.GetProperty(key,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
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
