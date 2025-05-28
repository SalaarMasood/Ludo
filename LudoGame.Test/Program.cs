using LudoGame.Core;
using System;

class Program
{
    static void Main()
    {
        var game = new LudoGame.Core.LudoGame();

        game.AddPlayer(new Player(0, "Red", "TestUserRed", "ConnIdRed"));    // Add username and connectionId
        game.AddPlayer(new Player(1, "Blue", "TestUserBlue", "ConnIdBlue"));
        game.AddPlayer(new Player(2, "Green", "TestUserGreen", "ConnIdGreen"));
        game.AddPlayer(new Player(3, "Yellow", "TestUserYellow", "ConnIdYellow"));

        game.StartGame();

        while (!game.IsGameOver())
        {
            var currentPlayer = game.Players[game.CurrentPlayerIndex];
            Console.WriteLine($"\nPlayer {currentPlayer.Color}'s turn.");

            int roll = game.RollDice();
            Console.WriteLine($"Rolled: {roll}");

            if (game.Phase == GamePhase.MovingPiece)
            {
                var validMoves = game.GetValidMoves();

                if (validMoves.Count == 0)
                {
                    Console.WriteLine("No valid moves. Skipping turn.");
                    continue;
                }

                // Pick the first valid piece (for testing)
                var chosenPiece = validMoves[0];
                Console.WriteLine($"Moving piece {chosenPiece.Id} at position {chosenPiece.Position}");

                game.ApplyMove(chosenPiece);

                foreach (var player in game.Players)
                {
                    Console.WriteLine($"{player.Color}: {string.Join(", ", player.Pieces.Select(p => p.Position))}");
                }
            }

            else if (game.Phase == GamePhase.RollingDice)
            {
                Console.WriteLine("No moves, skipping turn...");
            }

        }

        Console.WriteLine($"\nGame Over! Winner: {game.GetWinner().Color}");
    }
}
