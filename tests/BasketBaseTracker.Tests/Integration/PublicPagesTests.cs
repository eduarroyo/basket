using static BasketBaseTracker.Tests.Integration.AdminHttpTestHelpers;

namespace BasketBaseTracker.Tests.Integration;

// Cubre el núcleo navegable del área pública de BAS-12 (Portada, Listado de
// competiciones, Ficha de equipo/club/sede) vía HTTP real contra el AppHost
// completo: contenido, enlaces de navegación, 404 en id inexistente y Output
// Caching activo (misma técnica de verificación que ClasificacionPagesTests, BAS-11).
public class PublicPagesTests(AppHostSqlFixture fixture) : IClassFixture<AppHostSqlFixture>
{
    private static async Task<string> CrearSedeAsync(HttpClient client, string nombre, CancellationToken cancellationToken)
    {
        var createPage = await client.GetAsync("/Admin/Sede/Create", cancellationToken);
        var token = await GetAntiforgeryTokenAsync(createPage, cancellationToken);

        var response = await client.PostAsync(
            "/Admin/Sede/Create",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Sede.Nombre"] = nombre,
                ["Sede.Municipio"] = "Sevilla",
                ["Sede.Direccion"] = "Calle de Prueba, 1",
            }),
            cancellationToken);
        var html = await response.Content.ReadAsStringAsync(cancellationToken);

        return ExtraerId(html, "Sede", nombre);
    }

    private static async Task AsignarSedeHabitualAsync(
        HttpClient client, string equipoId, string competicionId, string clubId, string equipoNombre, string sedeId, CancellationToken cancellationToken)
    {
        var editUrl = $"/Admin/Equipo/Edit/{equipoId}";
        var editPage = await client.GetAsync(editUrl, cancellationToken);
        var editHtml = await editPage.Content.ReadAsStringAsync(cancellationToken);
        var token = await GetAntiforgeryTokenAsync(editPage, cancellationToken);
        var rowVersion = ExtraerCampoOculto(editHtml, "Equipo.RowVersion");

        var response = await client.PostAsync(
            editUrl,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Equipo.Id"] = equipoId,
                ["Equipo.RowVersion"] = rowVersion,
                ["Equipo.CompeticionId"] = competicionId,
                ["Equipo.ClubId"] = clubId,
                ["Equipo.Nombre"] = equipoNombre,
                ["Equipo.SedeHabitualId"] = sedeId,
                ["Equipo.Estado"] = "0",
            }),
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<string> CrearFichaJugadorAsync(HttpClient client, string equipoId, int dorsal, CancellationToken cancellationToken)
    {
        var createPage = await client.GetAsync($"/Admin/FichaJugador/Create/{equipoId}", cancellationToken);
        var token = await GetAntiforgeryTokenAsync(createPage, cancellationToken);

        var response = await client.PostAsync(
            $"/Admin/FichaJugador/Create/{equipoId}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["FichaJugador.Dorsal"] = dorsal.ToString(),
                ["FichaJugador.Posicion"] = "0",
            }),
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    [Fact]
    public async Task PortadaListaTemporadasYResaltaLaActual()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        await CrearCompeticionConEquiposAsync(client, "portada", numeroDeEquipos: 0, cancellationToken, temporadaEstado: "1"); // EnCurso

        using var anonimo = fixture.CreateAnonymousWebHttpClient();
        var html = await (await anonimo.GetAsync("/", cancellationToken)).Content.ReadAsStringAsync(cancellationToken);

        Assert.Contains("2025-2026 (portada)", html);

        // Otros tests en paralelo también pueden crear temporadas EnCurso, así que
        // "Actual" se comprueba dentro de la misma fila que esta temporada
        // concreta, no en cualquier parte de la página.
        var pattern = $"""<li[^>]*>(?:(?!</li>)[\s\S])*?2025-2026 \(portada\)(?:(?!</li>)[\s\S])*?</li>""";
        var fila = System.Text.RegularExpressions.Regex.Match(html, pattern);
        Assert.True(fila.Success, "No se encontró la fila de la temporada en la portada.");
        Assert.Contains("Actual", fila.Value);
    }

    [Fact]
    public async Task ListadoDeCompeticionesMuestraEquiposAgrupadosYEnlazaClasificacion()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var (competicionId, equipoIds, equipoNombres, temporadaId, _) = await CrearCompeticionConEquiposAsync(client, "listado-competiciones", numeroDeEquipos: 2, cancellationToken);

        using var anonimo = fixture.CreateAnonymousWebHttpClient();
        var html = await (await anonimo.GetAsync($"/temporadas/{temporadaId}/competiciones", cancellationToken)).Content.ReadAsStringAsync(cancellationToken);

        Assert.Contains("Cadete (listado-competiciones)", html);
        Assert.Contains(equipoNombres[0], html);
        Assert.Contains(equipoNombres[1], html);
        Assert.Contains($"/competiciones/{competicionId}/clasificacion", html);
        Assert.Contains($"/equipos/{equipoIds[0]}", html);
    }

    [Fact]
    public async Task FichaDeEquipoMuestraClubSedeYPlantilla()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var (competicionId, equipoIds, equipoNombres, _, clubIds) = await CrearCompeticionConEquiposAsync(client, "ficha-equipo", numeroDeEquipos: 1, cancellationToken);
        var sedeId = await CrearSedeAsync(client, "Pabellon de Prueba (ficha-equipo)", cancellationToken);

        var clubId = clubIds[0];
        await AsignarSedeHabitualAsync(client, equipoIds[0], competicionId, clubId, equipoNombres[0], sedeId, cancellationToken);
        await CrearFichaJugadorAsync(client, equipoIds[0], dorsal: 7, cancellationToken);

        using var anonimo = fixture.CreateAnonymousWebHttpClient();
        var html = await (await anonimo.GetAsync($"/equipos/{equipoIds[0]}", cancellationToken)).Content.ReadAsStringAsync(cancellationToken);

        Assert.Contains(equipoNombres[0], html);
        Assert.Contains("CB Prueba 0 (ficha-equipo)", html); // nombre del club
        Assert.Contains("Pabellon de Prueba (ficha-equipo)", html);
        Assert.Contains($"/clubes/{clubId}", html);
        Assert.Contains($"/sedes/{sedeId}", html);
        Assert.Contains("<td>7</td>", html);
        Assert.Contains("Base", html);
    }

    [Fact]
    public async Task FichaDeClubMuestraEquiposDeLaTemporadaEnCurso()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var (_, equipoIds, equipoNombres, _, clubIds) = await CrearCompeticionConEquiposAsync(
            client, "ficha-club-actual", numeroDeEquipos: 1, cancellationToken, temporadaEstado: "1"); // EnCurso

        using var anonimo = fixture.CreateAnonymousWebHttpClient();
        var html = await (await anonimo.GetAsync($"/clubes/{clubIds[0]}", cancellationToken)).Content.ReadAsStringAsync(cancellationToken);

        Assert.Contains(equipoNombres[0], html);
        Assert.Contains($"/equipos/{equipoIds[0]}", html);
    }

    [Fact]
    public async Task FichaDeClubSinTemporadaEnCursoNoFalla()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        // temporadaEstado por defecto es Planificada, no EnCurso.
        var (_, _, _, _, clubIds) = await CrearCompeticionConEquiposAsync(client, "ficha-club-sin-actual", numeroDeEquipos: 1, cancellationToken);

        using var anonimo = fixture.CreateAnonymousWebHttpClient();
        using var response = await anonimo.GetAsync($"/clubes/{clubIds[0]}", cancellationToken);
        var html = await response.Content.ReadAsStringAsync(cancellationToken);

        response.EnsureSuccessStatusCode();
        Assert.Contains("No hay ninguna temporada en curso", html);
    }

    [Fact]
    public async Task FichaDeSedeMuestraDatos()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var sedeId = await CrearSedeAsync(client, "Pabellon Municipal (ficha-sede)", cancellationToken);

        using var anonimo = fixture.CreateAnonymousWebHttpClient();
        var html = await (await anonimo.GetAsync($"/sedes/{sedeId}", cancellationToken)).Content.ReadAsStringAsync(cancellationToken);

        Assert.Contains("Municipal", html);
        Assert.Contains("Sevilla", html);
        Assert.Contains("Calle de Prueba, 1", html);
    }

    [Fact]
    public async Task IdInexistenteDevuelve404EnLasPaginasConId()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var anonimo = fixture.CreateAnonymousWebHttpClient();

        // Portada (/) no tiene id, así que no aplica el concepto de 404 por id
        // inexistente — solo las otras cuatro páginas de este incremento.
        string[] rutas =
        [
            "/temporadas/999999/competiciones",
            "/equipos/999999",
            "/clubes/999999",
            "/sedes/999999",
        ];

        foreach (var ruta in rutas)
        {
            using var response = await anonimo.GetAsync(ruta, cancellationToken);
            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    [Fact]
    public async Task LaBarraSuperiorEnlazaALaPortada()
    {
        // BAS-18: título y "Inicio" usaban asp-area="" (sin área), que no resuelve
        // a ninguna página real y dejaba al usuario en la página actual.
        var cancellationToken = TestContext.Current.CancellationToken;
        using var anonimo = fixture.CreateAnonymousWebHttpClient();
        var html = await (await anonimo.GetAsync("/", cancellationToken)).Content.ReadAsStringAsync(cancellationToken);

        Assert.Contains("""href="/">BasketBaseTracker</a>""", html);
        Assert.Contains("""href="/">Inicio</a>""", html);
    }

    [Fact]
    public async Task VerWebPublicaDesdeElAreaAdminEnlazaALaPortada()
    {
        // BAS-18: mismo bug que el título/"Inicio" de la barra pública, en el
        // layout del área Admin.
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var html = await (await client.GetAsync("/Admin/Index", cancellationToken)).Content.ReadAsStringAsync(cancellationToken);

        Assert.Contains("""href="/">Ver web pública</a>""", html);
    }

    [Fact]
    public async Task LaBarraSuperiorPublicaEnlazaAlAreaDeGestion()
    {
        // BAS-20: acceso al panel de administración desde la parte pública, sin
        // comprobar el estado de autenticación en el propio layout (el enlace es
        // idéntico para anónimos y autenticados; decide la redirección el middleware).
        var cancellationToken = TestContext.Current.CancellationToken;
        using var anonimo = fixture.CreateAnonymousWebHttpClient();
        var html = await (await anonimo.GetAsync("/", cancellationToken)).Content.ReadAsStringAsync(cancellationToken);

        Assert.Contains("""href="/Admin">Área de gestión</a>""", html);
    }

    [Fact]
    public async Task ElEnlaceAlAreaDeGestionRedirigeALoginSinSesion()
    {
        // BAS-20: seguir el enlace "Área de gestión" sin sesión iniciada debe
        // aterrizar en el login (mismo mecanismo que cualquier página Admin, ver
        // CatalogoAdminPagesTests.PeticionAnonimaATemporadasRedirigeALogin).
        var cancellationToken = TestContext.Current.CancellationToken;
        using var anonimo = fixture.CreateAnonymousWebHttpClient();

        using var response = await anonimo.GetAsync("/Admin", cancellationToken);

        Assert.Equal("/Admin/Login", response.RequestMessage?.RequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task ElEnlaceAlAreaDeGestionAterrizaDirectoConSesionIniciada()
    {
        // BAS-20: con sesión ya iniciada, el mismo enlace no debe pasar por login.
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        using var response = await client.GetAsync("/Admin", cancellationToken);

        Assert.Equal("/Admin", response.RequestMessage?.RequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task LasPaginasPublicasLlevanOutputCachingActivo()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var sedeId = await CrearSedeAsync(client, "Pabellon Cache (cache-publicas)", cancellationToken);

        using var anonimo = fixture.CreateAnonymousWebHttpClient();
        using var primera = await anonimo.GetAsync($"/sedes/{sedeId}", cancellationToken);
        using var segunda = await anonimo.GetAsync($"/sedes/{sedeId}", cancellationToken);

        Assert.False(primera.Headers.Contains("Age"));
        Assert.True(segunda.Headers.Contains("Age"));
    }
}
