namespace BasketBaseTracker.Seed;

public sealed record SeedOptions(int Temporadas, int Clubes, bool Reset)
{
    public const int TemporadasPorDefecto = 3;
    public const int ClubesPorDefecto = 10;
}
