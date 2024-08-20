using System.Security.Cryptography.X509Certificates;
using BeybladeTournamentManager.ApiCalls.Challonge.Data;
using BeybladeTournamentManager.Config;
using Challonge.Api;
using Challonge.Objects;
using BeybladeTournamentManager.Helpers;
using BeybladeTournamentManager.Components.Pages.ViewModels;


namespace BeybladeTournamentManager.Helpers
{
    public class LeaderboardHelper
    {
        public async Task<List<Player>> NewPlayers(List<Player> players, List<Player> leaderboard, string code, ChallongeClient client)
        {

            var matches = await client.GetMatchesAsync(code);

            foreach (var match in matches)
            {
                // find players in the match
                var player1 = players.Find(x => x.ChallongeId == match.Player1Id);
                var player2 = players.Find(x => x.ChallongeId == match.Player2Id);

                if (player1 != null)
                {
                    if (match.WinnerId != null && match.WinnerId == player1.ChallongeId)
                    {
                        player1.Wins++;
                        player1.Points++;
                    }
                    else
                    {
                        player1.Losses++;
                    }

                    // Update player in players list
                    var index = players.FindIndex(x => x.ChallongeId == player1.ChallongeId);
                    players[index] = player1;
                }

                if (player2 != null)
                {
                    if (match.WinnerId != null && match.WinnerId == player2.ChallongeId)
                    {
                        player2.Wins++;
                        player2.Points++;
                    }
                    else
                    {
                        player2.Losses++;
                    }

                    var index = players.FindIndex(x => x.ChallongeId == player2.ChallongeId);
                    players[index] = player2;
                }
            }

            // Sort ranks based on points, if points are equal give same rank
            players = players.OrderByDescending(x => x.Points).ThenByDescending(x => x.Wins).ThenBy(x => x.Losses).ToList();

            int lastRank = 0;
            // Set the rank for each player
            for (int i = 0; i < players.Count; i++)
            {

                // if the player has the same points and wins as the previous player give them the same rank
                if (i > 0 && players[i].Points == players[lastRank - 1].Points && players[i].Wins == players[i - 1].Wins)
                {

                    players[i].LeaderboardRank = players[lastRank - 1].LeaderboardRank;
                    lastRank = Convert.ToInt32(players[i].LeaderboardRank);
                }

                else
                {
                    players[i].LeaderboardRank = (lastRank + 1).ToString();
                    lastRank = i + 1;
                }
            }

            return players;
        }

        public async Task UpdatePlayersFromMatch(Match match, ISpreadsheetViewModel spreadsheetViewModel, IPlayersViewModel playersViewModel, AppSettings settings)
        {
            // Get the cached Leaderboard
            // var leaderboard = await spreadsheetViewModel.GetLeaderboard(settings.CurrentTournamentDetails.relatedSheetName);
            //copy playerslist
            var players = playersViewModel.Players;


            // update the leaderboard
            var player1 = players.Find(x => x.ChallongeId == match.Player1Id);
            var p1Score = match.Scores.Select(x => x.PlayerOneScore).FirstOrDefault();
            var player2 = players.Find(x => x.ChallongeId == match.Player2Id);
            var p2Score = match.Scores.Select(x => x.PlayerTwoScore).FirstOrDefault();

            player1.Wins += p1Score;
            player1.Losses += p2Score;
            player1.Points += p1Score;

            player2.Wins += p2Score;
            player2.Losses += p1Score;
            player2.Points += p2Score;

            // Update player in players list
            var index = players.FindIndex(x => x.ChallongeId == player1.ChallongeId);
            players[index] = player1;

            index = players.FindIndex(x => x.ChallongeId == player2.ChallongeId);
            players[index] = player2;

            // Update the leaderboard
            await spreadsheetViewModel.UpdatePlayers(settings.CurrentTournamentDetails.relatedSheetName, players);

            // Sort the sheet
            await spreadsheetViewModel.SortSheet(settings.CurrentTournamentDetails.relatedSheetName);
        }

        public async Task UndoUpdateFromMatch(Match match, ISpreadsheetViewModel spreadsheetViewModel, IPlayersViewModel playersViewModel, AppSettings settings)
        {
            // Get the cached Leaderboard
            var leaderboard = await spreadsheetViewModel.GetLeaderboard(settings.CurrentTournamentDetails.relatedSheetName);
            var players = playersViewModel.Players;


            // update the leaderboard
            var player1 = players.Find(x => x.ChallongeId == match.Player1Id);
            var p1Score = match.Scores.Select(x => x.PlayerOneScore).FirstOrDefault();
            var player2 = players.Find(x => x.ChallongeId == match.Player2Id);
            var p2Score = match.Scores.Select(x => x.PlayerTwoScore).FirstOrDefault();

            if (player1.Wins > 0)
            {
                player1.Wins -= p1Score;
                player1.Losses -= p2Score;
                player1.Points -= p1Score;

            }

            if (player2.Wins > 0)
            {
                player2.Wins -= p2Score;
                player2.Losses -= p1Score;
                player2.Points -= p2Score;
            }

            // Update player in players list
            var index = players.FindIndex(x => x.ChallongeId == player1.ChallongeId);
            players[index] = player1;

            index = players.FindIndex(x => x.ChallongeId == player2.ChallongeId);
            players[index] = player2;

            // Update the leaderboard
            await spreadsheetViewModel.UpdatePlayers(settings.CurrentTournamentDetails.relatedSheetName, players);
            
            // Sort the sheet
            await spreadsheetViewModel.SortSheet(settings.CurrentTournamentDetails.relatedSheetName);
        }
        private List<Player> SortRank(List<Player> players)
        {
            // Sort ranks based on points, if points are equal give same rank
            players = players.OrderByDescending(x => x.Points).ThenByDescending(x => x.Wins).ThenBy(x => x.Losses).ToList();

            int lastRank = 0;
            // Set the rank for each player
            for (int i = 0; i < players.Count; i++)
            {

                // if the player has the same points and wins as the previous player give them the same rank
                if (i > 0 && players[i].Points == players[lastRank - 1].Points && players[i].Wins == players[i - 1].Wins)
                {

                    players[i].LeaderboardRank = players[lastRank - 1].LeaderboardRank;
                    lastRank = Convert.ToInt32(players[i].LeaderboardRank);
                }

                else
                {
                    players[i].LeaderboardRank = (lastRank + 1).ToString();
                    lastRank = i + 1;
                }
            }

            return players;
        }

    }
}