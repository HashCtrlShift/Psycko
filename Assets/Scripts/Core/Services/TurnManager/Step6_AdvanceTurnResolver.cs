using System;
using Psycko.Core.Domain;

namespace Psycko.Core.Services.TurnManager
{
    /// <summary>
    /// Step6 — source unique de calcul de l'intention d'avancement.
    ///
    /// Ce resolver est purement fonctionnel : il ne mute jamais l'état et n'appelle
    /// jamais IGameStateCommand. Il retourne un TurnResult dont
    /// NextActivePlayerIndex vaut l'index calculé, ou null lorsqu'il n'y a aucun
    /// changement (notamment en cas de rejeu).
    /// L'exécution de cette intention appartient exclusivement à GameOrchestrator.
    /// </summary>
    public static class Step6_AdvanceTurnResolver
    {
        public static TurnResult Resolve(GameState state, bool skipNext, bool replay)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));

            // Un rejeu conserve strictement l'index courant : aucune intention
            // d'appel à SetActivePlayer n'est produite.
            if (replay)
                return new TurnResult(state, skipNext: false, replay: false)
                    .WithNextActivePlayerIndex(null);

            var activeCount = 0;
            for (var i = 0; i < state.Players.Count; i++)
                if (state.Players[i].CurrentPhase != DefPhase.Finished)
                    activeCount++;

            // La fin de partie est traitée par l'orchestrateur. Ici, on évite
            // simplement toute boucle lorsqu'il ne reste au plus qu'un actif.
            if (activeCount <= 1)
                return new TurnResult(state, skipNext: false, replay: false)
                    .WithNextActivePlayerIndex(null);

            var current = state.ActivePlayerIndex;
            if (current < 0 || current >= state.Players.Count)
                throw new ArgumentOutOfRangeException(nameof(state),
                    "ActivePlayerIndex doit être un siège valide.");

            // Un pas normal, puis exactement un pas actif supplémentaire si skipNext.
            // Les sièges Finished sont traversés mais ne consomment aucun pas.
            var steps = skipNext ? 2 : 1;
            for (var step = 0; step < steps; step++)
            {
                do
                {
                    current = NextSeat(current, state.Players.Count, state.Direction);
                }
                while (state.Players[current].CurrentPhase == DefPhase.Finished);
            }

            return new TurnResult(state, skipNext: false, replay: false)
                .WithNextActivePlayerIndex(current);
        }

        private static int NextSeat(int current, int count, PlayDirection direction)
        {
            return direction == PlayDirection.Clockwise
                ? (current + 1) % count
                : (current - 1 + count) % count;
        }
    }
}