// LudoApp.Server/Services/GameManager.cs
using System.Collections.Concurrent;
using LudoGame.Core; // For LudoGame, Player, Piece, GamePhase
using LudoApp.Shared; // For GameStateDto, MatchmakingPlayerInfo

namespace LudoApp.Server.Services
{
    public class GameManager
    {
        // Stores active games: GameId -> LudoGame instance
        private ConcurrentDictionary<string, LudoGame.Core.LudoGame> _activeGames = new();

        // Matchmaking queue: ConnectionId -> Player info (e.g., username)
        private ConcurrentQueue<MatchmakingPlayerContext> _waitingPlayers = new();
        public int WaitingPlayerCount => _waitingPlayers.Count;
        private readonly object _matchmakingLock = new object(); // For thread safety

        // Simple mapping from connection ID to username for current players (for demonstration)
        // In a real app, you'd manage authenticated user sessions properly, perhaps with a UserInfo service.
        private ConcurrentDictionary<string, string> _connectedUsers = new(); // ConnectionId -> Username

        public void AddConnectedUser(string connectionId, string username)
        {
            _connectedUsers.TryAdd(connectionId, username);
            Console.WriteLine($"[GameManager] Client connected: {connectionId} (User: {username})");
        }

        public void RemoveConnectedUser(string connectionId)
        {
            _connectedUsers.TryRemove(connectionId, out _);
            Console.WriteLine($"[GameManager] Client disconnected: {connectionId}");
            RemovePlayerFromMatchmaking(connectionId); // Ensure they're removed from queue
            // TODO: For a real game, you'd iterate _activeGames to see if this player was in a game
            // and handle game forfeit/disconnection for that game.
        }

        public string? GetUsernameForConnection(string connectionId)
        {
            _connectedUsers.TryGetValue(connectionId, out var username);
            return username;
        }

        public LudoGame.Core.LudoGame? GetGame(string gameId)
        {
            _activeGames.TryGetValue(gameId, out var game);
            return game;
        }

        public void RemoveGame(string gameId)
        {
            _activeGames.TryRemove(gameId, out _);
            Console.WriteLine($"[GameManager] Game {gameId} removed from active games.");
        }

        // Method to add a player to matchmaking and try to start a game
        public LudoGame.Core.LudoGame? AddPlayerToMatchmaking(string connectionId, string username)
        {
            lock (_matchmakingLock)
            {
                // Add to queue only if not already waiting
                if (!_waitingPlayers.Any(p => p.ConnectionId == connectionId))
                {
                    _waitingPlayers.Enqueue(new MatchmakingPlayerContext(connectionId, username));
                    Console.WriteLine($"[GameManager] Player {username} ({connectionId}) added to matchmaking queue. Current queue size: {_waitingPlayers.Count}");
                }
                else
                {
                     Console.WriteLine($"[GameManager] Player {username} ({connectionId}) is already in matchmaking queue.");
                }

                if (_waitingPlayers.Count >= 4) // We need 4 players for Ludo
                {
                    Console.WriteLine("[GameManager] Enough players found. Attempting to create new game...");
                    var playersToMatch = new List<MatchmakingPlayerContext>();
                    // Try to dequeue 4 players
                    for (int i = 0; i < 4; i++)
                    {
                        if (_waitingPlayers.TryDequeue(out var p))
                        {
                            playersToMatch.Add(p);
                        }
                        else
                        {
                            // If for some reason we can't dequeue enough (e.g., player disconnected between check and dequeue)
                            Console.WriteLine($"[GameManager] Failed to dequeue enough players ({playersToMatch.Count} of 4). Re-queuing...");
                            foreach (var player in playersToMatch) // Re-queue any dequeued players
                            {
                                _waitingPlayers.Enqueue(player);
                            }
                            return null;
                        }
                    }

                    // If we successfully dequeued 4 players, create the game
                    if (playersToMatch.Count == 4)
                    {
                        var newGameId = Guid.NewGuid().ToString();
                        var game = new LudoGame.Core.LudoGame(newGameId);
                        string[] colors = { "Red", "Blue", "Green", "Yellow" }; // Assign fixed colors for now

                        for (int i = 0; i < 4; i++)
                        {
                            // Create LudoGame.Core.Player instances
                            game.AddPlayer(new Player(i, colors[i], playersToMatch[i].Username, playersToMatch[i].ConnectionId));
                        }
                        game.StartGame(); // Set initial phase to RollingDice

                        _activeGames.TryAdd(newGameId, game);
                        Console.WriteLine($"[GameManager] Game {newGameId} started with players: {string.Join(", ", playersToMatch.Select(p => p.Username))}");
                        return game;
                    }
                }
            }
            return null; // Not enough players yet, or failed to dequeue 4
        }

        public void RemovePlayerFromMatchmaking(string connectionId)
        {
            lock (_matchmakingLock)
            {
                // For ConcurrentQueue, removing specific items is tricky.
                // This creates a new queue without the specified connectionId.
                var tempQueue = new ConcurrentQueue<MatchmakingPlayerContext>();
                int removedCount = 0;
                while (_waitingPlayers.TryDequeue(out var player))
                {
                    if (player.ConnectionId != connectionId)
                    {
                        tempQueue.Enqueue(player);
                    }
                    else
                    {
                        removedCount++;
                    }
                }
                _waitingPlayers = tempQueue; // Replace the queue
                Console.WriteLine($"[GameManager] Player {connectionId} removed from matchmaking. Removed: {removedCount}. Current queue size: {_waitingPlayers.Count}");
            }
        }
    }

    // Helper class to store matchmaking player context
    public class MatchmakingPlayerContext
    {
        public string ConnectionId { get; }
        public string Username { get; }

        public MatchmakingPlayerContext(string connectionId, string username)
        {
            ConnectionId = connectionId;
            Username = username;
        }
    }
}