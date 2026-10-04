namespace Psycko.Core.Domain.Log
{
    /// <summary>
    /// Nature d'une action ou d'un événement loggé au cours d'une partie.
    /// Les marqueurs (PhaseChange, DeckExhausted, GameEnded) ne sont pas des
    /// décisions de joueur mais des événements de déroulement de partie.
    /// </summary>
    public enum ActionKind
    {
        Play,
        PickupForced,
        RequestPickup,
        GiftCard,
        BlindPlay,
        PhaseChange,
        DeckExhausted,
        GameEnded
    }
}