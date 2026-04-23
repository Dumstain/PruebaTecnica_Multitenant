namespace PruebaTecnica_Multitenant.API.DTOs.Auth;

public class MultiOrgResponse
{
    public string SelectionToken { get; set; } = null!;
    public List<OrganizacionItem> Organizaciones { get; set; } = [];
}

public class OrganizacionItem
{
    public Guid   Id     { get; set; }
    public string Nombre { get; set; } = null!;
}
