namespace BasketBaseTracker.Seed.Dominio;

public sealed record Emparejamiento(int EquipoLocalId, int EquipoVisitanteId);

// Lógica de dominio pura (sin I/O): genera el calendario de una competición
// mediante el método del círculo (circle method) de emparejamiento
// round-robin. Garantiza por construcción que ningún equipo se repite en la
// misma jornada — spec.md de BAS-17.
public static class CalendarioRoundRobin
{
    public static IReadOnlyList<IReadOnlyList<Emparejamiento>> GenerarJornadas(
        IReadOnlyList<int> equipoIds, bool dobleVuelta = true)
    {
        if (equipoIds.Count < 2)
        {
            return [];
        }

        var equipos = equipoIds.Select(id => (int?)id).ToList();
        if (equipos.Count % 2 != 0)
        {
            equipos.Add(null); // equipo "fantasma": a quien le toque descansa esa ronda (bye).
        }

        var n = equipos.Count;
        var mitad = n / 2;
        var actuales = new List<int?>(equipos);
        var primeraVuelta = new List<IReadOnlyList<Emparejamiento>>();

        for (var ronda = 0; ronda < n - 1; ronda++)
        {
            var partidos = new List<Emparejamiento>();
            for (var i = 0; i < mitad; i++)
            {
                var a = actuales[i];
                var b = actuales[n - 1 - i];
                if (a is null || b is null)
                {
                    continue; // el equipo emparejado con el fantasma descansa esta ronda.
                }

                // Alterna qué lado del emparejamiento juega en casa entre rondas, para
                // no fijar siempre el mismo patrón local/visitante.
                partidos.Add((ronda + i) % 2 == 0
                    ? new Emparejamiento(a.Value, b.Value)
                    : new Emparejamiento(b.Value, a.Value));
            }

            primeraVuelta.Add(partidos);

            // Rotación del método del círculo: el primer equipo queda fijo, el resto
            // rota una posición (el último pasa a la segunda posición).
            var fijo = actuales[0];
            var ultimo = actuales[^1];
            var resto = actuales.GetRange(1, actuales.Count - 2);
            actuales = [fijo, ultimo, .. resto];
        }

        if (!dobleVuelta)
        {
            return primeraVuelta;
        }

        var segundaVuelta = primeraVuelta
            .Select(jornada => (IReadOnlyList<Emparejamiento>)jornada
                .Select(p => new Emparejamiento(p.EquipoVisitanteId, p.EquipoLocalId))
                .ToList())
            .ToList();

        return [.. primeraVuelta, .. segundaVuelta];
    }
}
