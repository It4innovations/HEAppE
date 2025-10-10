namespace SshCaAPI.DTO.JsonTypes
{
    public sealed class SignRequest
    {
        public string PublicKey { get; set; } = null!;
        public string Ott { get; set; } = null!;
        public string Resource { get; set; } = null!;
    }
}
