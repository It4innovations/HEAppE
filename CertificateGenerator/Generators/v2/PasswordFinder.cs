using Org.BouncyCastle.OpenSsl;

namespace HEAppE.CertificateGenerator.Generators.v2;

public class PasswordFinder : IPasswordFinder
{
    private string password;

    public PasswordFinder(string password)
    {
        this.password = password;
    }
    
    public char[] GetPassword()
    {
        return password.ToCharArray();
    }
}