using System;
using System.Collections.Generic;
using System.Linq;
using Psycko.Core.Domain;
using Psycko.Core.Domain.Log;

namespace Psycko.Core.Services.Log
{
    /// <summary>
    /// Accumulateur de compteurs d'une partie (une instance par partie).
    /// Side-effect pur, optionnel dans GameOrchestrator (null = coût nul),
    /// indépendant du recorder : fonctionne en LogMode.Off.
    ///
    /// Définitions :
    ///  - Coup global : Play, BlindPlay ou ramassage (le Don n'en est pas un).
    ///  - Play : ApplyPlay / ApplyBlindPlay acceptés (le ramassage n'est pas un Play).
    ///  - Coup de sortie : valeur du compteur global quand le siège passe Finished.
    /// </summary>
    public sealed class GameResultTracker
    {
        private readonly int _seed;
        private readonly int[] _exitMove;
        private readonly int[] _playsAtExit;
        private readonly int[] _plays;
        private readonly int[] _pickups;
        private readonly bool[] _finished;
        private readonly List<int> _ranking = new List<int>();
        private int _totalMoves;
        private int _totalPlays;

        public GameResultTracker(int seed, int playerCount)
        {
            if (playerCount <= 0) throw new ArgumentOutOfRangeException(nameof(playerCount));
             _seed = seed;
             _exitMove = Enumerable.Repeat(-1, playerCount).ToArray();
             _playsAtExit = Enumerable.Repeat(-1, playerCount).ToArray();
             _plays = new int[playerCount];
             _pickups = new int[playerCount];
             _finished = new bool[playerCount];
        }

        /// <summary>Un Play accepté (ApplyPlay ou ApplyBlindPlay) : +1 Play, +1 coup global.</summary>
        public void RecordPlay(int seatIndex)
        {
            _plays[seatIndex]++;
            _totalPlays++;
            _totalMoves++;
        }

        /// <summary>Un ramassage : +1 ramassage, +1 coup global, 0 Play.</summary>
        public void RecordPickup(int seatIndex)
        {
            _pickups[seatIndex]++;
            _totalMoves++;
        }

        /// <summary>
        /// À appeler après chaque résolution : tout siège nouvellement Finished reçoit
        /// le rang suivant (ordre des sièges en cas de sorties simultanées).
        /// Couvre aussi la sortie sur un Don (aucun compteur incrémenté par le Don).
        /// </summary>
        public void ObserveState(GameState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));

            for (int i = 0; i < _finished.Length; i++)
            {
                if (_finished[i] || state.Players[i].CurrentPhase != DefPhase.Finished)
                    continue;

                _finished[i] = true;
                _exitMove[i] = _totalMoves;
                _playsAtExit[i] = _plays[i];
                _ranking.Add(i);
            }
        }

        /// <summary>
        /// Construit le résultat à la fin de partie : le dernier siège non Finished
        /// (le Psycko) est ajouté en dernière position du classement.
        /// </summary>
        public GameResult Build(GameState finalState)
        {
            if (finalState == null) throw new ArgumentNullException(nameof(finalState));

            ObserveState(finalState);

            var ranking = new List<int>(_ranking);
            for (int i = 0; i < _finished.Length; i++)
            {
                if (!_finished[i])
                    ranking.Add(i);
            }

            return new GameResult(_seed, _totalMoves, _totalPlays, ranking,
                _exitMove, _playsAtExit, _plays, _pickups);
        }
    }
}