namespace RevisorFRX.Core.Models;

public enum Severity { Error, Warning, Info }

public class RuleResult
{
    public string RuleCode { get; set; } = "";
    public Severity Severity { get; set; }
    public string ComponentName { get; set; } = "";
    public string Message { get; set; } = "";
    public string Detail { get; set; } = "";
    public string FileName { get; set; } = "";
}
