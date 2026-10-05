namespace Psycko.Core.Domain.Log
{
    /// <summary>Types d'actions enregistrables dans le log d'une partie.</summary>
    public enum ActionKind
    {
        Play = 0,          // [PlayerId] joue [cartes]
        PickupPile = 1,    // [PlayerId] ramasse la Pile (forcé)
        GiftCard = 2,      // [PlayerId] donne [carte] à [PlayerId]
        BlindPlay = 3,     // [PlayerId] retourne [carte]
        RequestPickup = 4  // [PlayerId] ramasse volontairement la Pile
    }
}