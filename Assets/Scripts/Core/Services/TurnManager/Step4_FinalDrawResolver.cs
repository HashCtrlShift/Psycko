using System;
using Psycko.Core.Domain;
using Psycko.Core.Rules.Validation;

namespace Psycko.Core.Services.TurnManager
{
    /// <summary>
    /// Étape 4 — RE-PIOCHE FINALE (sous-temps 4 de l'Ordre Strict, CLAUDE.md).
    /// Point de reprise après résolution du Don du 7 (RequiresGiftResolution).
    /// Ne mute jamais l'état, ne réalise aucun Don ni aucune pioche réelle :
    /// délègue intégralement la décision à HandReconstructionPolicy et produit
    /// une intention portée par TurnResult.FinalReconstruction.
    ///
    /// SÉMANTIQUE DE previous.RequiresGiftResolution — À NE PAS SUR-INTERPRÉTER.
    /// Ce drapeau signifie « un Don devait être résolu, et l'état reçu ici EST
    /// l'état réel après cette résolution » (GameOrchestrator a muté les mains
    /// via IGameStateCommand avant d'appeler ResolveRemainder). Il ne signifie
    /// PAS littéralement « une carte a été donnée » : SevenHandler.IsGiftTriggered
    /// peut lever ce drapeau à tort si la main du poseur était en réalité vide
    /// après reconstruction (bug connu, cf. ticket T18-bis, non corrigé ici).
    /// Dans ce cas limite, l'état reçu est simplement l'état réel inchangé par
    /// un Don, et Reconstruct(..., 0) reste correct car aucune carte n'est
    /// retirée en trop.
    ///
    /// SANS Don (RequiresGiftResolution == false) : Step4 ne fait rien.
    /// Step2 a déjà résolu la reconstruction sur cet état non muté, et l'état
    /// courant respecte l'invariant « main ≥ 3, ou pioche vide ». Retourner
    /// HandReconstructionResult.None évite de recalculer une seconde fois une
    /// intention déjà portée par Step2.
    ///
    /// AVEC Don : Reconstruct est appelé avec cardsRemovedFromHand = 0, car la
    /// carte donnée a déjà été retirée de la main par GameOrchestrator avant cet
    /// appel. Même en phase Luck après un Don, le résultat est toujours None :
    /// c'est géré nativement par le switch de HandReconstructionPolicy.Reconstruct
    /// (case DefPhase.Luck => HandReconstructionResult.None), sans branche
    /// spéciale nécessaire ici.
    /// </summary>
    public static class Step4_FinalDrawResolver
    {
        public static TurnResult Resolve(TurnResult previous, Play play)
        {
            if (previous.State is null) throw new ArgumentNullException(nameof(previous));
            if (play is null) throw new ArgumentNullException(nameof(play));

            if (!previous.RequiresGiftResolution)
                return previous.WithFinalReconstruction(HandReconstructionResult.None);

            var r = HandReconstructionPolicy.Reconstruct(
                previous.State, previous.State.ActivePlayerIndex, cardsRemovedFromHand: 0);

            return previous.WithFinalReconstruction(r);
        }
    }
}