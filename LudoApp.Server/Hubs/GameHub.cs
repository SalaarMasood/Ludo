// LudoApp.Server/Hubs/GameHub.cs
using Microsoft.AspNetCore.SignalR;
using LudoApp.Server.Services; // For GameManager
using LudoApp.Shared; // For GameStateDto, ClientMoveDto, MatchmakingPlayerInfo
using LudoGame.Core; // For LudoGame, Player, Piece, GamePhase
using System.Security.Claims; // For authentication (optional for bare minimum)

namespace LudoApp.Server.Hubs
{
    public class GameHub : Hub
    {
        private readonly GameManager _gameManager;

        public GameHub(GameManager gameManager)
        {
            _gameManager = gameManager;
        }

        // --- Connection Management ---
        public override async Task OnConnectedAsync()
        {
            // We'll manage connectionId -> username mapping in GameManager when player
            // explicitly says they are looking for a game (FindMatch).
            Console.WriteLine($"[GameHub] Client connected: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            Console.WriteLine($"[GameHub] Client disconnected: {Context.ConnectionId}. Reason: {exception?.Message}");
            // Handle player removal from GameManager (matchmaking queue, active games)
            _gameManager.RemoveConnectedUser(Context.ConnectionId);
            // TODO: Iterate active games and if this player was in one, mark as forfeited
            await base.OnDisconnectedAsync(exception);
        }

        // --- Matchmaking ---
        // Client calls this when a user clicks "Play"
        public async Task FindMatch(string username) // Client sends username now
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                await Clients.Caller.SendAsync("MatchmakingError", "Username is required for matchmaking.");
                return;
            }

            _gameManager.AddConnectedUser(Context.ConnectionId, username); // Add/update connection-username mapping

            LudoGame.Core.LudoGame? newGame = _gameManager.AddPlayerToMatchmaking(Context.ConnectionId, username);

            if (newGame != null)
            {
                // A new game has started! Add all players to a SignalR group for this game.
                foreach (var player in newGame.Players)
                {
                    await Groups.AddToGroupAsync(player.ConnectionId, newGame.GameId);
                }

                // Send the initial game state to all players in the group
                var gameStateDto = new GameStateDto(newGame) { Message = "Match found! Starting game." };
                await Clients.Group(newGame.GameId).SendAsync("GameStarted", gameStateDto);
                Console.WriteLine($"[GameHub] Game {newGame.GameId} started and GameStarted event sent.");
            }
            else
            {
                // Not enough players yet, just inform the client they're waiting
                await Clients.Caller.SendAsync("WaitingForPlayers", username);
                Console.WriteLine($"[GameHub] Player {username} is waiting. Queue size: {_gameManager.WaitingPlayerCount}.");
            }
        }

        public async Task CancelMatchmaking()
        {
            _gameManager.RemovePlayerFromMatchmaking(Context.ConnectionId);
            await Clients.Caller.SendAsync("MatchmakingCancelled");
            Console.WriteLine($"[GameHub] Player {Context.ConnectionId} cancelled matchmaking.");
        }

        public async Task RequestGameState(string gameId)
        {
            var game = _gameManager.GetGame(gameId);
            if (game != null)
            {
                // Send the current game state ONLY to the requesting client (Caller)
                await Clients.Caller.SendAsync("GameStateUpdated", new GameStateDto(game) { Message = "Game state refreshed." });
                Console.WriteLine($"[GameHub] Client {Context.ConnectionId} requested and received game state for {gameId}.");
            }
            else
            {
                // Inform the client if the game doesn't exist (e.g., already ended)
                await Clients.Caller.SendAsync("GameError", "Game not found or already ended.");
                Console.WriteLine($"[GameHub] Client {Context.ConnectionId} requested state for non-existent game {gameId}.");
            }
        }

        // --- In-Game Actions (Will be implemented later) ---
        public async Task RollDice(string gameId)
        {
            var game = _gameManager.GetGame(gameId);
            if (game == null) { await Clients.Caller.SendAsync("GameError", "Game not found."); return; }

            var currentPlayer = game.Players[game.CurrentPlayerIndex];
            if (currentPlayer.ConnectionId != Context.ConnectionId || game.Phase != GamePhase.RollingDice)
            {
                await Clients.Caller.SendAsync("InvalidAction", "It's not your turn or not the rolling phase."); return;
            }

            int roll = game.RollDice();
            Console.WriteLine($"[GameHub] Player {currentPlayer.Username} rolled {roll} in game {gameId}. New phase: {game.Phase}.");

            await Clients.Group(gameId).SendAsync("GameStateUpdated", new GameStateDto(game) { Message = $"{currentPlayer.Username} rolled {roll}." });
        }

        public async Task MakeMove(ClientMoveDto moveDto)
        {
            var game = _gameManager.GetGame(moveDto.GameId);
            if (game == null) { await Clients.Caller.SendAsync("GameError", "Game not found."); return; }

            var currentPlayer = game.Players[game.CurrentPlayerIndex];
            if (currentPlayer.ConnectionId != Context.ConnectionId || game.Phase != GamePhase.MovingPiece)
            {
                await Clients.Caller.SendAsync("InvalidAction", "It's not your turn or not the moving phase."); return;
            }

            var pieceToMove = currentPlayer.Pieces.FirstOrDefault(p => p.Id == moveDto.PieceId);
            if (pieceToMove == null || !game.GetValidMoves().Contains(pieceToMove))
            {
                await Clients.Caller.SendAsync("InvalidAction", "Invalid piece or move."); return;
            }

            // MODIFIED: Call ApplyMove and get the MoveOutcome object
            MoveOutcome outcome = game.ApplyMove(pieceToMove);

            if (outcome.IsSuccessful)
            {
                string message = "";
                if (outcome.IsGameOver)
                {
                    message = $"{outcome.Winner?.Username} Won!";
                }
                else if (outcome.OpponentCaptured)
                {
                    // Specific message for capture
                    message = $"{currentPlayer.Username} captured {outcome.CapturedPlayer?.Username}'s P{outcome.CapturedPiece?.Id}! ";
                    if (outcome.CurrentPlayerGetsAnotherTurn)
                    {
                        message += $"{currentPlayer.Username} gets another turn!";
                    }
                }
                else if (outcome.CurrentPlayerGetsAnotherTurn) // Rolled 6, and no capture
                {
                    message = $"{currentPlayer.Username} moved and rolled a 6! {currentPlayer.Username} gets another turn!";
                }
                else // Normal turn change
                {
                    message = $"{currentPlayer.Username} moved. Next turn: {game.Players[game.CurrentPlayerIndex].Username}.";
                }

                // ... (your existing server-side logging for state AFTER move) ...

                // Decide whether to send GameEnded or GameStateUpdated
                if (outcome.IsGameOver)
                {
                    await Clients.Group(moveDto.GameId).SendAsync("GameEnded", new GameStateDto(game) { Message = message, WinnerColor = outcome.Winner?.Color });
                    _gameManager.RemoveGame(game.GameId);
                }
                else
                {
                    await Clients.Group(moveDto.GameId).SendAsync("GameStateUpdated", new GameStateDto(game) { Message = message });
                }
            }
            else // Move was not successful by ApplyMove (e.g., internal logic error in core game)
            {
                await Clients.Caller.SendAsync("InvalidAction", "The move could not be applied by the server.");
            }
        }


        public async Task ForfeitGame(string gameId)
        {
            var game = _gameManager.GetGame(gameId);
            if (game == null) { await Clients.Caller.SendAsync("GameError", "Game not found."); return; }

            var forfeitingPlayer = game.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
            if (forfeitingPlayer == null) { return; } // Player not in this game

            // Call the new method to end the game gracefully
            game.EndGame(); // This sets Phase = GamePhase.GameOver

            // Determine winner logic for forfeit (this is simplified for now)
            // Remove the forfeiting player temporarily to find who is left.
            var otherPlayers = game.Players.Where(p => p.ConnectionId != Context.ConnectionId).ToList();
            string winnerColor = null;
            if (otherPlayers.Any())
            {
                winnerColor = otherPlayers.First().Color; // Forfeit usually means other players win
                // In a 4-player Ludo, if one forfeits, the others continue or all get a win.
                // For bare minimum, just say the first remaining player wins.
            }

            Console.WriteLine($"[GameHub] Player {forfeitingPlayer.Username} forfeited game {gameId}.");
            await Clients.Group(gameId).SendAsync("GameEnded", new GameStateDto(game) {
                WinnerColor = winnerColor, // Pass the determined winner color
                Message = $"{forfeitingPlayer.Username} forfeited. {winnerColor} wins!"
            });
            _gameManager.RemoveGame(game.GameId); // Remove from active games
        }
    }
}