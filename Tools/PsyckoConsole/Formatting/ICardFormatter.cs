using Psycko.Core.Domain;

namespace PsyckoConsole.Formatting
{
    /// <summary>
    /// Contrat commun pour l'affichage textuel d'une Card en console.
    /// </summary>
    public interface ICardFormatter
    {
        string Format(Card card);
    }
}