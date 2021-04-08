using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

// Usage:
// var keyLines = File.ReadAllLines(@"keyfile");
// var keyBytes = System.Convert.FromBase64String(string.Join("", keyLines.Skip(1).Take(keyLines.Length - 2)));
// var puttyKey = RSAConverter.FromDERPrivateKey(keyBytes).ToPuttyPrivateKey();

namespace HEAppE.CertificateGenerator
{
    public class RSAConverter
    {
        public RSACryptoServiceProvider CryptoServiceProvider { get; private set; }
        public string Comment { get; set; }

        public RSAConverter(RSACryptoServiceProvider cryptoServiceProvider)
        {
            this.CryptoServiceProvider = cryptoServiceProvider;
            this.Comment = "imported-key";
        }

        public static RSAConverter FromDERPrivateKey(byte[] privateKey)
        {
            return new RSAConverter(DecodeRSAPrivateKey(privateKey));
        }

        // Adapted from http://www.jensign.com/opensslkey/opensslkey.cs
        public static RSACryptoServiceProvider DecodeRSAPrivateKey(byte[] privkey)
        {
            var RSA = new RSACryptoServiceProvider();
            var RSAparams = new RSAParameters();

            // ---------  Set up stream to decode the asn.1 encoded RSA private key  ------
            using (BinaryReader binr = new BinaryReader(new MemoryStream(privkey)))
            {
                byte bt = 0;
                ushort twobytes = 0;
                twobytes = binr.ReadUInt16();
                if (twobytes == 0x8130)    //data read as little endian order (actual data order for Sequence is 30 81)
                    binr.ReadByte();    //advance 1 byte
                else if (twobytes == 0x8230)
                    binr.ReadInt16();	//advance 2 bytes
                else
                    throw new Exception("Unexpected value read");

                twobytes = binr.ReadUInt16();
                if (twobytes != 0x0102)	//version number
                    throw new Exception("Unexpected version");

                bt = binr.ReadByte();
                if (bt != 0x00)
                    throw new Exception("Unexpected value read");

                //------  all private key components are Integer sequences ----
                RSAparams.Modulus = binr.ReadBytes(GetIntegerSize(binr));
                RSAparams.Exponent = binr.ReadBytes(GetIntegerSize(binr));
                RSAparams.D = binr.ReadBytes(GetIntegerSize(binr));
                RSAparams.P = binr.ReadBytes(GetIntegerSize(binr));
                RSAparams.Q = binr.ReadBytes(GetIntegerSize(binr));
                RSAparams.DP = binr.ReadBytes(GetIntegerSize(binr));
                RSAparams.DQ = binr.ReadBytes(GetIntegerSize(binr));
                RSAparams.InverseQ = binr.ReadBytes(GetIntegerSize(binr));
            }

            RSA.ImportParameters(RSAparams);
            return RSA;
        }

        public string ToPuttyPrivateKey()
        {
            var publicParameters = this.CryptoServiceProvider.ExportParameters(false);
            byte[] publicBuffer = new byte[3 + 7 + 4 + 1 + publicParameters.Exponent.Length + 4 + 1 + publicParameters.Modulus.Length + 1];

            using (var bw = new BinaryWriter(new MemoryStream(publicBuffer)))
            {
                bw.Write(new byte[] { 0x00, 0x00, 0x00 });
                bw.Write("ssh-rsa");
                PutPrefixed(bw, publicParameters.Exponent, true);
                PutPrefixed(bw, publicParameters.Modulus, true);
            }
            var publicBlob = System.Convert.ToBase64String(publicBuffer);

            var privateParameters = this.CryptoServiceProvider.ExportParameters(true);
            byte[] privateBuffer = new byte[4 + 1 + privateParameters.D.Length + 4 + 1 + privateParameters.P.Length + 4 + 1 + privateParameters.Q.Length + 4 + 1 + privateParameters.InverseQ.Length];

            using (var bw = new BinaryWriter(new MemoryStream(privateBuffer)))
            {
                PutPrefixed(bw, privateParameters.D, true);
                PutPrefixed(bw, privateParameters.P, true);
                PutPrefixed(bw, privateParameters.Q, true);
                PutPrefixed(bw, privateParameters.InverseQ, true);
            }
            var privateBlob = System.Convert.ToBase64String(privateBuffer);

            HMACSHA1 hmacsha1 = new HMACSHA1(new SHA1CryptoServiceProvider().ComputeHash(Encoding.ASCII.GetBytes("putty-private-key-file-mac-key")));
            byte[] bytesToHash = new byte[4 + 7 + 4 + 4 + 4 + this.Comment.Length + 4 + publicBuffer.Length + 4 + privateBuffer.Length];

            using (var bw = new BinaryWriter(new MemoryStream(bytesToHash)))
            {
                PutPrefixed(bw, Encoding.ASCII.GetBytes("ssh-rsa"));
                PutPrefixed(bw, Encoding.ASCII.GetBytes("none"));
                PutPrefixed(bw, Encoding.ASCII.GetBytes(this.Comment));
                PutPrefixed(bw, publicBuffer);
                PutPrefixed(bw, privateBuffer);
            }

            var hash = string.Join("", hmacsha1.ComputeHash(bytesToHash).Select(x => string.Format("{0:x2}", x)));

            var sb = new StringBuilder();
            sb.AppendLine("PuTTY-User-Key-File-2: ssh-rsa");
            sb.AppendLine("Encryption: none");
            sb.AppendLine("Comment: " + this.Comment);

            var publicLines = SpliceText(publicBlob, 64);
            sb.AppendLine("Public-Lines: " + publicLines.Length);
            foreach (var line in publicLines)
            {
                sb.AppendLine(line);
            }

            var privateLines = SpliceText(privateBlob, 64);
            sb.AppendLine("Private-Lines: " + privateLines.Length);
            foreach (var line in privateLines)
            {
                sb.AppendLine(line);
            }

            sb.AppendLine("Private-MAC: " + hash);

            return sb.ToString();
        }

        private static int GetIntegerSize(BinaryReader binr)
        {
            byte bt = 0;
            byte lowbyte = 0x00;
            byte highbyte = 0x00;
            int count = 0;
            bt = binr.ReadByte();
            if (bt != 0x02)		//expect integer
                throw new Exception("Expected integer");
            bt = binr.ReadByte();

            if (bt == 0x81)
            {
                count = binr.ReadByte();	// data size in next byte
            }
            else if (bt == 0x82)
            {
                highbyte = binr.ReadByte();	// data size in next 2 bytes
                lowbyte = binr.ReadByte();
                byte[] modint = { lowbyte, highbyte, 0x00, 0x00 };
                count = BitConverter.ToInt32(modint, 0);
            }
            else
            {
                count = bt;		// we already have the data size
            }

            while (binr.ReadByte() == 0x00)
            {	//remove high order zeros in data
                count -= 1;
            }

            binr.BaseStream.Seek(-1, SeekOrigin.Current);		//last ReadByte wasn't a removed zero, so back up a byte

            return count;
        }

        private static void PutPrefixed(BinaryWriter bw, byte[] bytes, bool addLeadingNull = false)
        {
            bw.Write(BitConverter.GetBytes(bytes.Length + (addLeadingNull ? 1 : 0)).Reverse().ToArray());
            if (addLeadingNull)
                bw.Write(new byte[] { 0x00 });
            bw.Write(bytes);
        }

        // http://stackoverflow.com/questions/7768373/c-sharp-line-break-every-n-characters
        private static string[] SpliceText(string text, int lineLength)
        {
            return Regex.Matches(text, ".{1," + lineLength + "}").Cast<Match>().Select(m => m.Value).ToArray();
        }
    }
}
