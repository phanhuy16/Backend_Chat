namespace Core.Enums
{
    public enum CallStatus
    {
        Pending = 0,    // Đợi trả lời
        Answered = 1,   // Đã trả lời
        Rejected = 2,   // Đã từ chối
        Missed = 3,     // Bị bỏ lỡ
        Completed = 4,  // Hoàn thành
        Ended = 5       // Kết thúc
    }
}
