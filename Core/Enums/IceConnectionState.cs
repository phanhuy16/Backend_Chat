namespace Core.Enums
{
    public enum IceConnectionState
    {
        New = 0,           // Mới
        Checking = 1,      // Đang kiểm tra
        Connected = 2,     // Đã kết nối
        Completed = 3,     // Hoàn thành
        Failed = 4,        // Thất bại
        Disconnected = 5,  // Mất kết nối
        Closed = 6         // Đã đóng
    }
}
