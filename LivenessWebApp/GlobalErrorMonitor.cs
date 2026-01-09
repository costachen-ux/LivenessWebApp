using System.Collections.Concurrent;

namespace LivenessWebApp;

public class GlobalErrorMonitor
{
    private readonly ConcurrentQueue<bool> _requestHistory = new();
    private const int WindowSize = 100; // 只看最近 100 個請求
    private const double FailureThreshold = 0.3; // 失敗率超過 30% 就判定不健康

    public void RecordRequest(bool isSuccess)
    {
        _requestHistory.Enqueue(isSuccess);
        if (_requestHistory.Count > WindowSize)
        {
            _requestHistory.TryDequeue(out _);
        }
    }

    public bool IsUnstable()
    {
        if (_requestHistory.Count < 20) return false; // 樣本不足時先視為健康

        var failureCount = _requestHistory.Count(x => x == false);
        return (double)failureCount / _requestHistory.Count >= FailureThreshold;
    }
}