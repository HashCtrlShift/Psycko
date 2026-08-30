namespace Psycko
{
    public struct PowerCard
    {
        public string Id { get; }
        public string PlayerId { get; }
        public PowerCardEffectType EffectType { get; }
        public bool IsUsed { get; set; }

        public PowerCard(string id, string playerId, PowerCardEffectType effectType = PowerCardEffectType.Unknown)
        {
            Id = id;
            PlayerId = playerId;
            EffectType = effectType;
            IsUsed = false;
        }

        public override bool Equals(object obj) => obj is PowerCard other && Id == other.Id;
        public override int GetHashCode() => Id.GetHashCode();
        public override string ToString() => $"PowerCard({Id}, {PlayerId}, {EffectType}, Used={IsUsed})";
    }
}