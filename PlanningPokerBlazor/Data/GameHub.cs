using Microsoft.AspNetCore.SignalR;

namespace PlanningPokerBlazor.Data;

public class GameHub: Hub
{
    public async Task JoinGame(string user)
    {
        await Clients.All.SendAsync("GameJoined", user);
    }

    public async Task ChooseCard(string user, int card)
    {
        await Clients.All.SendAsync("CardChosen", user, card);
    }
    
    public async Task RevealCards()
    {
        await Clients.All.SendAsync("CardsRevealed");
    }

    public async Task ResetGame()
    {
        await Clients.All.SendAsync("GameReset");
    }
}