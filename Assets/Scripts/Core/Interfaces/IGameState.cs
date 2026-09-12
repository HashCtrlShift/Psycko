namespace Psycko.Core.Interfaces
{
    /// <summary>
    /// Vue complète d'un état de partie : lecture + transitions.
    /// Permet le chaînage sans cast (state.PlayCards(p).DrawCards(i, 1)...)
    /// tout en gardant le type concret GameState invisible hors de Domain.
    ///
    /// Règle d'usage :
    /// - Rules/ reçoit des IGameStateQuery (lecture seule garantie par la signature).
    /// - Services/ (GameOrchestrator, TurnManager) manipule des IGameState.
    /// </summary>
    public interface IGameState : IGameStateQuery, IGameStateCommand
    {
    }
}