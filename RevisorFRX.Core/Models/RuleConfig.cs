namespace RevisorFRX.Core.Models;

public class RuleConfig
{
    public Dictionary<string, bool> EnabledRules { get; set; } = new(StringComparer.Ordinal);

    public bool IsEnabled(string ruleCode)
        => EnabledRules.TryGetValue(ruleCode, out var enabled) && enabled;

    public static RuleConfig AllEnabled()
    {
        var config = new RuleConfig();
        foreach (var rule in RuleRegistry.GetAll())
            config.EnabledRules[rule.Code] = true;
        return config;
    }

    public static RuleConfig DefaultUI()
    {
        var config = AllEnabled();
        foreach (var rule in RuleRegistry.GetAll())
            if (!rule.DefaultEnabled)
                config.EnabledRules[rule.Code] = false;
        return config;
    }

    public void SetAll(bool enabled)
    {
        var keys = EnabledRules.Keys.ToList();
        foreach (var key in keys)
            EnabledRules[key] = enabled;
    }
}
