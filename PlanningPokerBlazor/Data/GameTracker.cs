namespace PlanningPokerBlazor.Data;

public class GameTracker: IDisposable, IAsyncDisposable
{
    private const int MAX_NUMBER_OF_GAMES = 50;
    private const int MAX_INACTIVE_MINUTES_BEFORE_REMOVAL = 15;
    
    private readonly ILogger<GameTracker> _logger;
    private List<Data.PokerGame> _games = new();
    private readonly Timer _timer;
    
    public GameTracker(ILogger<GameTracker> logger)
    {
        _logger = logger;
        _timer = new System.Threading.Timer(_ =>
        {
            CheckForExpiredGames();
        }, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }
    
    public void Track(PokerGame game)
    {
        if (_games.Count >= MAX_NUMBER_OF_GAMES)
        {
            throw new ApplicationException("The maximum number of active games has been reached.");
        }
        
        _games.Add(game);
        
        _logger.LogInformation("Game {id} was added to game tracker.  Now tracks {nbGames}", game.Id, _games.Count);
    }

    public void Reset()
    {
        _games = new();
        
        _logger.LogInformation("All games were removed from tracker");
    }

    public PokerGame? GetGame(Guid id)
    {
        return _games.FirstOrDefault(x => x.Id == id);
    }

    private void CheckForExpiredGames()
    {
        var expiredGames = _games.Where(x => x.LastStateChange < DateTime.Now.AddMinutes(MAX_INACTIVE_MINUTES_BEFORE_REMOVAL * -1)).ToArray();

        var removedGames = _games.RemoveAll(x => expiredGames.Contains(x));

        if (removedGames > 0)
        {
            _logger.LogInformation("{nbGames} removed because of inactivity", removedGames);
        }

        foreach (var expiredGame in expiredGames)
        {
            expiredGame.Dispose();
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_timer != null)
        {
            await _timer.DisposeAsync();
        }
    }
}