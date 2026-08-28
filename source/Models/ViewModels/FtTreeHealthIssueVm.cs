namespace FamilyTree.Models.ViewModels;

public sealed class FtTreeHealthIssueVm
{
    public string IssueType { get; set; } = "";
    public string Severity { get; set; } = "WARN"; // ERROR / WARN
    public string Title { get; set; } = "";
    public string Detail { get; set; } = "";
    public int? PersonId { get; set; }
    public string? PersonName { get; set; }
    public int? RelatedPersonId { get; set; }
    public string? RelatedPersonName { get; set; }
}
