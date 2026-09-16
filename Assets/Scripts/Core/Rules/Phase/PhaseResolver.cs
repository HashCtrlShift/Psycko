using Psycko.Core.Domain;

namespace Psycko.Core.Rules.Phase
{
    /// <summary>
    /// Contrat abstrait commun aux trois phases de jeu (Travail, Talent, Chance).
    /// Répond à deux questions uniquement :
    ///   1. Quelle(s) couche(s) <see cref="CardLayer"/> sont jouables pour un joueur donné, à l'instant T ?
    ///   2. La transition vers la phase suivante doit-elle se déclencher, à l'instant T ?
    ///
    /// Ce résolveur ne tranche jamais le coup joué (validité d'une carte précise, ramassage de pile,
    /// re-pioche, détection Doublon/Carré). Ces responsabilités relèvent respectivement de
    /// CardPlayability, TurnManager et des services de Detection.
    ///
    /// Le joueur (<see cref="Player"/>) est un type immuable : ce résolveur ne fait que LIRE
    /// son état courant (Hand.Count, FaceUp.Count, FaceDown.Count, CurrentPhase) — il ne le modifie
    /// jamais et ne retourne jamais de nouvelle instance de Player. Toute mutation (WithHand,
    /// WithFaceUp, WithFaceDown, WithPhase) relève exclusivement de TurnManager / GameOrchestrator.
    ///
    /// Chaque implémentation concrète (WorkPhaseResolver, TalentPhaseResolver, LuckPhaseResolver)
    /// encapsule uniquement les règles de qualification de couche et de transition propres à sa phase,
    /// telles que verrouillées dans CLAUDE.md.
    /// </summary>
    public abstract class PhaseResolver
    {
        /// <summary>
        /// Phase de jeu représentée par ce résolveur. Doit correspondre à la valeur
        /// que porterait <see cref="Player.CurrentPhase"/> pour un joueur dans cette phase.
        /// </summary>
        public abstract DefPhase Phase { get; }

        /// <summary>
        /// Détermine si une couche de cartes donnée est jouable pour ce joueur, à l'instant T,
        /// dans le cadre de cette phase.
        ///
        /// Note d'exclusivité mutuelle (Phase 3 / Luck) : si player.Hand.Count > 0 (ex. suite à un
        /// Don via 7 ou à un ramassage de pile), CardLayer.Hand devient la SEULE couche jouable et
        /// CardLayer.FaceDown doit être considérée comme non jouable, même si elle est la couche
        /// "native" de cette phase. Cette exclusivité doit être respectée par chaque implémentation
        /// concrète — jamais deux couches jouables simultanément en Phase 3.
        /// </summary>
        /// <param name="player">Joueur pour lequel on qualifie la couche (lecture seule : Hand, FaceUp, FaceDown, CurrentPhase).</param>
        /// <param name="layer">Couche de cartes à qualifier.</param>
        /// <returns>true si cette couche est jouable pour ce joueur à l'instant T, sinon false.</returns>
        public abstract bool IsLayerPlayable(Player player, CardLayer layer);

        /// <summary>
        /// Détermine si les conditions de transition vers la phase suivante sont réunies,
        /// à l'instant T, pour ce joueur.
        ///
        /// Ce résolveur ne réalise jamais la transition lui-même (pas de ramassage de FaceUp,
        /// pas de re-pioche, pas d'appel à Player.WithPhase/WithHand/WithFaceUp/WithFaceDown) —
        /// il se contente de qualifier la condition. L'exécution effective de la transition
        /// (incluant la construction d'un nouveau Player via ses méthodes With*) relève de
        /// TurnManager / GameOrchestrator.
        /// </summary>
        /// <param name="player">Joueur pour lequel on évalue la condition de transition.</param>
        /// <returns>true si la transition vers la phase suivante doit se déclencher, sinon false.</returns>
        public abstract bool ShouldTransitionToNextPhase(Player player);

        /// <summary>
        /// Phase suivante vers laquelle ce résolveur transitionne lorsque
        /// <see cref="ShouldTransitionToNextPhase"/> retourne true.
        /// </summary>
        public abstract DefPhase NextPhase { get; }
    }
}