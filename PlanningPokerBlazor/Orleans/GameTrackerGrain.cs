using Orleans;
using Orleans.Providers;

namespace PlanningPokerBlazor.Orleans;

public interface IGameTrackerGrain : IGrainWithStringKey, IDisposable
{
    Task Track(Guid gameId);
    Task Reset();
}

[StorageProvider(ProviderName="games")]
public class GameTrackerGrain: Grain<GameTrackerState>, IGameTrackerGrain
{
    private const int MAX_NUMBER_OF_GAMES = 50;
    private const int MAX_INACTIVE_MINUTES_BEFORE_REMOVAL = 15;
    
    private readonly ILogger<GameTrackerGrain> _logger;
    private readonly IGrainFactory _grainFactory;
    private readonly IServiceProvider _serviceProvider;
    private IDisposable _timer;
    
    public GameTrackerGrain(ILogger<GameTrackerGrain> logger, IGrainFactory grainFactory, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _grainFactory = grainFactory;
        _serviceProvider = serviceProvider;
        
    }

    public override Task OnActivateAsync()
    {
        _timer = RegisterTimer(async (state) =>
        {
            await CheckForExpiredGames();
        }, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        
        return base.OnActivateAsync();
    }

    public async Task Track(Guid gameId)
    {
        if (State.GameIds.Count >= MAX_NUMBER_OF_GAMES)
        {
            throw new ApplicationException("The maximum number of active games has been reached.");
        }
        
        State.GameIds.Add(gameId);

        await WriteStateAsync();
        
        _logger.LogInformation("Game {id} was added to game tracker.  Now tracks {nbGames}", gameId, State.GameIds.Count);
    }

    public async Task Reset()
    {
        //todo: actually clear all games

        var gameIds = State.GameIds.ToArray();
        
        foreach (var gameId in gameIds)
        {
            var game = _grainFactory.GetGrain<IPokerGameGrain>(gameId);
            
            await game.DisposeAsync();

            State.GameIds.Remove(gameId);
        }
        
        await WriteStateAsync();
        
        _logger.LogInformation("All games were removed from tracker");
    }

    /*public PokerGame? GetGame(Guid id)
    {
        return _games.FirstOrDefault(x => x.Id == id);
    }*/

    private async Task CheckForExpiredGames()
    {
        var removedGameIds = new List<Guid>();
        
        foreach (var gameId in State.GameIds)
        {
            var game = _grainFactory.GetGrain<IPokerGameGrain>(gameId);

            if (await game.GetLastStateChange() < DateTime.Now.AddMinutes(MAX_INACTIVE_MINUTES_BEFORE_REMOVAL * -1))
            {
                //todo: remove game
                
                await game.DisposeAsync();
                
                removedGameIds.Add(gameId);
            }
        }

        if (removedGameIds.Count > 0)
        {
            _logger.LogInformation("{nbGames} removed because of inactivity", removedGameIds.Count);
        }
    }

    
    public void Dispose()
    {
        _timer?.Dispose();
    }
}


[Serializable]
public record GameTrackerState
{
    public List<Guid> GameIds { get; set; } = new();
}