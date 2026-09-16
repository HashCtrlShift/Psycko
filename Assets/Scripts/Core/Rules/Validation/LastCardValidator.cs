using System;
using Psycko.Core.Domain;
using Psycko.Core.Interfaces;

namespace Psycko.Core.Rules.Validation
{
    /// <summary>
    /// Valide la règle : interdiction de terminer une phase sur un (ou plusieurs) 2.
    /// Cette règle s'applique dans les 3 phases (Travail, Talent, Chance).
    /// </summary>
    public static class LastCardValidator
    {
        /// <summary>
        /// Détermine si ce coup termine le joueur ET contient un ou plusieurs 2.
        ///
        /// Retourne true si et seulement si :
        /// 1. Le coup est un coup de 2 (EffectiveRank == Two). Un Joker a un
        ///    EffectiveRank null : un Joker de Verre en dernière carte n'est donc
        ///    jamais concerné, conformément à CLAUDE.md.
        /// 2. Après ce coup, le joueur n'a plus aucune carte en main,
        ///    Face Découverte, ni Face Cachée.
        ///
        /// Consommateur : TurnManager — avant d'appliquer les effets du 2, vérifier
        /// cette condition et forcer un ramassage si elle est vraie.
        /// </summary>
        public static bool TerminatesOnTwo(Play play, Player player, IGameStateQuery state)
        {
            if (play is null) throw new ArgumentNullException(nameof(play));
            if (player is null) throw new ArgumentNullException(nameof(player));

            return IsTwoPlay(play) && TerminatesAfterPlay(play, player);
        }

        /// <summary>
        /// Un coup est un coup de 2 si sa hauteur effective est Two.
        /// Play.Create garantit l'homogénéité : soit N cartes de même hauteur,
        /// soit UN Joker (EffectiveRank null). Aucun mélange possible.
        /// </summary>
        private static bool IsTwoPlay(Play play)
            => play.EffectiveRank == DefRank.Two;

        /// <summary>
        /// Simule l'état du joueur après le coup en retirant Count cartes de la
        /// couche d'origine (Play.SourceLayer), puis vérifie que les trois couches
        /// sont vides.
        ///
        /// On s'appuie sur SourceLayer et non sur une comparaison d'identité :
        /// Card est un record (égalité par valeur), donc Hand.Contains(card)
        /// produirait des faux positifs sur des cartes identiques situées dans
        /// une autre couche.
        /// </summary>
        private static bool TerminatesAfterPlay(Play play, Player player)
        {
            int handAfter = player.Hand.Count;
            int faceUpAfter = player.FaceUp.Count;
            int faceDownAfter = player.FaceDown.Count;

            switch (play.SourceLayer)
            {
                case CardLayer.Hand:
                    handAfter -= play.Count;
                    break;
                case CardLayer.FaceUp:
                    faceUpAfter -= play.Count;
                    break;
                case CardLayer.FaceDown:
                    faceDownAfter -= play.Count;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(play),
                        play.SourceLayer,
                        "CardLayer non supporté.");
            }

            return handAfter <= 0 && faceUpAfter <= 0 && faceDownAfter <= 0;
        }
    }
}