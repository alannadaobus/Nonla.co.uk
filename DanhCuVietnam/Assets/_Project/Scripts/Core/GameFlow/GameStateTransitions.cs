using System.Collections.Generic;

namespace DanhCu.Core.GameFlow
{
    /// <summary>
    /// The single source of truth for which top-level state transitions are legal.
    /// Keep this table in sync with the diagram in Docs/TECHNICAL_DESIGN.md.
    /// </summary>
    public static class GameStateTransitions
    {
        private static readonly Dictionary<GameStateId, GameStateId[]> Allowed =
            new Dictionary<GameStateId, GameStateId[]>
            {
                // Boot -> Playing / MainMenu are development shortcuts used when Play is
                // pressed in the editor with a gameplay or menu scene already open.
                { GameStateId.Boot, new[] { GameStateId.Loading, GameStateId.MainMenu, GameStateId.Playing } },
                { GameStateId.MainMenu, new[] { GameStateId.MatchSetup, GameStateId.OnlineLobby } },
                { GameStateId.OnlineLobby, new[] { GameStateId.MatchSetup, GameStateId.MainMenu } },
                { GameStateId.MatchSetup, new[] { GameStateId.Loading, GameStateId.MainMenu, GameStateId.OnlineLobby } },
                { GameStateId.Loading, new[] { GameStateId.MainMenu, GameStateId.Playing } },
                { GameStateId.Playing, new[] { GameStateId.Paused, GameStateId.MatchResult, GameStateId.Loading } },
                { GameStateId.Paused, new[] { GameStateId.Playing, GameStateId.Loading } },
                { GameStateId.MatchResult, new[] { GameStateId.Loading } },
            };

        public static bool IsAllowed(GameStateId from, GameStateId to)
        {
            if (!Allowed.TryGetValue(from, out GameStateId[] targets))
            {
                return false;
            }

            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == to)
                {
                    return true;
                }
            }

            return false;
        }

        public static IReadOnlyList<GameStateId> GetTargets(GameStateId from)
        {
            return Allowed.TryGetValue(from, out GameStateId[] targets)
                ? targets
                : (IReadOnlyList<GameStateId>)System.Array.Empty<GameStateId>();
        }
    }
}
