// LudoApp.Client/Services/AuthService.cs
using Microsoft.AspNetCore.Components; // For NavigationManager
using Microsoft.JSInterop;            // For localStorage interaction

namespace LudoApp.Client.Services
{
    public class AuthService
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly NavigationManager _navigationManager;

        // Private fields to hold the current authentication state
        private string? _username;
        private bool _isLoggedIn;

        // Public properties to access the state
        public string? Username => _username;
        public bool IsLoggedIn => _isLoggedIn;

        // Event to notify components when auth state changes
        public event Action? AuthStateChanged;

        // Constructor with dependency injection for IJSRuntime and NavigationManager
        public AuthService(IJSRuntime jsRuntime, NavigationManager navigationManager)
        {
            _jsRuntime = jsRuntime;
            _navigationManager = navigationManager;
        }

        // Initializes the service, typically called once on app startup (e.g., in MainLayout.razor)
        public async Task InitializeAsync()
        {
            // Try to retrieve username from localStorage
            _username = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "username");
            _isLoggedIn = !string.IsNullOrEmpty(_username);
            // Notify any listeners that state has been initialized
            NotifyAuthStateChanged();
        }

        // Called when a user successfully logs in
        public async Task LoginAsync(string username)
        {
            _username = username;
            _isLoggedIn = true;
            // Store username in localStorage for persistence
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "username", username);
            NotifyAuthStateChanged(); // Notify listeners
        }

        // Called when a user logs out
        public async Task LogoutAsync()
        {
            _username = null;
            _isLoggedIn = false;
            // Remove username from localStorage
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "username");
            NotifyAuthStateChanged(); // Notify listeners
            _navigationManager.NavigateTo("/", forceLoad: true); // Redirect to home/login page and force reload to ensure all state is reset
        }

        // Helper method to invoke the event
        private void NotifyAuthStateChanged() => AuthStateChanged?.Invoke();
    }
}