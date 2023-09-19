using Orleans;
using Orleans.Providers;

namespace PlanningPokerBlazor.Orleans;

public interface IPokerGameGrain : IGrainWithGuidKey, IAsyncDisposable
{
    Task<DateTime?> GetLastStateChange();
    Task AddPlayer(Player player);
    Task Reset();
    Task ShowCards();
    Task PlayerChoosesCard(Guid playerId, int card);
    Task PlayerChangesName(Player player, string newName);
    /*Task RegisterEventHandler(EventHandler<EventArgs> handler);
    Task UnregisterEventHandler(EventHandler<EventArgs> handler);*/

    Task Subscribe(IPokerGameObserver observer);
    Task Unsubscribe(IPokerGameObserver observer);

    Task<PokerGameState> GetState();
}

public interface IPokerGameObserver: IGrainObserver
{
    Task OnStateChanged(PokerGameState state);
}

[StorageProvider(ProviderName="games")]
public class PokerGameGrain: Grain<PokerGameState>, IPokerGameGrain
{
    private const int MAX_NUMBER_OF_PLAYERS = 10;
    private readonly ILogger _logger;
    private HashSet<IPokerGameObserver> _observers = new();

    public async Task<PokerGameState> GetState()
    {
        return State;
    }

    public async Task<bool> IsDisposed()
    {
        return State.Disposed;
    }

    public PokerGameGrain(ILogger<PokerGameGrain> logger)
    {
        _logger = logger;
    }

    public override async Task OnActivateAsync()
    {
        State.LastStateChange = DateTime.Now;
        State.Id = this.GetPrimaryKey();

        await WriteStateAsync();
        
        await base.OnActivateAsync();
    }

    private async Task ChangeState()
    {
        State.LastStateChange = DateTime.Now;

        await WriteStateAsync();

        foreach (var observer in _observers)
        {
            await observer.OnStateChanged(State);
        }
    }

    public async Task AddPlayer(Player player)
    {
        if (State.Players.Count == MAX_NUMBER_OF_PLAYERS)
        {
            throw new ApplicationException($"The maximum number of players has been reached ({MAX_NUMBER_OF_PLAYERS})");
        }
        
        if (State.Players.Count == 0)
        {
            player.IsHost = true;
        }
        
        if (player.IsHost && State.Players.Any(x => x.IsHost))
        {
            throw new ApplicationException("Only one host");
        }
        
        if (State.Players.Any(x => x.DisplayName == player.DisplayName))
        {
            throw new ApplicationException("A player with the same name already exists");
        }
        
        State.Players.Add(player);
        
        _logger.LogInformation("Player {name} was added to game {id}", player.DisplayName, this.IdentityString);

        await ChangeState();
    }

    public async Task Reset()
    {
        foreach (Player p in State.Players)
        {
            p.ResetCard();
        }

        State.CardState = CardState.Hidden;
        
        _logger.LogInformation("Game {id} was reset", this.IdentityString);
        
        await ChangeState();
    }

    public async Task ShowCards()
    {
        if (!State.Players.All(x => x.ChosenCard.HasValue))
        {
            throw new ApplicationException("Not all players have chosen a card");
        }

        State.CardState = CardState.Shown;
        
        _logger.LogInformation("Cards were shown in game {id}", this.IdentityString);
        
        await ChangeState();
    }

    public async Task PlayerChoosesCard(Guid playerId, int card)
    {
        var player = State.Players.FirstOrDefault(x => x.Id == playerId);
        
        player?.ChooseCard(card);
        
        _logger.LogInformation("Player {name} chose card {card} in game {id}", player?.DisplayName, card, this.IdentityString);
        
        await ChangeState();
    }

    public async Task PlayerChangesName(Player player, string newName)
    {
        if (State.Players.Any(x => x.DisplayName == newName))
        {
            throw new ApplicationException("A player with the same name already exists");
        }

        player.Rename(newName);
        
        await ChangeState();
    }

    public async Task Subscribe(IPokerGameObserver observer)
    {
        _observers.Add(observer);
    }
    
    public async Task Unsubscribe(IPokerGameObserver observer)
    {
        _observers.Remove(observer);
    }

    public async ValueTask DisposeAsync()
    {
        // TODO release managed resources here
        State.Disposed = true;
        await ChangeState();

        await ClearStateAsync();
        DeactivateOnIdle();
    }

    public async Task<DateTime?> GetLastStateChange()
    {
        return State.LastStateChange;
    }
}

[Serializable]
public class PokerGameState
{
    public List<Player> Players { get; } = new();
    public CardState CardState { get; set; }
    public DateTime? LastStateChange { get; set; }
    public bool Disposed { get; set; }
    public Guid Id { get; set; }
}

[Serializable]
public class Player
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Player(string displayName)
    {
        DisplayName = displayName;
    }
    
    public string DisplayName { get; set; }
    public int? ChosenCard { get; set; }
    public bool IsHost { get; set; }

    public void ChooseCard(int card)
    {
        ChosenCard = card;
    }

    public void ResetCard()
    {
        ChosenCard = null;
    }

    public void Rename(string newName)
    {
        DisplayName = newName;
    }
}

[Serializable]
public enum CardState
{
    Hidden,
    Shown
}