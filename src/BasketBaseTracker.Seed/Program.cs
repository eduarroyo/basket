using System.Reflection;
using BasketBaseTracker.Seed;
using BasketBaseTracker.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

const string ConfirmacionRequerida = "BORRAR";

var temporadas = SeedOptions.TemporadasPorDefecto;
var clubes = SeedOptions.ClubesPorDefecto;
var reset = false;
string? confirmar = null;

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--temporadas":
            temporadas = int.Parse(args[++i]);
            break;
        case "--clubes":
            clubes = int.Parse(args[++i]);
            break;
        case "--reset":
            reset = true;
            break;
        case "--confirmar":
            confirmar = args[++i];
            break;
        default:
            Console.Error.WriteLine($"Argumento no reconocido: {args[i]}");
            return 1;
    }
}

// Salvaguarda explícita (spec.md de BAS-17): borrar datos existentes exige
// pasar además "--confirmar BORRAR" en el propio comando, no solo el flag
// --reset — no hay entorno de staging (architecture.md punto 13), así que
// cualquier ejecución con --reset contra una cadena de conexión real está
// borrando la única base de datos que existe fuera de local.
if (reset && confirmar != ConfirmacionRequerida)
{
    Console.Error.WriteLine(
        $"--reset exige además pasar --confirmar {ConfirmacionRequerida} — no se ha borrado ni modificado nada.");
    return 1;
}

var configuracion = new ConfigurationBuilder()
    .AddUserSecrets(Assembly.GetExecutingAssembly())
    .AddEnvironmentVariables()
    .Build();

var cadenaDeConexion = configuracion.GetConnectionString("basketbasetracker");
if (string.IsNullOrWhiteSpace(cadenaDeConexion))
{
    Console.Error.WriteLine(
        "No se ha encontrado la cadena de conexión \"ConnectionStrings:basketbasetracker\" " +
        "(dotnet user-secrets en local, variable de entorno ConnectionStrings__basketbasetracker en CI).");
    return 1;
}

var opciones = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseSqlServer(cadenaDeConexion)
    .Options;

await using var context = new ApplicationDbContext(opciones);

try
{
    await DemoSeeder.SeedAsync(
        context, new SeedOptions(temporadas, clubes, reset), DateOnly.FromDateTime(DateTime.Now), new Random(), CancellationToken.None);
}
catch (InvalidOperationException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

Console.WriteLine($"Datos de demostración generados: {temporadas} temporadas históricas + 1 en curso, {clubes} clubes.");
return 0;
