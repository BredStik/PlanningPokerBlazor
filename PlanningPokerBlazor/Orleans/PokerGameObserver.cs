namespace PlanningPokerBlazor.Orleans;

public class PokerGameObserver: IPokerGameObserver
{
    private readonly Func<PokerGameState, Task> _onStateChanged;

    public PokerGameObserver(Func<PokerGameState, Task> onStateChanged)
    {
        _onStateChanged = onStateChanged;
    }
    
    public async Task OnStateChanged(PokerGameState state)
    {
        await _onStateChanged(state);
    }
}