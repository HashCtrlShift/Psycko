using Psycko.Core.Domain;
using UnityEngine;

namespace Psycko.Presentation.Interfaces
{
    /// <summary>Contrat de skin : apparence uniquement, aucune logique de jeu.</summary>
    public interface ICardSkin
    {
        string Id { get; }
        string DisplayName { get; }
        Sprite GetFace(Card card);
        Sprite GetBack();
        Color TableColor { get; }
        Color SeatColor { get; }
        Font Font { get; }
    }
}