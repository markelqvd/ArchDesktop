using System;
using System.Collections.Generic;

namespace Archaeo.Core
{
    /// Máquina de estados sencilla basada en enum con transiciones permitidas.
    public class GameStateMachine
    {
        public GameState Current { get; private set; } = GameState.Excavating;
        public event Action<GameState, GameState> OnStateChanged; // (anterior, nuevo)

        // Transiciones válidas según el flujo del GDD
        private static readonly Dictionary<GameState, GameState[]> Allowed = new()
        {
            { GameState.Excavating,  new[] { GameState.Discovery, GameState.Museum } },
            { GameState.Discovery,   new[] { GameState.Restoration, GameState.Excavating, GameState.Museum } },
            { GameState.Restoration, new[] { GameState.Excavating, GameState.Museum } },
            { GameState.Museum,      new[] { GameState.Excavating } },
        };

        public bool CanChangeTo(GameState next)
        {
            if (next == Current) return false;
            return Array.IndexOf(Allowed[Current], next) >= 0;
        }

        public bool TryChangeState(GameState next)
        {
            if (!CanChangeTo(next)) return false;
            var prev = Current;
            Current = next;
            OnStateChanged?.Invoke(prev, next);
            return true;
        }
    }
}