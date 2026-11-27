namespace Psycko.Core.Domain
{
    /// <summary>Effets calculés par Core à partir de son résultat de tour; indicateur de présentation sans règles dupliquées.</summary>
    public readonly struct PlacementInfo
    {
        public bool HasPreviewEffect { get; }
        public bool IsForcedPickup { get; }
        public PlacementInfo(bool hasPreviewEffect, bool isForcedPickup = false)
        { HasPreviewEffect = hasPreviewEffect; IsForcedPickup = isForcedPickup; }
        public static PlacementInfo Plain { get { return new PlacementInfo(false); } }
    }
}
