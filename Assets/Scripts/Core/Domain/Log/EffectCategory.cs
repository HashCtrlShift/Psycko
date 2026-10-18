using System;

namespace Psycko.Core.Domain.Log
{
    /// <summary>
    /// Catégorie d'un effet de log, pour la Présentation (filtrage des animations).
    /// </summary>
    public enum EffectCategory
    {
        /// <summary>Effets des cartes et de la Pile (y compris le Proverbe, lié à l'As).</summary>
        CardPile,

        /// <summary>Effets des joueurs (Don, changement de phase).</summary>
        Player
    }

    public static class EffectKindExtensions
    {
        /// <summary>
        /// Catégorie d'un EffectKind. Lève une exception si un effet n'est pas classé :
        /// tout nouvel EffectKind doit choisir sa catégorie ici.
        /// </summary>
        public static EffectCategory Category(this EffectKind kind)
        {
            switch (kind)
            {
                case EffectKind.Doublon:
                case EffectKind.Detruite2:
                case EffectKind.DetruiteCarre:
                case EffectKind.DetruiteBombe:
                case EffectKind.SensReverse:
                case EffectKind.PriestEffect:
                case EffectKind.JokerVerre:
                case EffectKind.JokerNoir:
                case EffectKind.Proverbe:
                    return EffectCategory.CardPile;

                case EffectKind.Don:
                case EffectKind.PhaseTransition:
                    return EffectCategory.Player;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(kind), kind, "EffectKind sans catégorie.");
            }
        }
    }
}