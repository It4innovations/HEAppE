namespace SshCaAPI.DTO.JsonTypes
{
    public sealed class Permission
    {
        public CriticalOption[] CriticalOptions { get; set; } = [];
        public Extension[] Extensions { get; set; } = [];
    }
}
