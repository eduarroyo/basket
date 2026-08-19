using BasketBaseTracker.Web.Data.Entities;
using BasketBaseTracker.Web.Data.ImportExport;
using BasketBaseTracker.Web.Domain;

namespace BasketBaseTracker.Tests.Unit;

public class ImportExportServiceTests
{
    private static ExportacionDatos FicheroValido()
    {
        SedeExport[] sedes = [new(1, "Pabellon", "Sevilla", "Calle 1")];
        ClubExport[] clubes = [new(1, "CB Triana", "Sevilla", new DateOnly(2020, 1, 1)), new(2, "CB Nervion", "Sevilla", new DateOnly(2020, 1, 1))];
        TemporadaExport[] temporadas = [new(1, "2025-2026", new DateOnly(2025, 9, 1), new DateOnly(2026, 6, 30), TemporadaEstado.EnCurso)];
        CategoriaExport[] categorias = [new(1, "Cadete", 1)];
        CompeticionExport[] competiciones = [new(1, 1, 1, 2, 1)];
        EquipoExport[] equipos = [new(1, 1, 1, "CB Triana A", 1, EquipoEstado.Activo), new(2, 1, 2, "CB Nervion A", null, EquipoEstado.Activo)];
        FichaJugadorExport[] fichas = [new(1, 1, 4, Posicion.Base)];
        JornadaExport[] jornadas = [new(1, 1, 1, "Jornada 1", true)];
        PartidoExport[] partidos = [new(1, 1, 1, 2, 1, new DateTime(2026, 10, 4, 18, 0, 0), PartidoEstado.Programado, null, null, null, null, null)];
        PartidoParcialExport[] parciales = [new(1, 1, 1, 20, 18)];
        PenalizacionClasificacionExport[] penalizaciones = [new(1, 1, -1, "Sanción disciplinaria", 1, new DateOnly(2026, 1, 1))];

        return new ExportacionDatos(
            ImportExportService.SchemaVersionActual, sedes, clubes, temporadas, categorias, competiciones,
            equipos, fichas, jornadas, partidos, parciales, penalizaciones);
    }

    [Fact]
    public void Exportar_MapeaCadaEntidadASuDto()
    {
        Sede[] sedes = [new() { Id = 1, Nombre = "Pabellon", Municipio = "Sevilla", Direccion = "Calle 1" }];
        Club[] clubes = [new() { Id = 1, Nombre = "CB Triana", Municipio = "Sevilla", FechaAlta = new DateOnly(2020, 1, 1) }];

        var resultado = ImportExportService.Exportar(
            sedes, clubes, [], [], [], [], [], [], [], [], []);

        Assert.Equal(ImportExportService.SchemaVersionActual, resultado.SchemaVersion);
        var sede = Assert.Single(resultado.Sedes);
        Assert.Equal(new SedeExport(1, "Pabellon", "Sevilla", "Calle 1"), sede);
        var club = Assert.Single(resultado.Clubes);
        Assert.Equal(new ClubExport(1, "CB Triana", "Sevilla", new DateOnly(2020, 1, 1)), club);
    }

    [Fact]
    public void Validar_FicheroCorrecto_NoDevuelveErrores()
    {
        var errores = ImportExportService.Validar(FicheroValido());

        Assert.Empty(errores);
    }

    [Fact]
    public void Validar_VersionNoSoportada_DevuelveUnUnicoError()
    {
        var datos = FicheroValido() with { SchemaVersion = 99 };

        var errores = ImportExportService.Validar(datos);

        var error = Assert.Single(errores);
        Assert.Contains("versión de esquema", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validar_FkHuerfana_DevuelveError()
    {
        var valido = FicheroValido();
        var datos = valido with { Equipos = [valido.Equipos[0] with { CompeticionId = 999 }, valido.Equipos[1]] };

        var errores = ImportExportService.Validar(datos);

        Assert.Contains(errores, e => e.Contains("CompeticionId 999", StringComparison.Ordinal));
    }

    [Fact]
    public void Validar_RestriccionUnicaViolada_DevuelveError()
    {
        var valido = FicheroValido();
        CompeticionExport[] competicionesDuplicadas = [valido.Competiciones[0], valido.Competiciones[0] with { Id = 2 }];
        var datos = valido with { Competiciones = competicionesDuplicadas };

        var errores = ImportExportService.Validar(datos);

        Assert.Contains(errores, e => e.Contains("Competicion duplicada", StringComparison.Ordinal));
    }

    [Fact]
    public void Validar_EquipoRepetidoEnLaMismaJornada_DevuelveError()
    {
        var valido = FicheroValido();
        // Segundo partido en la misma jornada 1, reutilizando el equipo 1 (ya
        // juega el partido 1 como local).
        PartidoExport[] partidosConConflicto =
        [
            valido.Partidos[0],
            new(2, 1, 1, 2, null, null, PartidoEstado.Programado, null, null, null, null, null),
        ];
        var datos = valido with { Partidos = partidosConConflicto };

        var errores = ImportExportService.Validar(datos);

        Assert.Contains(errores, e => e.Contains("ya juega otro partido en la jornada", StringComparison.Ordinal));
    }
}
