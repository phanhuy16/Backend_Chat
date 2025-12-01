

using Core.Enums;

namespace Core.DTOs.Users
{
    public interface UpdateStatusRequest
    {
        public StatusUser Status { get; set; }
    }
}
