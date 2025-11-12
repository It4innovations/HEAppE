namespace SshCaAPI.DTO.JsonTypes
{
    public sealed class CAParam
    {
        public int Ttl { get; set; }
        public Permission[] Permissions { get; set; } = [];
    }
}
