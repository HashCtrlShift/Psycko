using Psycko.Core.Domain;

namespace Psycko.Core.Rules.Detection
{
    /// <summary>
    /// Proverbe : « Finir sur un As, c'est la classe ! ».
    /// Vrai si le coup posé est un As RÉEL (jamais un Joker) ET que ce coup
    /// déclenche une transition de phase du poseur (y compris son passage en Finished).
    /// Lecture seule : ne décide rien, ne modifie aucun état.
    /// </summary>
    public static class ProverbDetection
    {
        public static bool IsProverb(Play play, bool triggersPhaseTransition)
        {
            if (play == null)
                throw new System.ArgumentNullException(nameof(play));

            return triggersPhaseTransition
                && !play.IsJokerPlay
                && play.EffectiveRank == DefRank.Ace;
        }
    }
}