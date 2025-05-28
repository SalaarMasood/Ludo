// LudoApp.Client/Services/GameClientService.cs
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Components; // For NavigationManager
using LudoApp.Shared; // For GameStateDto

namespace LudoApp.Client.Services
{
    public class GameClientService : IAsyncDisposable
    {
        private readonly HubConnection _hubConnection;
        private readonly NavigationManager _navigationManager; // Injected for redirecting
        private readonly AuthService _authService; // Injected to get current username

        // Events that Blazor components can subscribe to
        public event Action<string>? OnWaitingForPlayers; // username of the current client
        public event Action<GameStateDto>? OnGameStarted;
        public event Action? OnMatchmakingCancelled;
        public event Action<string>? OnMatchmakingError; // Error message from server
        public event Action<GameStateDto>? OnGameStateUpdated; // For actual gameplay (later)
        public event Action<GameStateDto>? OnGameEnded;       // For game over (later)
        public event Action<string>? OnGameError;             // Generic game errors (later)
        public event Action<string>? OnInvalidAction;         // Invalid move/action (later)
        public string? ConnectionId => _hubConnection.State == HubConnectionState.Connected ? _hubConnection.ConnectionId : null;


        public GameClientService(HubConnection hubConnection, NavigationManager navigationManager, AuthService authService)
        {
            _hubConnection = hubConnection;
            _navigationManager = navigationManager;
            _authService = authService;

            // Subscribe to incoming messages from the server
            _hubConnection.On<string>("WaitingForPlayers", (username) => OnWaitingForPlayers?.Invoke(username));
            _hubConnection.On<GameStateDto>("GameStarted", (gameState) => OnGameStarted?.Invoke(gameState));
            _hubConnection.On("MatchmakingCancelled", () => OnMatchmakingCancelled?.Invoke());
            _hubConnection.On<string>("MatchmakingError", (message) => OnMatchmakingError?.Invoke(message));

            // Events for actual gameplay (will be used later, but set them up now)
            _hubConnection.On<GameStateDto>("GameStateUpdated", (gameState) => OnGameStateUpdated?.Invoke(gameState));
            _hubConnection.On<GameStateDto>("GameEnded", (gameState) => OnGameEnded?.Invoke(gameState));
            _hubConnection.On<string>("GameError", (message) => OnGameError?.Invoke(message));
            _hubConnection.On<string>("InvalidAction", (message) => OnInvalidAction?.Invoke(message));


            // Optional: Handle auto-reconnect logic if needed, e.g., re-joining game groups.
            // For this demo, simply connecting is enough.
            _hubConnection.Closed += async (exception) =>
            {
                Console.WriteLine($"[GameClientService] Connection closed: {exception?.Message}");
                // Potentially show a "connection lost" message to the user
            };
        }

        // Starts the SignalR connection if not already connected
        public async Task StartConnectionAsync()
        {
            if (_hubConnection.State == HubConnectionState.Disconnected)
            {
                try
                {
                    await _hubConnection.StartAsync();
                    Console.WriteLine("[GameClientService] SignalR connection started.");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[GameClientService] Error starting SignalR connection: {ex.Message}");
                    // You might want to notify UI here (e.g., "Could not connect to game server")
                }
            }
        }

        public async Task RequestGameStateAsync(string gameId)
        {
            // Ensure connection is established before sending a message
            await StartConnectionAsync();
            await _hubConnection.SendAsync("RequestGameState", gameId);
        }

        // Client method to request matchmaking
        public async Task FindMatchAsync()
        {
            await StartConnectionAsync(); // Ensure connection is established
            // Get username from AuthService (which gets it from localStorage)
            string? username = _authService.Username;
            if (string.IsNullOrEmpty(username))
            {
                // This scenario should ideally not happen if "Play" button is only visible when logged in.
                Console.Error.WriteLine("[GameClientService] Cannot find match: User not logged in.");
                _navigationManager.NavigateTo("/login"); // Redirect to login
                return;
            }
            await _hubConnection.SendAsync("FindMatch", username);
        }

        // Client method to cancel matchmaking
        public async Task CancelMatchmakingAsync()
        {
            await _hubConnection.SendAsync("CancelMatchmaking");
        }

        // Client methods for in-game actions (placeholders for now)
        public async Task RollDiceAsync(string gameId)
        {
            await _hubConnection.SendAsync("RollDice", gameId);
        }

        public async Task MakeMoveAsync(ClientMoveDto moveDto)
        {
            await _hubConnection.SendAsync("MakeMove", moveDto);
        }

        public async Task ForfeitGameAsync(string gameId)
        {
            await _hubConnection.SendAsync("ForfeitGame", gameId);
        }

        // Dispose method to properly clean up the SignalR connection
        public async ValueTask DisposeAsync()
        {
            if (_hubConnection is not null)
            {
                await _hubConnection.DisposeAsync();
            }
        }
    }
}