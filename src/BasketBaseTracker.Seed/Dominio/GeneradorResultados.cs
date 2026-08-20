namespace BasketBaseTracker.Seed.Dominio;

public sealed record ResultadoGenerado(int PuntosLocal, int PuntosVisitante, IReadOnlyList<(int PuntosLocal, int PuntosVisitante)> Parciales);

// Lógica de dominio pura: genera un resultado plausible de baloncesto base y
// sus 4 parciales por periodo, con la invariante de que la suma de los
// parciales coincide exactamente con el resultado final — plan.md de BAS-17,
// decisión técnica 6.
public static class GeneradorResultados
{
    private const int PuntosMinimosPorEquipo = 25;
    private const int PuntosMaximosPorEquipo = 70;
    private const int NumeroDePeriodos = 4;

    public static ResultadoGenerado Generar(Random random)
    {
        var puntosLocal = random.Next(PuntosMinimosPorEquipo, PuntosMaximosPorEquipo + 1);
        var puntosVisitante = random.Next(PuntosMinimosPorEquipo, PuntosMaximosPorEquipo + 1);
        if (puntosLocal == puntosVisitante)
        {
            // Un partido de baloncesto base no puede terminar en empate (se juega
            // prórroga hasta deshacerlo) — resumen-reglas-relevantes.md, punto 1.
            puntosVisitante += 1;
        }

        var parcialesLocal = RepartirEnPeriodos(puntosLocal, random);
        var parcialesVisitante = RepartirEnPeriodos(puntosVisitante, random);
        var parciales = parcialesLocal.Zip(parcialesVisitante, (l, v) => (l, v)).ToList();

        return new ResultadoGenerado(puntosLocal, puntosVisitante, parciales);
    }

    private static int[] RepartirEnPeriodos(int total, Random random)
    {
        var cortes = new int[NumeroDePeriodos + 1];
        cortes[0] = 0;
        cortes[NumeroDePeriodos] = total;
        for (var i = 1; i < NumeroDePeriodos; i++)
        {
            cortes[i] = random.Next(0, total + 1);
        }

        Array.Sort(cortes);

        var partes = new int[NumeroDePeriodos];
        for (var i = 0; i < NumeroDePeriodos; i++)
        {
            partes[i] = cortes[i + 1] - cortes[i];
        }

        return partes;
    }
}
