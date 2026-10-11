namespace Psycko.Core.Domain.Exchange
{
    public enum ExchangeRejectionReason
    {
        None = 0,
        UnknownPlayer,
        PlayerAlreadyReady,
        ExchangeClosed,
        HandCardNotFound,
        FaceUpCardNotFound
    }
}