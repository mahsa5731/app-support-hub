using System.ComponentModel.DataAnnotations;

namespace AppSupportHub.Web.Models.WorkItems;

public sealed class WorkItemFormInput
{
    [Display(Name = "Application system")]
    public Guid ApplicationSystemId { get; set; }

    [Required]
    public string Type { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(4000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string Priority { get; set; } = string.Empty;

    [Display(Name = "Due date and time (UTC)")]
    public string? DueAtUtc { get; set; }
}
