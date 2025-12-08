namespace Core.Enums
{
    public enum ConnectionState
    {
        New = 0,           // Mới
        Connecting = 1,    // Đang kết nối
        Connected = 2,     // Đã kết nối
        Disconnected = 3,  // Mất kết nối
        Failed = 4,        // Thất bại
        Closed = 5         // Đã đóng
    }
}
