
## 🛠️ Setup Instructions

### Prerequisites

*   [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
*   [MongoDB Server](https://www.mongodb.com/try/download/community) installed and running.
    *   **Important:** Ensure your MongoDB server (`mongod.exe`) is running on `mongodb://localhost:27017/`. If not, configure `appsettings.json` accordingly.
    *   By default, MongoDB expects a data directory at `C:\data\db`. If this folder does not exist, create it manually.

### Installation

1.  **Clone the repository:**
    ```bash
    git clone [Your-Repo-URL-Here]
    cd [Your-Project-Folder-Name]
    ```
2.  **Restore NuGet packages for all projects:**
    ```bash
    dotnet restore
    ```
3.  **Configure MongoDB Connection:**
    *   Open `LudoApp.Server/appsettings.json`.
    *   Verify or set your MongoDB connection string:
        ```json
        "MongoDB": {
          "ConnectionString": "mongodb://localhost:27017",
          "DatabaseName": "LudoUsersDb",
          "UsersCollectionName": "Users"
        }
        ```

## 🚀 How to Run

You will need at least **two separate terminal windows** (or more if you want to test the full 4-player game simultaneously without opening new browser tabs).

1.  **Start MongoDB Server:**
    *   Open a **new terminal window**.
    *   Navigate to your MongoDB `bin` directory (e.g., `cd "C:\Program Files\MongoDB\Server\8.0\bin"`).
    *   Start the MongoDB daemon:
        ```bash
        mongod.exe --dbpath C:\data\db
        ```
    *   **Keep this window open and running.** It should display "waiting for connections on port 27017".

2.  **Start the Backend Server:**
    *   Open a **new terminal window**.
    *   Navigate to the `LudoApp.Server` directory:
        ```bash
        cd LudoApp.Server
        ```
    *   Run the server:
        ```bash
        dotnet run
        ```
    *   **Important:** If you make changes to `LudoApp.Server`, `LudoApp.Shared`, or `LudoGame.Core`, you MUST stop this server (Ctrl+C) and restart it after rebuilding.

3.  **Start the Frontend Client:**
    *   Open a **new terminal window**.
    *   Navigate to the `LudoApp.Client` directory:
        ```bash
        cd LudoApp.Client
        ```
    *   Run the client:
        ```bash
        dotnet run
        ```
    *   Your browser should automatically open to `http://localhost:5260` (or another assigned port).

## 🎮 How to Play

1.  **Access the Application:** Open your web browser and navigate to `http://localhost:5260` (or the URL provided by the client's `dotnet run` command).
2.  **Sign Up:** Create a new user account via the "Sign Up" page if you don't have one.
3.  **Login:** Log in with your new (or existing) user credentials. Your login status will be saved locally.
4.  **Start Matchmaking:** From the "Home" page, click the "Play" button. You will be redirected to the waiting room.
5.  **Multiplayer Test:** To test a full 4-player game:
    *   Open **three more browser tabs/windows**.
    *   In each new tab, go to `http://localhost:5260` and **log in with a different user account**. (You may need to quickly sign up 3 more users).
    *   Click "Play" in each of these new tabs.
6.  **Game Start:** Once the 4th player clicks "Play", all 4 browser tabs will automatically redirect to the game board.
7.  **Gameplay:**
    *   The game is turn-based. The current player's turn will be indicated.
    *   Click the "Roll Dice" button when it's your turn.
    *   If you have valid moves, pieces will become clickable. Click a piece to move it.
    *   Observe piece positions updating in real-time across all players' screens.
    *   Special messages will appear for events like dice rolls, turn changes, and piece captures.
    *   The game ends when a player gets all pieces home.
    *   You can forfeit a game using the "Forfeit Game" button.
