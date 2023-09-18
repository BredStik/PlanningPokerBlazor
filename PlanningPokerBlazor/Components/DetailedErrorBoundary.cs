using Microsoft.AspNetCore.Components.Web;

namespace PlanningPokerBlazor.Components;

public class DetailedErrorBoundary: ErrorBoundary
{
    public new Exception? CurrentException => base.CurrentException;
}