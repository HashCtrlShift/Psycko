using Psycko.Core.Domain;

namespace Psycko.Core.Interfaces
{
    /// <summary>Contrat commun de mise en forme d'une carte en texte lisible.</summary>
    public interface ICardFormatter
    {
        /// <summary>Ex : "7♥", "Valet♠", "Joker de Verre".</summary>
        string Format(Card card);
    }
}