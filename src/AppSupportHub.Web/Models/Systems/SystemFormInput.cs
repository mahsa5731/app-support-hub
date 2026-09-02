using System.ComponentModel.DataAnnotations;

namespace AppSupportHub.Web.Models.Systems;

public sealed class SystemFormInput
{
    [Required]
    [StringLength(160)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(4000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string Type { get; set; } = string.Empty;

    [Required]
    public string Criticality { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Business owner")]
    [StringLength(200)]
    public string BusinessOwner { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Technical owner")]
    [StringLength(200)]
    public string TechnicalOwner { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Support team")]
    [StringLength(200)]
    public string SupportTeam { get; set; } = string.Empty;

    [Display(Name = "Vendor name")]
    [StringLength(200)]
    public string? VendorName { get; set; }
}
