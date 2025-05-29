// LudoApp.Shared/GameCommunication.cs
using System.Collections.Generic;
using LudoGame.Core; // To use LudoGame.Core.Player, LudoGame.Core.Piece, LudoGame.Core.GamePhase
using System.Linq; // For Select extension method

namespace LudoApp.Shared
{
    // Represents the full state of a Ludo game to be sent to clients
    public class GameStateDto
    {
        public string GameId { get; set; } = string.Empty;
        public List<PlayerDto> Players { get; set; } = new(); // ← Changed to PlayerDto
        public int CurrentPlayerId { get; set; }
        public int DiceRoll { get; set; }
        public GamePhase Phase { get; set; }
        public string? WinnerColor { get; set; }
        public string? Message { get; set; } // Generic message for game events/status updates

        // Constructor to properly map from LudoGame.Core.LudoGame
        public GameStateDto(LudoGame.Core.LudoGame coreGame)
        {
            GameId = coreGame.GameId;

            // ← PROPER MAPPING: Create new DTO objects instead of direct assignment
            Players = coreGame.Players.Select(p => new PlayerDto
            {
                Id = p.Id,
                Username = p.Username,
                Color = p.Color,
                ConnectionId = p.ConnectionId,
                Pieces = p.Pieces.Select(piece => new PieceDto
                {
                    Id = piece.Id,
                    Position = piece.Position,
                    IsAtHome = piece.Position == -1
                }).ToList()
            }).ToList();

            CurrentPlayerId = coreGame.Players.Any() ? coreGame.Players[coreGame.CurrentPlayerIndex].Id : -1; // Handle case where players list might be empty (e.g., initial state)
            DiceRoll = coreGame.DiceRoll;
            Phase = coreGame.Phase;
            WinnerColor = coreGame.IsGameOver() ? coreGame.GetWinner()?.Color : null;
            // Message will be set by the server when emitting the DTO
        }

        public GameStateDto() { } // Parameterless constructor for deserialization
    }

    // DTO classes for player and piece information
    public class PlayerDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string ConnectionId { get; set; } = string.Empty;
        public List<PieceDto> Pieces { get; set; } = new();
    }

    public class PieceDto
    {
        public int Id { get; set; }
        public int Position { get; set; }
        public bool IsAtHome { get; set; }
    }

    // Represents a player joining the matchmaking queue
    public class MatchmakingPlayerInfo
    {
        public string Username { get; set; } = string.Empty;
        // No ConnectionId here, as that's server-side context
        // No PlayerId/Color yet, that's assigned when a game starts
    }

    // Represents a client's request to make a move
    public class ClientMoveDto
    {
        public string GameId { get; set; } = string.Empty;
        public int PieceId { get; set; } // ID of the piece the player wants to move
        // You might need a TargetPosition here later if moves aren't single-choice
    }
}