using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Web.Data;

// Los reintentos ante fallos transitorios de Azure SQL (EnableRetryOnFailure) se
// configuran en Program.cs, no aquí vía OnConfiguring: AddSqlServerDbContext usa
// un pool de contextos, y EF Core prohíbe sobreescribir OnConfiguring cuando el
// pooling está activo — ver spec.md de BAS-3.
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options);
