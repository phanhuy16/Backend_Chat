namespace Core.DTOs.ChatHub
{
    public class IceCandidateDto
    {
        public string CallId { get; set; } = string.Empty;
        public object Candidate { get; set; } = null!; // RTCIceCandidate
    }
}
