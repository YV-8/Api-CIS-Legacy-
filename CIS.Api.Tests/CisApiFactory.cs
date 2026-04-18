using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CIS.Api.Tests;

/// <summary>
/// Activa EF InMemory vía configuración (un solo proveedor en el contenedor DI).
/// </summary>
public class CisApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Testing:UseInMemoryDatabase", "true");
    }
}
