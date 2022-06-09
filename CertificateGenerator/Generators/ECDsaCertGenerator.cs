using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace HEAppE.CertificateGenerator.Generators
{
    /// <summary>
    /// ECDsa Cipher
    /// </summary>
    public class ECDsaCertGenerator : GenericCertGenerator
    {
        #region Instances
        /// <summary>
        /// Curve
        /// </summary>
        private ECCurve _curve = ECCurve.NamedCurves.nistP521;

        /// <summary>
        /// Is allowed convert to PuTTY
        /// </summary>
        private readonly bool _isAllowedConvertToPuTTY = false;
        #endregion
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        public ECDsaCertGenerator()
        {
            _key = ECDsa.Create(_curve);
            _isAllowedConvertToPuTTY = true;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="curve">Curve</param>
        public ECDsaCertGenerator(ECCurve curve)
        {
            _curve = curve;
            _key = ECDsa.Create(curve);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="curveName">Curve name</param>
        public ECDsaCertGenerator(string curveName)
        {
            _curve = ECCurve.CreateFromFriendlyName(curveName);
            if (curveName.Contains("nistP"))
                _isAllowedConvertToPuTTY = true;

            _key = ECDsa.Create(_curve);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="curve">Curve</param>
        /// <param name="comment">Comment</param>
        public ECDsaCertGenerator(ECCurve curve, string comment)
        {
            _curve = curve;
            _key = ECDsa.Create(curve);
            _publicComment = comment;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="curveName">Curve name</param>
        /// <param name="comment">Comment</param>
        public ECDsaCertGenerator(string curveName, string comment)
        {
            _curve = ECCurve.CreateFromFriendlyName(curveName);
            if (curveName.Contains("nistP"))
                _isAllowedConvertToPuTTY = true;

            _key = ECDsa.Create(_curve);
            _publicComment = comment;
        }
        #endregion
        #region Public Methods
        /// <summary>
        /// Re-Generate key
        /// </summary>
        public override void Regenerate()
        {
            _key = ECDsa.Create(_curve);
        }

        /// <summary>
        /// Returns the SSH private key
        /// </summary>
        /// <returns></returns>
        public override string ToPrivateKey()
        {
            var builder = new StringBuilder();
            builder.AppendLine("-----BEGIN EC PRIVATE KEY-----");
            var privateKeyBytes = Convert.ToBase64String(((ECDsa)_key).ExportECPrivateKey()).ToCharArray();
            for (var i = 0; i < privateKeyBytes.Length; i += 64)
            {
                builder.AppendLine(new string(privateKeyBytes, i, Math.Min(64, privateKeyBytes.Length - i)));
            }
            builder.AppendLine("-----END EC PRIVATE KEY-----");
            return builder.ToString();
        }

        /// <summary>
        /// Returns the SSH public key
        /// </summary>
        /// <returns></returns>
        public override string ToPublicKey()
        {
            var builder = new StringBuilder();
            builder.AppendLine("-----BEGIN PUBLIC KEY-----");
            var publicKeyBytes = Convert.ToBase64String(_key.ExportSubjectPublicKeyInfo()).ToCharArray();
            for (var i = 0; i < publicKeyBytes.Length; i += 64)
            {
                builder.AppendLine(new string(publicKeyBytes, i, Math.Min(64, publicKeyBytes.Length - i)));
            }
            builder.AppendLine("-----END PUBLIC KEY-----");
            return builder.ToString();

        }

        /// <summary>
        /// Returns the SSH public key in PuTTY format
        /// </summary>
        /// <returns></returns>
        public override string ToPuTTYPublicKey()
        {
            if (!_isAllowedConvertToPuTTY)
            {
                throw new Exception($"For ECDsa curve: {_curve.CurveType} is not implemented export to PuTTY format!");
            }

            var parameters = ((ECDsa)_key).ExportParameters(false);
            string cipherType = $"ecdsa-sha2-nistp{_key.KeySize}";
            byte[] sshrsaBytes = Encoding.Default.GetBytes(cipherType);
            byte[] curveBytes = Encoding.Default.GetBytes($"nistp{_key.KeySize}");

            string publicBase64Key;
            using (var ms = new MemoryStream())
            {
                ms.Write(ToBytes(sshrsaBytes.Length), 0, 4);
                ms.Write(sshrsaBytes, 0, sshrsaBytes.Length);

                //Curve
                ms.Write(ToBytes(curveBytes.Length), 0, 4);
                ms.Write(curveBytes, 0, curveBytes.Length);

                // Q.X and Q.Y
                var publicParamsLength = parameters.Q.X.Length + parameters.Q.Y.Length + 1;
                ms.Write(ToBytes(publicParamsLength), 0, 4);
                ms.Write(new byte[] { 4 }, 0, 1);

                ms.Write(parameters.Q.X, 0, parameters.Q.X.Length);
                ms.Write(parameters.Q.Y, 0, parameters.Q.Y.Length);

                ms.Flush();
                publicBase64Key = Convert.ToBase64String(ms.ToArray());
            }

            return $"{cipherType} {publicBase64Key} {_publicComment}";
        }
        #endregion
    }
}
