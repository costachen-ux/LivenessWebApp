namespace LivenessWebApp;

public class ErrorTrackingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly GlobalErrorMonitor _monitor;

    public ErrorTrackingMiddleware(RequestDelegate next, GlobalErrorMonitor monitor)
    {
        _next = next;
        _monitor = monitor;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
            // 只要結果是 5xx，就紀錄為失敗
            bool isSuccess = context.Response.StatusCode < 500;
            _monitor.RecordRequest(isSuccess);
        }
        catch (Exception)
        {
            _monitor.RecordRequest(false); // 發生 Unhandled Exception 一定是失敗
            throw; // 繼續拋出讓 ASP.NET Core 處理成 500
        }
    }
}