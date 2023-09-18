namespace PlanningPokerBlazor.Data;

public class PokerGame: IDisposable
{
    private const int MAX_NUMBER_OF_PLAYERS = 10;
    private readonly ILogger _logger;
    public Guid Id { get; }
    public List<Player> Players { get; } = new();
    public event EventHandler<EventArgs> StateChanged;
    public bool Disposed { get; set; }

    public PokerGame(Guid id, ILogger logger)
    {
        _logger = logger;
        Id = id;
        LastStateChange = DateTime.Now;
    }

    public DateTime? LastStateChange { get; private set; }
    
    private void ChangeState()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
        LastStateChange = DateTime.Now;
    }

    public void AddPlayer(Player player)
    {
        if (Players.Count == MAX_NUMBER_OF_PLAYERS)
        {
            throw new ApplicationException($"The maximum number of players has been reached ({MAX_NUMBER_OF_PLAYERS})");
        }
        
        if (player.IsHost && Players.Any(x => x.IsHost))
        {
            throw new ApplicationException("Only one host");
        }
        
        if (Players.Any(x => x.DisplayName == player.DisplayName))
        {
            throw new ApplicationException("A player with the same name already exists");
        }
        
        Players.Add(player);
        _logger.LogInformation("Player {name} was added to game {id}", player.DisplayName, Id);

        ChangeState();
    }

    public void Reset()
    {
        foreach (Player p in Players)
        {
            p.ResetCard();
        }

        CardState = CardState.Hidden;
        
        _logger.LogInformation("Game {id} was reset", Id);
        
        ChangeState();
    }

    public void ShowCards()
    {
        if (!Players.All(x => x.ChosenCard.HasValue))
        {
            throw new ApplicationException("Not all players have chosen a card");
        }

        CardState = CardState.Shown;
        
        _logger.LogInformation("Cards were shown in game {id}", Id);
        
        ChangeState();
    }
    
    public CardState CardState { get; private set; }

    public void PlayerChoosesCard(Player player, int card)
    {
        player?.ChooseCard(card);
        
        _logger.LogInformation("Player {name} chose card {card} in game {id}", player?.DisplayName, card, Id);
        
        ChangeState();
    }

    public void PlayerChangesName(Player player, string newName)
    {
        if (Players.Any(x => x.DisplayName == newName))
        {
            throw new ApplicationException("A player with the same name already exists");
        }

        player.Rename(newName);
        
        ChangeState();
    }

    public void Dispose()
    {
        // TODO release managed resources here
        Disposed = true;
        ChangeState();
    }
}

public class Player
{
    private readonly PokerGame _game;
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Player(string displayName, PokerGame game, bool isHost = false)
    {
        _game = game;
        DisplayName = displayName;
        IsHost = isHost;
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

public enum CardState
{
    Hidden,
    Shown
}