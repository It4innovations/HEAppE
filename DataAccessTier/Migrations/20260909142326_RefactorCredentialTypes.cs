using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HEAppE.DataAccessTier.Migrations
{
    /// <inheritdoc />
    public partial class RefactorCredentialTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remap existing AuthenticationType integer values from the legacy enum to the new flag-based enum:
            // -------------------------------------------------------------------------------------------------
            // Legacy Enum:
            //  1 = Password, 2 = PasswordInteractive, 3 = PasswordAndPrivateKey, 4 = PrivateKey
            //  5 = PasswordViaProxy, 6 = PasswordInteractiveViaProxy, 7 = PasswordAndPrivateKeyViaProxy
            //  8 = PrivateKeyViaProxy, 9 = PrivateKeyInSshAgent, 10 = PrivateKeyInVaultAndInSshAgent
            //  11 = SshCertificate, 12 = SshCertificateViaProxy, 13 = Kerberos, 14 = FirecRestIdpViaExpirio
            // -------------------------------------------------------------------------------------------------
            // New Enum:
            //  1 = PasswordInteractive, 2 = Password, 4 = PrivateKey, 6 = PasswordAndPrivateKey (2 | 4)
            //  8 = PrivateKeyInSshAgent, 16 = PrivateKeyInVaultAndInSshAgent, 32 = SshCertificate
            //  64 = Kerberos, 128 = FirecRestIdpViaExpirio
            // (Note: *ViaProxy variants are consolidated into their standard non-proxy counterparts)
            migrationBuilder.Sql(@"
                UPDATE ClusterAuthenticationCredentials
                SET AuthenticationType = CASE AuthenticationType
                    WHEN 1 THEN 2    -- Password -> Password
                    WHEN 5 THEN 2    -- PasswordViaProxy -> Password
                    WHEN 2 THEN 1    -- PasswordInteractive -> PasswordInteractive
                    WHEN 6 THEN 1    -- PasswordInteractiveViaProxy -> PasswordInteractive
                    WHEN 3 THEN 6    -- PasswordAndPrivateKey -> PasswordAndPrivateKey (2 | 4)
                    WHEN 7 THEN 6    -- PasswordAndPrivateKeyViaProxy -> PasswordAndPrivateKey
                    WHEN 4 THEN 4    -- PrivateKey -> PrivateKey
                    WHEN 8 THEN 4    -- PrivateKeyViaProxy -> PrivateKey
                    WHEN 9 THEN 8    -- PrivateKeyInSshAgent -> PrivateKeyInSshAgent
                    WHEN 10 THEN 16  -- PrivateKeyInVaultAndInSshAgent -> PrivateKeyInVaultAndInSshAgent
                    WHEN 11 THEN 32  -- SshCertificate -> SshCertificate
                    WHEN 12 THEN 32  -- SshCertificateViaProxy -> SshCertificate
                    WHEN 13 THEN 64  -- Kerberos -> Kerberos
                    WHEN 14 THEN 128 -- FirecRestIdpViaExpirio -> FirecRestIdpViaExpirio
                    ELSE AuthenticationType
                END;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Roll back the new flag-based enum integer values to legacy enum values:
            migrationBuilder.Sql(@"
                UPDATE ClusterAuthenticationCredentials
                SET AuthenticationType = CASE AuthenticationType
                    WHEN 2 THEN 1    -- Password -> Password
                    WHEN 1 THEN 2    -- PasswordInteractive -> PasswordInteractive
                    WHEN 6 THEN 3    -- PasswordAndPrivateKey -> PasswordAndPrivateKey
                    WHEN 4 THEN 4    -- PrivateKey -> PrivateKey
                    WHEN 8 THEN 9    -- PrivateKeyInSshAgent -> PrivateKeyInSshAgent
                    WHEN 16 THEN 10  -- PrivateKeyInVaultAndInSshAgent -> PrivateKeyInVaultAndInSshAgent
                    WHEN 32 THEN 11  -- SshCertificate -> SshCertificate
                    WHEN 64 THEN 13  -- Kerberos -> Kerberos
                    WHEN 128 THEN 14 -- FirecRestIdpViaExpirio -> FirecRestIdpViaExpirio
                    ELSE AuthenticationType
                END;
            ");
        }
    }
}