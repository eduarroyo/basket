using BasketBaseTracker.Web.Data.Entities;

namespace BasketBaseTracker.Web.Domain;

public static class ResultadoReglas
{
    public static bool RequiereMarcador(PartidoEstado estado) =>
        estado is PartidoEstado.Jugado or PartidoEstado.Resuelto;

    // Las reglas FIBA de prórroga excluyen el empate en cualquier partido
    // terminado — reglamento/resumen-reglas-relevantes.md, §1.
    public static bool MarcadorValido(int? puntosLocal, int? puntosVisitante) =>
        puntosLocal is not null && puntosVisitante is not null && puntosLocal != puntosVisitante;

    public static bool ResolucionValida(MotivoResolucion? motivo, int? equipoGanadorId, int equipoLocalId, int equipoVisitanteId) =>
        motivo is not null && equipoGanadorId is not null &&
        (equipoGanadorId == equipoLocalId || equipoGanadorId == equipoVisitanteId);

    // Marcador técnico habitual (2-0) de los Art. 80/148/149.2 del Reglamento
    // General de la F.A.B. — reglamento/resumen-reglas-relevantes.md, §4.
    public static (int Local, int Visitante) MarcadorTecnicoSugerido(int equipoGanadorId, int equipoLocalId) =>
        equipoGanadorId == equipoLocalId ? (2, 0) : (0, 2);
}
