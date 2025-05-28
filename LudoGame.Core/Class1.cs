using System;
using System.Collections.Generic;
using System.Linq;

namespace LudoGame.Core
{
    public enum GamePhase
    {
        WaitingForPlayers,
        RollingDice,
        MovingPiece,
        GameOver
    }

    public class Player
    {
        public int Id { get; }
        public string Color { get; }
        public string Username { get; }
        public string ConnectionId { get; } 

        public List<Piece> Pieces { get; }

        public Player(int id, string color, string username, string connectionId)
        {
            Id = id;
            Color = color;
            Username = username;
            ConnectionId = connectionId;
            Pieces = new List<Piece>();
            
            for (int i = 0; i < 4; i++)
                Pieces.Add(new Piece(i, color));
        }

        public bool HasAllPiecesHome() => Pieces.All(p => p.Position == 58);
    }

    public class Piece
    {
        public int Id { get; }
        public string Color { get; }
        public int Position { get; set; } // -1 = base, 0–51 = board, 52–57 = home path, 58 = home

        public Piece(int id, string color)
        {
            Id = id;
            Color = color;
            Position = -1;
        }

        public bool IsAtBase => Position == -1;
        public bool IsAtHome => Position == 58;
    }

    public static class Board
    {
        public static HashSet<int> SafeSpots = new() { 0, 8, 13, 21, 26, 34, 39, 47 };

        public static Dictionary<string, int> EntryPoints = new()
        {
            { "Red", 0 },
            { "Blue", 13 },
            { "Green", 26 },
            { "Yellow", 39 }
        };

        public static Dictionary<string, int> HomeEntry = new()
        {
            { "Red", 51 },
            { "Blue", 12 },
            { "Green", 25 },
            { "Yellow", 38 }
        };
    }


    public class MoveOutcome
    {
        public bool IsSuccessful { get; set; }
        public bool OpponentCaptured { get; set; }
        public Player? CapturedPlayer { get; set; } // The player whose piece was captured
        public Piece? CapturedPiece { get; set; }  // The specific piece captured
        public bool CurrentPlayerGetsAnotherTurn { get; set; } // True if player rolled 6 or captured
        public bool IsGameOver { get; set; }
        public Player? Winner { get; set; }
    }
    

    public class LudoGame
    {
        public string GameId { get; }
        public List<Player> Players { get; private set; } = new();
        public int CurrentPlayerIndex { get; private set; } = 0;
        public int DiceRoll { get; private set; }
        public GamePhase Phase { get; private set; } = GamePhase.WaitingForPlayers;
        private Random rng = new();

        public LudoGame(string gameId)
        {
            GameId = gameId;
        }

        public LudoGame() { GameId = Guid.NewGuid().ToString(); }

        public void AddPlayer(Player player)
        {
            if (Players.Count < 4)
            {
                Players.Add(player);
            }
        }

        public void StartGame()
        {
            if (Players.Count == 4)
            {
                Phase = GamePhase.RollingDice;
                CurrentPlayerIndex = 0;
            }
        }

        public int RollDice()
        {
            if (Phase != GamePhase.RollingDice)
                throw new InvalidOperationException("Not in dice roll phase.");

            DiceRoll = rng.Next(1, 7); // roll dice

            // immediately after rolling, check if player has valid moves
            var player = Players[CurrentPlayerIndex];
            List<Piece> possibleMoves = new List<Piece>();
            foreach (var piece in player.Pieces)
            {
                if (CanMovePiece(piece, DiceRoll)) 
                {
                    possibleMoves.Add(piece);
                }
            }

            if (possibleMoves.Any())
            {
                Phase = GamePhase.MovingPiece;
            }
            else
            {
                // no valid moves, skip turn
                CurrentPlayerIndex = (CurrentPlayerIndex + 1) % Players.Count;
                Phase = GamePhase.RollingDice; //
            }

            return DiceRoll;
        }
        public List<Piece> GetValidMoves()
        {
            var player = Players[CurrentPlayerIndex];
            var valid = new List<Piece>();

            foreach (var piece in player.Pieces)
            {
                if (CanMovePiece(piece, DiceRoll))
                    valid.Add(piece);
            }

            return valid;
        }

        private bool CanMovePiece(Piece piece, int roll)
        {
            // at 58, cannot move
            if (piece.IsAtHome)
            {
                return false;
            }

            // at -1, needs 6
            if (piece.IsAtBase)
            {
                return roll == 6;
            }

            // b/w 52-57
            if (piece.Position >= 52 && piece.Position <= 57)
            {
                int potentialNewPosition = piece.Position + roll;
                return potentialNewPosition <= 58;
            }

            // b/w 0-51
            if (piece.Position >= 0 && piece.Position <= 51)
            {
                int playerEntryPoint = Board.EntryPoints[piece.Color];
                int playerHomeEntrySpot = Board.HomeEntry[piece.Color];

                int stepsTakenOnMainBoard = (piece.Position - playerEntryPoint + 52) % 52;
                int potentialStepsOnMainBoard = stepsTakenOnMainBoard + roll;

                // We enter the home path
                if (potentialStepsOnMainBoard > 51)
                {
                    int stepsIntoHomePath = potentialStepsOnMainBoard - 51; 
                    int finalHomePathPosition = 52 + stepsIntoHomePath - 1; 

                    return finalHomePathPosition <= 58;
                }
                // We stay on the main board
                else
                {
                    int finalMainBoardPos = (piece.Position + roll) % 52;

                    // RULE (CAN MODIFY LATER): Cannot land on own piece on the main board
                    if (Players[CurrentPlayerIndex].Pieces.Any(p => p != piece && p.Position == finalMainBoardPos))
                    {
                        return false;
                    }

                    return true;
                }
            }

            return false; // should not be reached 
        }

        public MoveOutcome ApplyMove(Piece piece)
        {
            // --- MOVED DECLARATIONS TO TOP OF METHOD ---
            bool capturedOpponent = false;
            Player? capturedPlayer = null;
            Piece? capturedPiece = null;
            // ------------------------------------------

            var outcome = new MoveOutcome { IsSuccessful = false }; // Default to failure

            // Initial checks for phase and valid move
            if (Phase != GamePhase.MovingPiece)
            {
                return outcome; // Returns failure outcome
            }

            if (!CanMovePiece(piece, DiceRoll))
            {
                return outcome; // Returns failure outcome
            }

            // --- ONLY SET SUCCESSFUL TO TRUE AFTER ALL INITIAL CHECKS PASS ---
            outcome.IsSuccessful = true; // Move is now considered conceptually successful before details
            // -----------------------------------------------------------------

            int playerEntryPoint = Board.EntryPoints[piece.Color];

            if (piece.IsAtBase)
            {
                piece.Position = playerEntryPoint;
            }
            else // piece is already on the board or home path
            {
                if (piece.Position >= 52 && piece.Position <= 57) // already on home path
                {
                    piece.Position += DiceRoll;
                }
                else // on main board (0-51)
                {
                    int stepsTakenOnMainBoard = (piece.Position - playerEntryPoint + 52) % 52;

                    // Check if entering home path
                    if (stepsTakenOnMainBoard + DiceRoll > 51)
                    {
                        int stepsIntoHomePath = (stepsTakenOnMainBoard + DiceRoll) - 51;
                        piece.Position = 52 + stepsIntoHomePath - 1;
                    }
                    else // Staying on main board
                    {
                        int newMainBoardPos = (piece.Position + DiceRoll) % 52;

                        var potentialOpponentPiece = Players
                            .Where(p => p.Color != piece.Color)
                            .SelectMany(p => p.Pieces)
                            .FirstOrDefault(p => p.Position == newMainBoardPos && !Board.SafeSpots.Contains(newMainBoardPos));

                        if (potentialOpponentPiece != null)
                        {
                            // A capture occurred!
                            capturedOpponent = true;
                            capturedPiece = potentialOpponentPiece;
                            // FINDING CAPTURED PLAYER: This line is now safe as capturedPiece is declared at method scope
                            capturedPlayer = Players.First(p => p.Pieces.Contains(capturedPiece));
                            potentialOpponentPiece.Position = -1; // Send opponent to base
                        }

                        piece.Position = newMainBoardPos; // Update piece position after capture check
                    }
                }
            }

            // --- Populate the MoveOutcome object after all logic ---
            outcome.OpponentCaptured = capturedOpponent;
            outcome.CapturedPlayer = capturedPlayer; // Now safe
            outcome.CapturedPiece = capturedPiece;   // Now safe

            // Check for win condition immediately after the move
            if (Players[CurrentPlayerIndex].HasAllPiecesHome())
            {
                Phase = GamePhase.GameOver;
                outcome.IsGameOver = true;
                outcome.Winner = GetWinner();
            }

            // Determine if current player gets another turn (based on roll or capture)
            if (DiceRoll == 6 || capturedOpponent)
            {
                Phase = GamePhase.RollingDice; // Keep turn for current player
                outcome.CurrentPlayerGetsAnotherTurn = true;
            }
            else // Normal turn change
            {
                CurrentPlayerIndex = (CurrentPlayerIndex + 1) % Players.Count;
                Phase = GamePhase.RollingDice; // Switch turn to next player
                outcome.CurrentPlayerGetsAnotherTurn = false;
            }

            return outcome; // Return the fully populated outcome
        }

        public bool IsGameOver() => Phase == GamePhase.GameOver;

        public Player GetWinner()
        {
            return Players.FirstOrDefault(p => p.HasAllPiecesHome());
        }

        public void EndGame(string? winnerColor = null)
        {
            Phase = GamePhase.GameOver;
        }
    }
}
