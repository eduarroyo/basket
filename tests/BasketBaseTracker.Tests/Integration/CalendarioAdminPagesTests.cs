using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using static BasketBaseTracker.Tests.Integration.AdminHttpTestHelpers;

namespace BasketBaseTracker.Tests.Integration;

// Cubre el calendario de planificación de BAS-9 (Jornada, Partido) vía HTTP real
// contra el AppHost completo: alta, listado, edición, la restricción única de
// Jornada reflejada en la UI, y el invariante de aplicación "un equipo no puede
// aparecer dos veces en la misma jornada" (data-model.md) que no es expresable
// como constraint de BD.
public class CalendarioAdminPagesTests(AppHostSqlFixture fixture) : IClassFixture<AppHostSqlFixture>
{
    private static async Task<(string CompeticionId, string[] EquipoIds)> CrearCompeticionConEquiposAsync(
        HttpClient client, string sufijo, int numeroDeEquipos, CancellationToken cancellationToken)
    {
        async Task<string> CrearAsync(string pagina, string entidad, Dictionary<string, string> campos)
        {
            var createPage = await client.GetAsync($"/Admin/{pagina}/Create", cancellationToken);
            var token = await GetAntiforgeryTokenAsync(createPage, cancellationToken);
            campos["__RequestVerificationToken"] = token;

            var response = await client.PostAsync($"/Admin/{pagina}/Create", new FormUrlEncodedContent(campos), cancellationToken);
            var html = await response.Content.ReadAsStringAsync(cancellationToken);

            var referencia = campos.First(c => c.Key.EndsWith(".Nombre", StringComparison.Ordinal)).Value;
            return ExtraerId(html, entidad, referencia);
        }

        var temporadaId = await CrearAsync("Temporada", "Temporada", new Dictionary<string, string>
        {
            ["Temporada.Nombre"] = $"2025-2026 ({sufijo})",
            ["Temporada.FechaInicio"] = "2025-09-01",
            ["Temporada.FechaFin"] = "2026-06-30",
            ["Temporada.Estado"] = "0",
        });
        var categoriaId = await CrearAsync("Categoria", "Categoria", new Dictionary<string, string>
        {
            ["Categoria.Nombre"] = $"Cadete ({sufijo})",
            ["Categoria.Orden"] = "1",
        });

        var createCompeticionPage = await client.GetAsync("/Admin/Competicion/Create", cancellationToken);
        var createCompeticionToken = await GetAntiforgeryTokenAsync(createCompeticionPage, cancellationToken);
        var competicionResponse = await client.PostAsync(
            "/Admin/Competicion/Create",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = createCompeticionToken,
                ["Competicion.TemporadaId"] = temporadaId,
                ["Competicion.CategoriaId"] = categoriaId,
                ["Competicion.PuntosVictoria"] = "2",
                ["Competicion.PuntosDerrota"] = "1",
            }),
            cancellationToken);
        var competicionHtml = await competicionResponse.Content.ReadAsStringAsync(cancellationToken);
        var competicionId = ExtraerId(competicionHtml, "Competicion", $"2025-2026 ({sufijo})");

        var equipoIds = new string[numeroDeEquipos];
        for (var i = 0; i < numeroDeEquipos; i++)
        {
            var clubId = await CrearAsync("Club", "Club", new Dictionary<string, string>
            {
                ["Club.Nombre"] = $"CB Prueba {i} ({sufijo})",
                ["Club.Municipio"] = "Sevilla",
                ["Club.FechaAlta"] = "2020-01-01",
            });

            var createEquipoPage = await client.GetAsync("/Admin/Equipo/Create", cancellationToken);
            var createEquipoToken = await GetAntiforgeryTokenAsync(createEquipoPage, cancellationToken);
            var equipoNombre = $"CB Prueba {i} A ({sufijo})";
            var equipoResponse = await client.PostAsync(
                "/Admin/Equipo/Create",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["__RequestVerificationToken"] = createEquipoToken,
                    ["Equipo.CompeticionId"] = competicionId,
                    ["Equipo.ClubId"] = clubId,
                    ["Equipo.Nombre"] = equipoNombre,
                    ["Equipo.Estado"] = "0",
                }),
                cancellationToken);
            var equipoHtml = await equipoResponse.Content.ReadAsStringAsync(cancellationToken);
            equipoIds[i] = ExtraerId(equipoHtml, "Equipo", equipoNombre);
        }

        return (competicionId, equipoIds);
    }

    private static async Task<string> CrearJornadaAsync(HttpClient client, string competicionId, int numero, string etiqueta, CancellationToken cancellationToken)
    {
        var createPage = await client.GetAsync($"/Admin/Jornada/Create/{competicionId}", cancellationToken);
        var token = await GetAntiforgeryTokenAsync(createPage, cancellationToken);

        var response = await client.PostAsync(
            $"/Admin/Jornada/Create/{competicionId}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Jornada.Numero"] = numero.ToString(),
                ["Jornada.Etiqueta"] = etiqueta,
                ["Jornada.CuentaParaClasificacion"] = "true",
            }),
            cancellationToken);
        var html = await response.Content.ReadAsStringAsync(cancellationToken);

        return ExtraerId(html, "Jornada", etiqueta);
    }

    [Fact]
    public async Task JornadaAltaListadoYEdicion()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var (competicionId, _) = await CrearCompeticionConEquiposAsync(client, "jornada-alta", numeroDeEquipos: 0, cancellationToken);
        var jornadaId = await CrearJornadaAsync(client, competicionId, numero: 1, etiqueta: "Jornada 1 (jornada-alta)", cancellationToken);

        var editUrl = $"/Admin/Jornada/Edit/{jornadaId}";
        var editPage = await client.GetAsync(editUrl, cancellationToken);
        var editToken = await GetAntiforgeryTokenAsync(editPage, cancellationToken);

        using var editResponse = await client.PostAsync(
            editUrl,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = editToken,
                ["Jornada.Id"] = jornadaId,
                ["Jornada.CompeticionId"] = competicionId,
                ["Jornada.Numero"] = "2",
                ["Jornada.Etiqueta"] = "Jornada 2 (jornada-alta)",
                ["Jornada.CuentaParaClasificacion"] = "false",
            }),
            cancellationToken);
        var afterEditHtml = await editResponse.Content.ReadAsStringAsync(cancellationToken);

        Assert.Equal($"/Admin/Jornada/{competicionId}", editResponse.RequestMessage?.RequestUri?.AbsolutePath);
        Assert.Contains("Jornada 2 (jornada-alta)", afterEditHtml);
        Assert.Contains("<td>2</td>", afterEditHtml);
    }

    [Fact]
    public async Task JornadaRechazaNumeroDuplicadoEnLaMismaCompeticion()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var (competicionId, _) = await CrearCompeticionConEquiposAsync(client, "jornada-duplicada", numeroDeEquipos: 0, cancellationToken);
        await CrearJornadaAsync(client, competicionId, numero: 1, etiqueta: "Primera (jornada-duplicada)", cancellationToken);

        var createPage = await client.GetAsync($"/Admin/Jornada/Create/{competicionId}", cancellationToken);
        var token = await GetAntiforgeryTokenAsync(createPage, cancellationToken);
        using var segundaRespuesta = await client.PostAsync(
            $"/Admin/Jornada/Create/{competicionId}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Jornada.Numero"] = "1",
                ["Jornada.Etiqueta"] = "Repetida (jornada-duplicada)",
                ["Jornada.CuentaParaClasificacion"] = "true",
            }),
            cancellationToken);
        var segundaHtml = await segundaRespuesta.Content.ReadAsStringAsync(cancellationToken);

        Assert.Equal($"/Admin/Jornada/Create/{competicionId}", segundaRespuesta.RequestMessage?.RequestUri?.AbsolutePath);
        Assert.Contains("Ya existe una jornada con ese n", segundaHtml);
    }

    [Fact]
    public async Task PartidoAltaListadoYEdicion()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var (competicionId, equipoIds) = await CrearCompeticionConEquiposAsync(client, "partido-alta", numeroDeEquipos: 2, cancellationToken);
        var jornadaId = await CrearJornadaAsync(client, competicionId, numero: 1, etiqueta: "Jornada 1 (partido-alta)", cancellationToken);

        var createPage = await client.GetAsync($"/Admin/Partido/Create/{jornadaId}", cancellationToken);
        var createToken = await GetAntiforgeryTokenAsync(createPage, cancellationToken);
        using var createResponse = await client.PostAsync(
            $"/Admin/Partido/Create/{jornadaId}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = createToken,
                ["Partido.EquipoLocalId"] = equipoIds[0],
                ["Partido.EquipoVisitanteId"] = equipoIds[1],
                ["Partido.SedeId"] = "",
                ["Partido.FechaHora"] = "",
            }),
            cancellationToken);
        var indexHtml = await createResponse.Content.ReadAsStringAsync(cancellationToken);

        Assert.Equal($"/Admin/Partido/{jornadaId}", createResponse.RequestMessage?.RequestUri?.AbsolutePath);
        Assert.Contains($"CB Prueba 0 A (partido-alta)", indexHtml);
        Assert.Contains("no programado", indexHtml);

        var partidoId = ExtraerId(indexHtml, "Partido", "CB Prueba 0 A (partido-alta)");
        var editUrl = $"/Admin/Partido/Edit/{partidoId}";
        var editPage = await client.GetAsync(editUrl, cancellationToken);
        var editToken = await GetAntiforgeryTokenAsync(editPage, cancellationToken);

        using var editResponse = await client.PostAsync(
            editUrl,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = editToken,
                ["Partido.Id"] = partidoId,
                ["Partido.EquipoLocalId"] = equipoIds[0],
                ["Partido.EquipoVisitanteId"] = equipoIds[1],
                ["Partido.SedeId"] = "",
                ["Partido.FechaHora"] = "2026-03-15T18:30",
            }),
            cancellationToken);
        var afterEditHtml = await editResponse.Content.ReadAsStringAsync(cancellationToken);

        Assert.Equal($"/Admin/Partido/{jornadaId}", editResponse.RequestMessage?.RequestUri?.AbsolutePath);
        Assert.Contains("15/03/2026 18:30", afterEditHtml);
    }

    [Fact]
    public async Task PartidoRechazaEquipoLocalIgualAlVisitante()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var (competicionId, equipoIds) = await CrearCompeticionConEquiposAsync(client, "partido-mismo-equipo", numeroDeEquipos: 1, cancellationToken);
        var jornadaId = await CrearJornadaAsync(client, competicionId, numero: 1, etiqueta: "Jornada 1 (partido-mismo-equipo)", cancellationToken);

        var createPage = await client.GetAsync($"/Admin/Partido/Create/{jornadaId}", cancellationToken);
        var token = await GetAntiforgeryTokenAsync(createPage, cancellationToken);
        using var response = await client.PostAsync(
            $"/Admin/Partido/Create/{jornadaId}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Partido.EquipoLocalId"] = equipoIds[0],
                ["Partido.EquipoVisitanteId"] = equipoIds[0],
                ["Partido.SedeId"] = "",
                ["Partido.FechaHora"] = "",
            }),
            cancellationToken);
        var html = await response.Content.ReadAsStringAsync(cancellationToken);

        Assert.Equal($"/Admin/Partido/Create/{jornadaId}", response.RequestMessage?.RequestUri?.AbsolutePath);
        Assert.Contains("no pueden ser el mismo", html);
    }

    [Fact]
    public async Task PartidoRechazaEquipoQueYaJuegaEnLaJornada()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var (competicionId, equipoIds) = await CrearCompeticionConEquiposAsync(client, "partido-repetido", numeroDeEquipos: 3, cancellationToken);
        var jornadaId = await CrearJornadaAsync(client, competicionId, numero: 1, etiqueta: "Jornada 1 (partido-repetido)", cancellationToken);

        async Task<HttpResponseMessage> CrearPartidoAsync(string equipoLocalId, string equipoVisitanteId)
        {
            var createPage = await client.GetAsync($"/Admin/Partido/Create/{jornadaId}", cancellationToken);
            var token = await GetAntiforgeryTokenAsync(createPage, cancellationToken);

            return await client.PostAsync(
                $"/Admin/Partido/Create/{jornadaId}",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["__RequestVerificationToken"] = token,
                    ["Partido.EquipoLocalId"] = equipoLocalId,
                    ["Partido.EquipoVisitanteId"] = equipoVisitanteId,
                    ["Partido.SedeId"] = "",
                    ["Partido.FechaHora"] = "",
                }),
                cancellationToken);
        }

        using var primeraRespuesta = await CrearPartidoAsync(equipoIds[0], equipoIds[1]);
        Assert.Equal($"/Admin/Partido/{jornadaId}", primeraRespuesta.RequestMessage?.RequestUri?.AbsolutePath);

        // El equipo 0 ya juega (como local) en esta jornada — no puede repetirse
        // como visitante de otro partido, aunque el rival sea distinto.
        using var segundaRespuesta = await CrearPartidoAsync(equipoIds[2], equipoIds[0]);
        var segundaHtml = await segundaRespuesta.Content.ReadAsStringAsync(cancellationToken);

        Assert.Equal($"/Admin/Partido/Create/{jornadaId}", segundaRespuesta.RequestMessage?.RequestUri?.AbsolutePath);
        Assert.Contains("ya tiene un partido programado en esta jornada", segundaHtml);
    }

    [Fact]
    public async Task PartidoEdicionNoModificaElEstadoNiElMarcador()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var (competicionId, equipoIds) = await CrearCompeticionConEquiposAsync(client, "partido-preserva-estado", numeroDeEquipos: 2, cancellationToken);
        var jornadaId = await CrearJornadaAsync(client, competicionId, numero: 1, etiqueta: "Jornada 1 (partido-preserva-estado)", cancellationToken);

        var createPage = await client.GetAsync($"/Admin/Partido/Create/{jornadaId}", cancellationToken);
        var createToken = await GetAntiforgeryTokenAsync(createPage, cancellationToken);
        using var createResponse = await client.PostAsync(
            $"/Admin/Partido/Create/{jornadaId}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = createToken,
                ["Partido.EquipoLocalId"] = equipoIds[0],
                ["Partido.EquipoVisitanteId"] = equipoIds[1],
                ["Partido.SedeId"] = "",
                ["Partido.FechaHora"] = "",
            }),
            cancellationToken);
        var indexHtml = await createResponse.Content.ReadAsStringAsync(cancellationToken);
        var partidoId = int.Parse(ExtraerId(indexHtml, "Partido", "CB Prueba 0 A (partido-preserva-estado)"));

        // Simula que la futura pantalla Resultados ya introdujo un resultado —
        // este incremento no expone ningún camino de UI para hacerlo.
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(fixture.ConnectionString).Options;
        await using (var context = new ApplicationDbContext(options))
        {
            var partido = await context.Partidos.SingleAsync(p => p.Id == partidoId, cancellationToken);
            partido.Estado = PartidoEstado.Jugado;
            partido.PuntosLocal = 80;
            partido.PuntosVisitante = 70;
            await context.SaveChangesAsync(cancellationToken);
        }

        var editUrl = $"/Admin/Partido/Edit/{partidoId}";
        var editPage = await client.GetAsync(editUrl, cancellationToken);
        var editToken = await GetAntiforgeryTokenAsync(editPage, cancellationToken);
        using var editResponse = await client.PostAsync(
            editUrl,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = editToken,
                ["Partido.Id"] = partidoId.ToString(),
                ["Partido.EquipoLocalId"] = equipoIds[0],
                ["Partido.EquipoVisitanteId"] = equipoIds[1],
                ["Partido.SedeId"] = "",
                ["Partido.FechaHora"] = "2026-04-01T20:00",
            }),
            cancellationToken);
        Assert.Equal($"/Admin/Partido/{jornadaId}", editResponse.RequestMessage?.RequestUri?.AbsolutePath);

        await using var verificacion = new ApplicationDbContext(options);
        var partidoTrasEditar = await verificacion.Partidos.SingleAsync(p => p.Id == partidoId, cancellationToken);

        Assert.Equal(PartidoEstado.Jugado, partidoTrasEditar.Estado);
        Assert.Equal(80, partidoTrasEditar.PuntosLocal);
        Assert.Equal(70, partidoTrasEditar.PuntosVisitante);
        Assert.Equal(new DateTime(2026, 4, 1, 20, 0, 0), partidoTrasEditar.FechaHora);
    }
}
