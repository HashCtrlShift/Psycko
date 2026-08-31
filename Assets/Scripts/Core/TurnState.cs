namespace Psycko
{
    public class TurnState
    {
        public Player CurrentPlayer { get; set; }
        public int CurrentPlayerIndex { get; set; }
        public GameDirection Direction { get; set; }

        public TurnState(Player currentPlayer, int currentPlayerIndex, GameDirection direction = GameDirection.Clockwise)
        {
            CurrentPlayer = currentPlayer;
            CurrentPlayerIndex = currentPlayerIndex;
            Direction = direction;
        }

        public override string ToString()
        {
            return $"TurnState(Player={CurrentPlayer?.Name}, Index={CurrentPlayerIndex}, Direction={Direction})";
        }
    }
}