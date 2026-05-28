namespace RevisorFRX.Core.Models;

public class RuleDefinition
{
    public string Code { get; init; } = "";
    public string Description { get; init; } = "";
    public Severity DefaultSeverity { get; init; }
    public bool DefaultEnabled { get; init; } = true;
}
