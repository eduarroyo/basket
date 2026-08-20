using BasketBaseTracker.Seed.Dominio;
using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Seed;

// Orquesta la generación completa del dataset de demostración (spec.md de
// BAS-17). Método público (no solo invocable desde Program.cs) siguiendo el
// mismo patrón que IdentitySeeder.MigrarRolesAsync: permite testearlo
// directamente contra una base de datos real de test sin depender del
// arranque de un proceso de consola aparte.
public static class DemoSeeder
{
    private const int PeriodosPorPartido = 4;
    private const int JugadoresMinimosPorEquipo = 8;
    private const int JugadoresMaximosPorEquipo = 20;

    public static async Task SeedAsync(ApplicationDbContext context, SeedOptions opciones, DateOnly hoy, Random random, CancellationToken cancellationToken)
    {
        var hayDatosExistentes = await context.Temporadas.AnyAsync(cancellationToken);
        if (hayDatosExistentes && !opciones.Reset)
        {
            throw new InvalidOperationException(
                "Ya existen datos de competición en la base de datos. Usa --reset para sustituirlos.");
        }

        if (hayDatosExistentes)
        {
            await BorrarDatosDeportivosAsync(context, cancellationToken);
        }

        var categorias = CrearCategorias();
        context.Categorias.AddRange(categorias);

        var clubesGenerados = Catalogo.GenerarClubes(opciones.Clubes);
        var municipiosUsados = clubesGenerados.Select(c => c.Municipio).Distinct().ToList();
        var sedesPorMunicipio = municipiosUsados.ToDictionary(m => m, CrearSedeParaMunicipio);
        context.Sedes.AddRange(sedesPorMunicipio.Values);

        var clubes = clubesGenerados.Select(c => CrearClub(c, random)).ToList();
        context.Clubes.AddRange(clubes);

        var temporadas = CrearTemporadas(opciones.Temporadas, hoy);
        context.Temporadas.AddRange(temporadas);

        await context.SaveChangesAsync(cancellationToken);

        var competiciones = new List<Competicion>();
        foreach (var temporada in temporadas)
        {
            foreach (var categoria in categorias)
            {
                competiciones.Add(new Competicion { TemporadaId = temporada.Id, CategoriaId = categoria.Id });
            }
        }

        context.Competiciones.AddRange(competiciones);
        await context.SaveChangesAsync(cancellationToken);

        var equipos = new List<Equipo>();
        foreach (var competicion in competiciones)
        {
            foreach (var club in clubes)
            {
                equipos.Add(new Equipo
                {
                    CompeticionId = competicion.Id,
                    ClubId = club.Id,
                    Nombre = club.Nombre,
                    SedeHabitualId = sedesPorMunicipio[club.Municipio].Id,
                    Estado = EquipoEstado.Activo,
                });
            }
        }

        context.Equipos.AddRange(equipos);
        await context.SaveChangesAsync(cancellationToken);

        var fichas = equipos.SelectMany(equipo => CrearPlantilla(equipo, random)).ToList();
        context.FichasJugador.AddRange(fichas);
        await context.SaveChangesAsync(cancellationToken);

        var equiposPorCompeticion = equipos.ToLookup(e => e.CompeticionId);
        var temporadaPorCompeticion = competiciones.ToDictionary(c => c.Id, c => temporadas.Single(t => t.Id == c.TemporadaId));

        var jornadas = new List<Jornada>();
        var partidosPorJornada = new Dictionary<Jornada, List<(Equipo Local, Equipo Visitante)>>();
        var totalJornadasPorCompeticion = new Dictionary<int, int>();
        foreach (var competicion in competiciones)
        {
            var equiposDeLaCompeticion = equiposPorCompeticion[competicion.Id].ToList();
            var equiposPorId = equiposDeLaCompeticion.ToDictionary(e => e.Id);
            var calendario = CalendarioRoundRobin.GenerarJornadas(equiposDeLaCompeticion.Select(e => e.Id).ToList());
            totalJornadasPorCompeticion[competicion.Id] = calendario.Count;

            for (var numero = 1; numero <= calendario.Count; numero++)
            {
                var jornada = new Jornada { CompeticionId = competicion.Id, Numero = numero, CuentaParaClasificacion = true };
                jornadas.Add(jornada);
                partidosPorJornada[jornada] = calendario[numero - 1]
                    .Select(p => (equiposPorId[p.EquipoLocalId], equiposPorId[p.EquipoVisitanteId]))
                    .ToList();
            }
        }

        context.Jornadas.AddRange(jornadas);
        await context.SaveChangesAsync(cancellationToken);

        var partidos = new List<Partido>();
        var resultadosPorPartido = new Dictionary<Partido, ResultadoGenerado>();
        foreach (var jornada in jornadas)
        {
            var temporada = temporadaPorCompeticion[jornada.CompeticionId];
            var puntoDeReferencia = temporada.FechaInicio;
            if (temporada.Estado == TemporadaEstado.EnCurso && (hoy < temporada.FechaInicio || hoy > temporada.FechaFin))
            {
                // "hoy" cae fuera de la ventana Sept-jun de la temporada en curso (p. ej.
                // parón estival de julio-agosto): no hay forma realista de que la
                // temporada esté "a medio disputar" en esas fechas, así que se antepone
                // el criterio de aceptación (mezcla Jugado/Programado, spec.md de
                // BAS-17) a la verosimilitud exacta de las fechas — se recentra el
                // calendario de esta competición para que "hoy" caiga a mitad de sus
                // jornadas, en vez de usar el inicio real de la temporada.
                var totalJornadas = totalJornadasPorCompeticion[jornada.CompeticionId];
                puntoDeReferencia = hoy.AddDays(-7 * (totalJornadas / 2));
            }

            var fechaHora = FechaHoraDeJornada(puntoDeReferencia, jornada.Numero);
            var jugado = DateOnly.FromDateTime(fechaHora) <= hoy;

            foreach (var (local, visitante) in partidosPorJornada[jornada])
            {
                var partido = new Partido
                {
                    JornadaId = jornada.Id,
                    EquipoLocalId = local.Id,
                    EquipoVisitanteId = visitante.Id,
                    SedeId = local.SedeHabitualId,
                    FechaHora = fechaHora,
                    Estado = jugado ? PartidoEstado.Jugado : PartidoEstado.Programado,
                };

                if (jugado)
                {
                    var resultado = GeneradorResultados.Generar(random);
                    partido.PuntosLocal = resultado.PuntosLocal;
                    partido.PuntosVisitante = resultado.PuntosVisitante;
                    resultadosPorPartido[partido] = resultado;
                }

                partidos.Add(partido);
            }
        }

        context.Partidos.AddRange(partidos);
        await context.SaveChangesAsync(cancellationToken);

        var parciales = new List<PartidoParcial>();
        foreach (var (partido, resultado) in resultadosPorPartido)
        {
            for (var periodo = 1; periodo <= PeriodosPorPartido; periodo++)
            {
                var (puntosLocal, puntosVisitante) = resultado.Parciales[periodo - 1];
                parciales.Add(new PartidoParcial
                {
                    PartidoId = partido.Id,
                    NumeroPeriodo = periodo,
                    PuntosLocal = puntosLocal,
                    PuntosVisitante = puntosVisitante,
                });
            }
        }

        context.PartidosParciales.AddRange(parciales);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task BorrarDatosDeportivosAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        // Orden inverso de dependencia (DeleteBehavior.Restrict en todas las FK,
        // sin cascade — mismo orden que ImportadorDatos.TablasEnOrdenDeBorrado).
        await context.PenalizacionesClasificacion.ExecuteDeleteAsync(cancellationToken);
        await context.PartidosParciales.ExecuteDeleteAsync(cancellationToken);
        await context.Partidos.ExecuteDeleteAsync(cancellationToken);
        await context.Jornadas.ExecuteDeleteAsync(cancellationToken);
        await context.FichasJugador.ExecuteDeleteAsync(cancellationToken);
        await context.Equipos.ExecuteDeleteAsync(cancellationToken);
        await context.Competiciones.ExecuteDeleteAsync(cancellationToken);
        await context.Categorias.ExecuteDeleteAsync(cancellationToken);
        await context.Clubes.ExecuteDeleteAsync(cancellationToken);
        await context.Sedes.ExecuteDeleteAsync(cancellationToken);
        await context.Temporadas.ExecuteDeleteAsync(cancellationToken);
    }

    private static List<Categoria> CrearCategorias() =>
        Catalogo.Categorias.Select(c => new Categoria { Nombre = c.Nombre, Orden = c.Orden }).ToList();

    private static Sede CrearSedeParaMunicipio(string municipio) => new()
    {
        Nombre = $"Pabellón Municipal de {municipio}",
        Municipio = municipio,
        Direccion = "Avenida del Deporte, s/n",
    };

    private static Club CrearClub(ClubGenerado generado, Random random) => new()
    {
        Nombre = generado.Nombre,
        Municipio = generado.Municipio,
        FechaAlta = new DateOnly(random.Next(1965, 2016), 1, 1),
    };

    private static List<Temporada> CrearTemporadas(int numeroDeTemporadasHistoricas, DateOnly hoy)
    {
        // La temporada empieza en septiembre y termina en junio del año
        // siguiente; si "hoy" cae entre enero y agosto, la temporada en curso
        // empezó el septiembre del año anterior.
        var anioInicioActual = hoy.Month >= 9 ? hoy.Year : hoy.Year - 1;

        var temporadas = new List<Temporada>();
        for (var i = numeroDeTemporadasHistoricas; i >= 1; i--)
        {
            var anioInicio = anioInicioActual - i;
            temporadas.Add(new Temporada
            {
                Nombre = $"{anioInicio}-{anioInicio + 1}",
                FechaInicio = new DateOnly(anioInicio, 9, 1),
                FechaFin = new DateOnly(anioInicio + 1, 6, 30),
                Estado = i == 1 ? TemporadaEstado.Finalizada : TemporadaEstado.Archivada,
            });
        }

        temporadas.Add(new Temporada
        {
            Nombre = $"{anioInicioActual}-{anioInicioActual + 1}",
            FechaInicio = new DateOnly(anioInicioActual, 9, 1),
            FechaFin = new DateOnly(anioInicioActual + 1, 6, 30),
            Estado = TemporadaEstado.EnCurso,
        });

        return temporadas;
    }

    private static List<FichaJugador> CrearPlantilla(Equipo equipo, Random random)
    {
        var numeroDeJugadores = random.Next(JugadoresMinimosPorEquipo, JugadoresMaximosPorEquipo + 1);
        var posiciones = Enum.GetValues<Posicion>();
        var fichas = new List<FichaJugador>();
        for (var dorsal = 1; dorsal <= numeroDeJugadores; dorsal++)
        {
            fichas.Add(new FichaJugador
            {
                EquipoId = equipo.Id,
                Dorsal = dorsal,
                Posicion = posiciones[random.Next(posiciones.Length)],
            });
        }

        return fichas;
    }

    // Reparte las jornadas en fines de semana consecutivos (sábados a las
    // 18:00) desde el inicio de la temporada — plan.md, decisión técnica 5.
    private static DateTime FechaHoraDeJornada(DateOnly fechaInicioTemporada, int numeroDeJornada)
    {
        var inicio = fechaInicioTemporada.ToDateTime(TimeOnly.MinValue);
        var diasHastaElPrimerSabado = ((int)DayOfWeek.Saturday - (int)inicio.DayOfWeek + 7) % 7;
        var primerSabado = inicio.AddDays(diasHastaElPrimerSabado);
        return primerSabado.AddDays((numeroDeJornada - 1) * 7).AddHours(18);
    }
}
