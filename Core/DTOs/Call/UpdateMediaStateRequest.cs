namespace Core.DTOs.Call
{
    public class UpdateMediaStateRequest
    {
        public bool IsMuted { get; set; }      // Audio muted?
        public bool IsVideoOff { get; set; }   // Video off?
    }
}
