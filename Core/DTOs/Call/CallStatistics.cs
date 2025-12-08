namespace Core.DTOs.Call
{
    public class CallStatistics
    {
        public int TotalCalls { get; set; }
        public int CompletedCalls { get; set; }
        public int MissedCalls { get; set; }
        public int RejectedCalls { get; set; }
        public int TotalDurationSeconds { get; set; }
        public double AverageDurationSeconds { get; set; }
        public int VideoCallsCount { get; set; }
        public int AudioCallsCount { get; set; }
    }
}
