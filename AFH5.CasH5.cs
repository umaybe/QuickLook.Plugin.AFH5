using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using PureHDF;
using Untitled.Sexp;
using Newtonsoft.Json;

namespace QuickLook.Plugin.AFH5;

public class CasH5
{
    public static string Read(string path)
    {
        var File = H5File.OpenRead(path);

        var Settings = File.Group("/settings");

        var Origin = Settings.Dataset("Origin");
        var VersionInfo = Origin.Read<string>();

        var RampantVariables = Settings.Dataset("Rampant Variables");
        var GeneralInfo = RampantVariables.Read<string>();

        var ThreadVariables = Settings.Dataset("Thread Variables");
        var BoundaryInfo = ThreadVariables.Read<string>();

        // Get version info
        string InfoNeed = $"{VersionInfo}\n\n";

        // Get solver info
        string CaseConfig = Regex.Match(GeneralInfo, @"^(\(case-config.*)$", RegexOptions.Multiline).Value;

        string SolverType = Regex.Match(CaseConfig, @"\(rp-seg\?\s+\.\s+([^)\s]+)\)").Value;
        SolverType = SolverType.Contains("#t") ? "pbns" : "dbns";
        InfoNeed += $"Solver Type: {SolverType}\n";

        string SolverTime = Regex.Match(CaseConfig, @"\(rp-unsteady\?\s+\.\s+([^)\s]+)\)").Value;
        SolverTime = SolverTime.Contains("#t") ? "transient" : "steady";
        InfoNeed += $"Solver Time: {SolverTime}\n";

        string SolverDimension = Regex.Match(CaseConfig, @"\(rp-3d\?\s+\.\s+([^)\s]+)\)").Value;
        SolverDimension = SolverDimension.Contains("#t") ? "3d" : "2d";
        InfoNeed += $"Solver Dimension: {SolverDimension}\n";

        string SolverPrecision = Regex.Match(CaseConfig, @"\(rp-double\?\s+\.\s+([^)\s]+)\)").Value;
        SolverPrecision = SolverPrecision.Contains("#t") ? "double" : "single";
        InfoNeed += $"Solver Precision: {SolverPrecision}\n";

        string SolverAxi = Regex.Match(CaseConfig, @"\(rp-axi\?\s+\.\s+([^)\s]+)\)").Value;
        SolverAxi = SolverAxi.Contains("#t") ? "true" : "false";
        InfoNeed += $"Solver Axi: {SolverAxi}\n";

        string SolverInit = Regex.Match(CaseConfig, @"\(hyb-init\?\s+\.\s+([^)\s]+)\)").Value;
        SolverInit = SolverInit.Contains("#t") ? "hybrid" : "standard";
        InfoNeed += $"Solver Init: {SolverInit}\n";

        string SolverTurd = GetActiveViscousModel(CaseConfig);
        InfoNeed += $"Solver Turd: {SolverTurd}\n";

        string SolverEnergy = Regex.Match(CaseConfig, @"\(rf-energy\?\s+\.\s+([^)\s]+)\)").Value;
        SolverEnergy = SolverEnergy.Contains("#t") ? "true" : "false";
        InfoNeed += $"Solver Energy: {SolverEnergy}\n";

        string Gravity = Regex.Match(GeneralInfo, @"\(gravity\?\s+([^)\s]+)\)").Value;
        Gravity = Gravity.Contains("#t") ? "true" : "false";
        InfoNeed += $"Gravity: {Gravity}\n";

        if (Gravity == "true")
        {
            var result = GetGravityValue(GeneralInfo);
            string GravityValue = JsonConvert.SerializeObject(result, JsonOptions);
            InfoNeed += $"Gravity Value: {GravityValue}\n";
        }

        InfoNeed += "\n";

        // Get materials info
        string Materials = Regex.Match(GeneralInfo, @"^(\(materials.*)$", RegexOptions.Multiline).Value;
        var MaterialsConstants = ExtractMaterialConstants(Materials);
        Materials = JsonConvert.SerializeObject(MaterialsConstants, JsonOptions);
        InfoNeed += $"{Materials}\n\n";

        // Get cell zone & boundary condition info
        var boundaryProperties = BoundaryFactory.ExtractBoundaryInfo(BoundaryInfo);
        foreach (var zone in boundaryProperties)
        {
            var key = zone.Key;
            InfoNeed += $"{key}:\n";
            foreach (var boundary in zone.Value)
            {
                InfoNeed += boundary.ToFormattedString();
                InfoNeed += "\n";
            }
        }
        InfoNeed += "\n";

        // Get discretization scheme info
        string FlowSchemeIndex = Regex.Match(GeneralInfo, @"\(flow/scheme\s+(\d+)\)").Groups[1].Value;
        string FlowScheme = SCHEME_ENUM.GetValueOrDefault(FlowSchemeIndex, "");
        InfoNeed += $"Flow Scheme: {FlowScheme}\n";

        string PressureSchemeIndex = Regex.Match(GeneralInfo, @"\(pressure/scheme\s+(\d+)\)").Groups[1].Value;
        string PressureScheme = SCHEME_ENUM.GetValueOrDefault(PressureSchemeIndex, "");
        InfoNeed += $"Pressure Scheme: {PressureScheme}\n";

        string MomSchemeIndex = Regex.Match(GeneralInfo, @"\(mom/scheme\s+(\d+)\)").Groups[1].Value;
        string MomScheme = SCHEME_ENUM.GetValueOrDefault(MomSchemeIndex, "");
        InfoNeed += $"Mom Scheme: {MomScheme}\n";

        string TemperatureSchemeIndex = Regex.Match(GeneralInfo, @"\(temperature/scheme\s+(\d+)\)").Groups[1].Value;
        string TemperatureScheme = SCHEME_ENUM.GetValueOrDefault(TemperatureSchemeIndex, "");
        InfoNeed += $"Temperature Scheme: {TemperatureScheme}\n";

        if (SolverTurd == "k-epsilon")
        {
            string KSchemeIndex = Regex.Match(GeneralInfo, @"\(k/scheme\s+(\d+)\)").Groups[1].Value;
            string KScheme = SCHEME_ENUM.GetValueOrDefault(KSchemeIndex, "");
            InfoNeed += $"K Scheme: {KScheme}\n";
            string EpsilonSchemeIndex = Regex.Match(GeneralInfo, @"\(epsilon/scheme\s+(\d+)\)").Groups[1].Value;
            string EpsilonScheme = SCHEME_ENUM.GetValueOrDefault(EpsilonSchemeIndex, "");
            InfoNeed += $"Epsilon Scheme: {EpsilonScheme}\n";
        }
        else if (SolverTurd == "k-omega")
        {
            string KSchemeIndex = Regex.Match(GeneralInfo, @"\(k/scheme\s+(\d+)\)").Groups[1].Value;
            string KScheme = SCHEME_ENUM.GetValueOrDefault(KSchemeIndex, "");
            InfoNeed += $"K Scheme: {KScheme}\n";
            string OmegaSchemeIndex = Regex.Match(GeneralInfo, @"\(omega/scheme\s+(\d+)\)").Groups[1].Value;
            string OmegaScheme = SCHEME_ENUM.GetValueOrDefault(OmegaSchemeIndex, "");
            InfoNeed += $"Omega Scheme: {OmegaScheme}\n";
        }
        InfoNeed += "\n";

        // Get under-relaxation factor info
        string PressureRelax = Regex.Match(GeneralInfo, @"\(pressure/relax\s+([\d.]+)\)").Groups[1].Value;
        InfoNeed += $"Under-Relaxation Factor for pressure: {PressureRelax}\n";

        string MomRelax = Regex.Match(GeneralInfo, @"\(mom/relax\s+([\d.]+)\)").Groups[1].Value;
        InfoNeed += $"Under-Relaxation Factor for momentum: {MomRelax}\n";

        string TempRelax = Regex.Match(GeneralInfo, @"\(temperature/relax\s+([\d.]+)\)").Groups[1].Value;
        InfoNeed += $"Under-Relaxation Factor for temperature: {TempRelax}\n";

        if (SolverTurd == "k-epsilon")
        {
            string KRelax = Regex.Match(GeneralInfo, @"\(k/relax\s+([\d.]+)\)").Groups[1].Value;
            InfoNeed += $"Under-Relaxation Factor for k: {KRelax}\n";
            string EpsilonRelax = Regex.Match(GeneralInfo, @"\(epsilon/relax\s+([\d.]+)\)").Groups[1].Value;
            InfoNeed += $"Under-Relaxation Factor for epsilon: {EpsilonRelax}\n";
        }
        else if (SolverTurd == "k-omega")
        {
            string KRelax = Regex.Match(GeneralInfo, @"\(k/relax\s+([\d.]+)\)").Groups[1].Value;
            InfoNeed += $"Under-Relaxation Factor for k: {KRelax}\n";
            string OmegaRelax = Regex.Match(GeneralInfo, @"\(omega/relax\s+([\d.]+)\)").Groups[1].Value;
            InfoNeed += $"Under-Relaxation Factor for omega: {OmegaRelax}\n";
        }
        InfoNeed += "\n";

        // Get residuals info
        // string ResidualsSettings = Regex.Match(GeneralInfo, @"^(\(residuals/settings .*)$", RegexOptions.Multiline).Value;
        // TODO: residuals settings

        // Get iteration info
        if (SolverTime == "steady")
        {
            string NumberOfIterations = Regex.Match(GeneralInfo, @"\(number-of-iterations\s+(\d+)\)").Groups[1].Value;
            InfoNeed += $"Number of Iterations: {NumberOfIterations}\n";
        }
        else
        {
            string PhysicalTimeStepSel = Regex.Match(GeneralInfo, @"\(physical-time-step-sel\s+""([^""]+)""\)").Groups[1].Value;
            string PhysicalTimeStepExpr = Regex.Match(GeneralInfo, @"\(physical-time-step-expr\s+""([^""]+)""\)").Groups[1].Value;
            InfoNeed += $"Physical Time Step: {PhysicalTimeStepSel} {PhysicalTimeStepExpr}\n";
            string NumberOfTimeSteps = Regex.Match(GeneralInfo, @"\(number-of-time-steps\s+(\d+)\)").Groups[1].Value;
            InfoNeed += $"Number of Time Steps: {NumberOfTimeSteps}\n";
            string MaxIterationsPerStep = Regex.Match(GeneralInfo, @"\(max-iterations-per-step\s+(\d+)\)").Groups[1].Value;
            InfoNeed += $"Max Iterations Per Step: {MaxIterationsPerStep}\n";
            string TimeStep = Regex.Match(GeneralInfo, @"\(time-step\s+(\d+)\)").Groups[1].Value;
            InfoNeed += $"Time Step: {TimeStep}\n";
            string FlowTime = Regex.Match(GeneralInfo, @"\(flow-time\s+(\d+)\)").Groups[1].Value;
            InfoNeed += $"Flow Time: {FlowTime}\n";
        }

        return InfoNeed;
    }

    private static string GetActiveViscousModel(string CaseConfig)
    {
        var cfg = Regex
            .Matches(CaseConfig, @"\(([^()\s]+)\s+\.\s+([^()\s]+)\)")
            .Cast<Match>()
            .ToDictionary(
                m => m.Groups[1].Value,
                m => m.Groups[2].Value
            );

        if (cfg.GetValueOrDefault("rp-visc") == "#f" || cfg.GetValueOrDefault("rp-inviscid") == "#t")
            return "Inviscid";

        if (cfg.GetValueOrDefault("rp-lam?") == "#t")
            return "Laminar";

        var models = new Dictionary<string, string>
        {
            ["rp-ke?"] = "k-epsilon",
            ["rp-kw?"] = "k-omega",
            ["rp-sa?"] = "Spalart-Allmaras",
            ["sg-rsm?"] = "Reynolds Stress (RSM)",
            ["rp-les?"] = "Large Eddy Simulation (LES)",
            ["rp-des?"] = "Detached Eddy Simulation (DES)",
            ["rp-kklw?"] = "k-kl-omega Transition",
            ["rp-v2f?"] = "V2F"
        };

        return models.FirstOrDefault(m => cfg.GetValueOrDefault(m.Key) == "#t").Value ?? "Unknown";
    }

    private static Dictionary<string, Tuple<string, string>> GetGravityValue(string GeneralInfo)
    {
        var result = new Dictionary<string, Tuple<string, string>>();
        string[] axes = ["x", "y", "z"];

        foreach (string axis in axes)
        {
            var selMatch = Regex.Match(GeneralInfo, $@"\(gravity/{axis}-sel\s+""([^""]+)""\)");
            var exprMatch = Regex.Match(GeneralInfo, $@"\(gravity/{axis}-expr\s+""([^""]+)""\)");
            result[axis] = new Tuple<string, string>(
                selMatch.Groups[1].Value,
                exprMatch.Groups[1].Value
            );
        }

        return result;
    }

    static readonly JsonSerializerSettings JsonOptions = new() { Formatting = Formatting.Indented };

    private static Dictionary<string, Dictionary<string, double>> ExtractMaterialConstants(string materialsStr)
    {
        var results = new Dictionary<string, Dictionary<string, double>>();
        var parsed = Sexp.Parse(materialsStr);

        if (!parsed.IsList) return results;

        var parsedList = parsed.ToList();
        if (parsedList.Count < 2) return results;

        var materialsList = parsedList[1];
        if (!materialsList.IsList) return results;

        foreach (var mat in materialsList.AsEnumerable())
        {
            if (!mat.IsList) continue;

            var matItems = mat.ToList();
            if (matItems.Count < 3) continue;

            // matItems[0] = name (Symbol), matItems[1] = type, matItems[2:] = property_list
            if (!matItems[0].IsSymbol) continue;
            var matName = matItems[0].AsSymbol().Name;

            results[matName] = [];

            for (int i = 2; i < matItems.Count; i++)
            {
                var prop = matItems[i];
                if (!prop.IsList) continue;

                var propItems = prop.ToList();
                if (propItems.Count < 2) continue;

                if (!propItems[0].IsSymbol) continue;
                var propName = propItems[0].AsSymbol().Name;

                for (int j = 1; j < propItems.Count; j++)
                {
                    var detail = propItems[j];
                    if (detail.IsPair && !detail.IsList)
                    {
                        var pair = detail.AsPair();
                        if (pair.Car.IsSymbol && pair.Car.AsSymbol().Name == "constant")
                        {
                            results[matName][propName] = ConvertValue(pair.Cdr);
                            break;
                        }
                    }
                }
            }
        }

        return results;
    }

    private static double ConvertValue(SValue value)
    {
        if (value.IsNumber) return value.AsDouble();
        if (value.IsBoolean) return (bool)value ? 1.0 : 0.0;
        throw new InvalidCastException($"Cannot convert SValue of type {value.Type} to double");
    }

    // From context.scm
    private static readonly Dictionary<string, string> SCHEME_ENUM = new()
    {
        ["0"] = "First Order Upwind",
        ["1"] = "Second Order Upwind",
        ["2"] = "Power Law",
        ["3"] = "Central Difference",
        ["4"] = "Quick",
        ["5"] = "Modified HRIC",
        ["6"] = "Third-Order MUSCL",
        ["7"] = "Bounded Central Differencing",
        ["8"] = "CICSAM",
        ["9"] = "Low Diffusion Second Order",
        ["10"] = "Standard",
        ["11"] = "Linear",
        ["12"] = "Second Order",
        ["13"] = "Body Force Weighted",
        ["14"] = "PRESTO!",
        ["15"] = "Continuity Based",
        ["16"] = "Geo-Reconstruct",
        ["17"] = "Donor-Acceptor",
        ["18"] = "Modified Body Force Weighted",
        ["20"] = "SIMPLE",
        ["21"] = "SIMPLEC",
        ["22"] = "PISO",
        ["23"] = "Phase Coupled SIMPLE",
        ["24"] = "Coupled",
        ["25"] = "Fractional Step",
        ["28"] = "Compressive",
        ["29"] = "BGM",
        ["30"] = "Phase Coupled PISO",
        ["31"] = "Low Diffusion Central"
    };

    private static string FormatValue(object value)
    {
        if (value == null) return "null";

        if (value is Dictionary<string, object> dict)
        {
            if (dict.Count == 2 && dict.ContainsKey("type") && dict.ContainsKey("value"))
            {
                return $"{dict["type"]} = {FormatValue(dict["value"])}";
            }
            var parts = dict.Select(kv => $"{kv.Key}: {FormatValue(kv.Value)}");
            return "{" + string.Join(", ", parts) + "}";
        }

        if (value is List<object> list)
        {
            return "[" + string.Join(", ", list.Select(FormatValue)) + "]";
        }

        if (value is bool b) return b ? "true" : "false";
        if (value is double d) return d.ToString("0.######");
        if (value is string s) return string.IsNullOrEmpty(s) ? "\"\"" : s;

        return value.ToString() ?? "null";
    }
}
