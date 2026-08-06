using Hangfire.Dashboard;

public class AnonymousHangfireFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // Разрешаем доступ абсолютно всем запросам
        return true;
    }
}