namespace SshCaAPI.DTO.JsonTypes
{
    public sealed class ConfigResponse
    {
        public bool Fake { get; set; }
        public bool Hide { get; set; }
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string PublicKey { get; set; } = null!;
        public string ConfigEndpoint { get; set; } = null!;
        public string? SSHTemplate { get; set; }
        public string? HTMLTemplate { get; set; }
        public string? DefaultPrincipals { get; set; }
        public string? AuthnContextClassRef { get; set; }
        public bool HashedPrincipal { get; set; }
        public bool MyAccessID { get; set; }
        public string? ScopeCAParams { get; set; }
        public CAParam[] CAParams { get; set; } = [];
    }
}
