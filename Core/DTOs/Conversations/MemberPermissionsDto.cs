namespace Core.DTOs.Conversations
{
    public class MemberPermissionsDto
    {
        public bool CanChangeGroupInfo { get; set; }
        public bool CanAddMembers { get; set; }
        public bool CanRemoveMembers { get; set; }
        public bool CanDeleteMessages { get; set; }
        public bool CanPinMessages { get; set; }
        public bool CanChangePermissions { get; set; }
    }
}
