using System.ComponentModel.DataAnnotations;

namespace PruebaTecnica_Multitenant.API.DTOs.Auth;

public class SelectOrgRequest
{
    [Required]
    public string SelectionToken { get; set; } = null!;

    [Required]
    public Guid OrganizacionId { get; set; }
}
