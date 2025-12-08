using Core.Enums;

namespace Core.Entities
{
    public class Call
    {
        public int Id { get; set; }
        public string CallId { get; set; } = string.Empty;
        public int InitiatorId { get; set; }
        public int ReceiverId { get; set; }
        public int ConversationId { get; set; }
        public CallType CallType { get; set; } = CallType.Audio;
        public CallStatus Status { get; set; } = CallStatus.Pending;
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public int DurationInSeconds { get; set; }
        public bool IsInitiatorMuted { get; set; }
        public bool IsInitiatorVideoOff { get; set; }
        public bool IsReceiverMuted { get; set; }
        public bool IsReceiverVideoOff { get; set; }

        // Foreign keys
        public User Initiator { get; set; } = null!;
        public User Receiver { get; set; } = null!;
        public Conversations Conversation { get; set; } = null!;
    }
}
