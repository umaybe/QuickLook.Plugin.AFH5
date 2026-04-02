// A script to test Untitled.Sexp.
// usage: dotnet test-sexp.cs
// You need .net 10 or later to run this.

#:package Untitled.Sexp@0.1.2

using Untitled.Sexp;

var test_string = """
(39 (542 solid hv_coil 1)(
 (material . hv)
 (sources? . #t)
 (source-terms (energy ((profile "udf" "energy_source_hv") (constant . 0) (inactive . #f))))
 (fixed? . #f)
 (cylindrical-fixed-var? . #f)
 (fixes)
 (motion-spec . 0)
 (relative-to-thread . -1)
 (omega . 0)
 (grid-x-vel . 0)
 (grid-y-vel . 0)
 (grid-z-vel . 0)
 (x-origin . 0.)
 (y-origin . 0.)
 (z-origin . 0.)
 (axis-origin-component 0. 0. 0.)
 (ai . 0)
 (aj . 0)
 (ak . 1)
 (axis-direction-component 0 0 1)
 (udf-zmotion-name . "none")
 (mrf-motion? . #f)
 (mrf-relative-to-thread . -1)
 (mrf-omega (constant . 0) (profile "" ""))
 (mrf-grid-x-vel (constant . 0) (profile "" ""))
 (mrf-grid-y-vel (constant . 0) (profile "" ""))
 (mrf-grid-z-vel (constant . 0) (profile "" ""))
 (reference-frame-velocity-components ((constant . 0) (profile "" "")) ((constant . 0) (profile "" "")) ((constant . 0) (profile "" "")))
 (mrf-x-origin (constant . 0.) (profile "" ""))
 (mrf-y-origin (constant . 0.) (profile "" ""))
 (mrf-z-origin (constant . 0.) (profile "" ""))
 (reference-frame-axis-origin-components ((constant . 0.) (profile "" "")) ((constant . 0.) (profile "" "")) ((constant . 0.) (profile "" "")))
 (mrf-ai (constant . 0) (profile "" ""))
 (mrf-aj (constant . 0) (profile "" ""))
 (mrf-ak (constant . 1) (profile "" ""))
 (reference-frame-axis-direction-components ((constant . 0) (profile "" "")) ((constant . 0) (profile "" "")) ((constant . 1) (profile "" "")))
 (mrf-udf-zmotion-name . "none")
 (mgrid-enable-transient? . #f)
 (mgrid-motion? . #f)
 (mgrid-relative-to-thread . -1)
 (mgrid-omega (constant . 0) (profile "" ""))
 (mgrid-grid-x-vel (constant . 0) (profile "" ""))
 (mgrid-grid-y-vel (constant . 0) (profile "" ""))
 (mgrid-grid-z-vel (constant . 0) (profile "" ""))
 (moving-mesh-velocity-components ((constant . 0) (profile "" "")) ((constant . 0) (profile "" "")) ((constant . 0) (profile "" "")))
 (mgrid-x-origin (constant . 0) (profile "" ""))
 (mgrid-y-origin (constant . 0) (profile "" ""))
 (mgrid-z-origin (constant . 0) (profile "" ""))
 (moving-mesh-axis-origin-components ((constant . 0) (profile "" "")) ((constant . 0) (profile "" "")) ((constant . 0) (profile "" "")))
 (mgrid-ai (constant . 0) (profile "" ""))
 (mgrid-aj (constant . 0) (profile "" ""))
 (mgrid-ak (constant . 1) (profile "" ""))
 (moving-mesh-axis-direction-components ((constant . 0) (profile "" "")) ((constant . 0) (profile "" "")) ((constant . 1) (profile "" "")))
 (mgrid-udf-zmotion-name . "none")
 (solid-motion? . #f)
 (solid-relative-to-thread . -1)
 (solid-omega (constant . 0) (profile "" ""))
 (solid-grid-x-vel (constant . 0) (profile "" ""))
 (solid-grid-y-vel (constant . 0) (profile "" ""))
 (solid-grid-z-vel (constant . 0) (profile "" ""))
 (solid-motion-velocity-components ((constant . 0) (profile "" "")) ((constant . 0) (profile "" "")) ((constant . 0) (profile "" "")))
 (solid-x-origin (constant . 0) (profile "" ""))
 (solid-y-origin (constant . 0) (profile "" ""))
 (solid-z-origin (constant . 0) (profile "" ""))
 (solid-motion-axis-origin-components ((constant . 0) (profile "" "")) ((constant . 0) (profile "" "")) ((constant . 0) (profile "" "")))
 (solid-ai (constant . 0) (profile "" ""))
 (solid-aj (constant . 0) (profile "" ""))
 (solid-ak (constant . 1) (profile "" ""))
 (solid-motion-axis-direction-components ((constant . 0) (profile "" "")) ((constant . 0) (profile "" "")) ((constant . 1) (profile "" "")))
 (solid-udf-zmotion-name . "none")
 (radiating? . #t)
 (deactivated? . #f)
 (les-embedded? . #f)
 (contact-property? . #f)
 (active-wetsteam-zone? . #t)
 (vapor-phase-realgas . -1)
 (cursys? . #f)
 (cursys-name . "From Material")
 (pcb-model? . #f)
 (pcb-zone-info (ecad-name . "") (choice . "By Count") (rows . 0) (columns . 0) (ref-frame . "global") (pwr-names ()))
))
""";

var values = Sexp.Parse(test_string);

var zone_data = values.AsPair().Cdr;
var properties = zone_data.AsPair().Cdr.AsPair().Car;

var valid_properties = new List<string>() { "sources", "sourceterms" };
foreach (var property in properties.AsEnumerable())
{
    string? key = null;
    string? value = null;

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
                var source_type = lst[1].AsPair().Car.ToString();
                var cdr_value = lst[1].AsPair().Cdr;
                if (cdr_value == SValue.Null) continue;
                Console.WriteLine(cdr_value.ToString());
                var source_value = cdr_value.AsPair().Car.ToList();
                Console.WriteLine(source_value.Count);
                var source_profile = source_value[0].ToList();
                var profile_type = source_profile[1].AsString();
                var profile_name = source_profile[2].AsString();
                value = $"{source_type}-{profile_type}-{profile_name}";
            }
            else
            {
                value = lst[1].AsPair().Cdr.ToString();
            }
            Console.WriteLine("list");
            Console.WriteLine($"{key}: {value}");
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
        Console.WriteLine("pair");
        Console.WriteLine($"{key}: {value}");
    }
}
