using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
//using System.Management.Automation;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace HEAppE.CertificateGenerator {
	public class CertificateGenerator {
		private StringBuilder puttyPrivateKey;
		private StringBuilder puttyPublicKey;

		//private StringBuilder PEMPrivateKey;

		public RSACryptoServiceProvider CryptoServiceProvider { get; private set; }
		public string Comment { get; set; }

		/// <summary>
		///   Create self-signedCertificate
		/// </summary>
		/// <param name="cn"></param>
		/// <param name="nameofFile"></param>
		//public void CreateCertificate(string cn, string nameofFile) {
		//	string hash = "";

		//	using (PowerShell powerShellInstance = PowerShell.Create()) {
		//		// use "AddScript" to add the contents of a script file to the end of the execution pipeline.
		//		// use "AddCommand" to add individual commands/cmdlets to the end of the execution pipeline.
		//		powerShellInstance.AddScript("New-SelfSignedCertificate -certstorelocation cert:\\localmachine\\my -dnsname " + cn);

		//		// invoke execution on the pipeline (collecting output)
		//		Collection<PSObject> psOutput = powerShellInstance.Invoke();

		//		foreach (PSObject outputItem in psOutput) {
		//			// if null object was dumped to the pipeline during the script then a null
		//			// object may be present here. check for null to prevent potential NRE.
		//			if (outputItem != null) {
		//				//TODO: do something with the output item 
		//				//Console.WriteLine(outputItem.BaseObject.GetType().FullName);
		//				//Console.WriteLine(outputItem.BaseObject + "\n");
		//				hash = outputItem.Members["thumbprint"].Value.ToString();
		//				Console.WriteLine(hash);
		//			}
		//			break;
		//		}
		//	}
		//	using (PowerShell powerShellInstance = PowerShell.Create()) {
		//		powerShellInstance.AddScript("$pwd = ConvertTo-SecureString -String \"vawer#rad4634a6dsfa\" -Force -AsPlainText");
		//		powerShellInstance.AddScript("Export-PfxCertificate -cert cert:\\localMachine\\my\\" + hash + " -FilePath " + nameofFile + ".pfx -Password $pwd");
		//		Collection<PSObject> psOutput = powerShellInstance.Invoke();

		//		foreach (PSObject outputItem in psOutput) {
		//			// if null object was dumped to the pipeline during the script then a null
		//			// object may be present here. check for null to prevent potential NRE.
		//			if (outputItem != null) {
		//				//TODO: do something with the output item 
		//				Console.WriteLine(outputItem.BaseObject.GetType().FullName);
		//				Console.WriteLine(outputItem.BaseObject + "\n");
		//			}
		//		}
		//	}
		//}

		/// <summary>
		///   Generate new key pair - public/private key
		/// </summary>
		/// <param name="size"> Size of key</param>
		public void GenerateKey(int size) {
			this.CryptoServiceProvider = new RSACryptoServiceProvider(size);
			this.Comment = "imported-key";
		}

		/// <summary>
		///   Convert key to Putty
		/// </summary>
		/// <returns></returns>
		private string ToPuttyPrivateKey() {
			var publicParameters = this.CryptoServiceProvider.ExportParameters(false);
			byte[] publicBuffer = new byte[3 + 7 + 4 + 1 + publicParameters.Exponent.Length + 4 + 1 + publicParameters.Modulus.Length + 1];
			using (var bw = new BinaryWriter(new MemoryStream(publicBuffer))) {
				bw.Write(new byte[] {0x00, 0x00, 0x00});
				bw.Write("ssh-rsa");
				PutPrefixed(bw, publicParameters.Exponent, true);
				PutPrefixed(bw, publicParameters.Modulus, true);
			}
			var publicBlob = Convert.ToBase64String(publicBuffer);
			var privateParameters = this.CryptoServiceProvider.ExportParameters(true);
			byte[] privateBuffer =
				new byte[
					4 + 1 + privateParameters.D.Length + 4 + 1 + privateParameters.P.Length + 4 + 1 + privateParameters.Q.Length + 4 + 1 + privateParameters.InverseQ.Length];
			using (var bw = new BinaryWriter(new MemoryStream(privateBuffer))) {
				PutPrefixed(bw, privateParameters.D, true);
				PutPrefixed(bw, privateParameters.P, true);
				PutPrefixed(bw, privateParameters.Q, true);
				PutPrefixed(bw, privateParameters.InverseQ, true);
			}
			var privateBlob = Convert.ToBase64String(privateBuffer);
			HMACSHA1 hmacsha1 = new HMACSHA1(new SHA1CryptoServiceProvider().ComputeHash(Encoding.ASCII.GetBytes("putty-private-key-file-mac-key")));
			byte[] bytesToHash = new byte[4 + 7 + 4 + 4 + 4 + this.Comment.Length + 4 + publicBuffer.Length + 4 + privateBuffer.Length];
			using (var bw = new BinaryWriter(new MemoryStream(bytesToHash))) {
				PutPrefixed(bw, Encoding.ASCII.GetBytes("ssh-rsa"));
				PutPrefixed(bw, Encoding.ASCII.GetBytes("none"));
				PutPrefixed(bw, Encoding.ASCII.GetBytes(this.Comment));
				PutPrefixed(bw, publicBuffer);
				PutPrefixed(bw, privateBuffer);
			}
			var hash = string.Join("", hmacsha1.ComputeHash(bytesToHash).Select(x => string.Format("{0:x2}", x)));
			var sb = new StringBuilder();
			puttyPublicKey = new StringBuilder();
			puttyPrivateKey = new StringBuilder();

			sb.AppendLine("PuTTY-User-Key-File-2: ssh-rsa");
			sb.AppendLine("Encryption: none");
			sb.AppendLine("Comment: " + this.Comment);
			var publicLines = SpliceText(publicBlob, 64);
			sb.AppendLine("Public-Lines: " + publicLines.Length);
			foreach (var line in publicLines) {
				sb.AppendLine(line);
				puttyPublicKey.AppendLine(line);
			}
			var privateLines = SpliceText(privateBlob, 64);
			sb.AppendLine("Private-Lines: " + privateLines.Length);
			foreach (var line in privateLines) {
				sb.AppendLine(line);
				puttyPrivateKey.AppendLine(line);
			}
			sb.AppendLine("Private-MAC: " + hash);
			return sb.ToString();
		}

		private static int GetIntegerSize(BinaryReader binr) {
			byte bt = 0;
			byte lowbyte = 0x00;
			byte highbyte = 0x00;
			int count = 0;
			bt = binr.ReadByte();
			if (bt != 0x02) //expect integer
				throw new Exception("Expected integer");
			bt = binr.ReadByte();
			if (bt == 0x81) {
				count = binr.ReadByte(); // data size in next byte
			}
			else if (bt == 0x82) {
				highbyte = binr.ReadByte(); // data size in next 2 bytes
				lowbyte = binr.ReadByte();
				byte[] modint = {lowbyte, highbyte, 0x00, 0x00};
				count = BitConverter.ToInt32(modint, 0);
			}
			else {
				count = bt; // we already have the data size
			}
			while (binr.ReadByte() == 0x00) {
				//remove high order zeros in data
				count -= 1;
			}
			binr.BaseStream.Seek(-1, SeekOrigin.Current); //last ReadByte wasn't a removed zero, so back up a byte
			return count;
		}

		private static void PutPrefixed(BinaryWriter bw, byte[] bytes, bool addLeadingNull = false) {
			bw.Write(BitConverter.GetBytes(bytes.Length + (addLeadingNull ? 1 : 0)).Reverse().ToArray());
			if (addLeadingNull)
				bw.Write(new byte[] {0x00});
			bw.Write(bytes);
		}

		private static string[] SpliceText(string text, int lineLength) {
			return Regex.Matches(text, ".{1," + lineLength + "}").Cast<Match>().Select(m => m.Value).ToArray();
		}

		/// <summary>
		///   Save private key in PEM formate
		/// </summary>
		/// <param name="file">Name of file</param>
		public void ExportToPemPrivateFile(string file) {
			if (CryptoServiceProvider == null)
				throw new Exception("Missing key");
			using (TextWriter writer = File.CreateText(file)) {
				writer.WriteLine(ExportPrivateKey());
			}
		}

		/// <summary>
		///   Save public key in PEM formate
		/// </summary>
		/// <param name="file">Name of file</param>
		public void ExportToPemPublicFile(string file) {
			if (CryptoServiceProvider == null)
				throw new Exception("Missing key");

			using (TextWriter writer = File.CreateText(file)) {
				ExportPublicKeyToPemFormat(writer);
			}
		}

		/// <summary>
		///   Save key in putty formate -ppk
		/// </summary>
		/// <param name="file">Name of File</param>
		public void ExportToPuttyFile(string file) {
			if (CryptoServiceProvider == null)
				throw new Exception("Missing key");
			using (TextWriter writer = File.CreateText(file)) {
				writer.Write(ToPuttyPrivateKey());
			}
		}

		public string ExportToPutty() {
			if (CryptoServiceProvider == null)
				throw new Exception("Missing key");
			return ToPuttyPrivateKey();
		}

		public string ExportToPuttyPublicKey() {
			if (CryptoServiceProvider == null)
				throw new Exception("Missing key");
			ToPuttyPrivateKey();
			return puttyPublicKey.ToString();
		}

		public string ExportToPuttyPrivateKey() {
			if (CryptoServiceProvider == null)
				throw new Exception("Missing key");
			ToPuttyPrivateKey();
			return puttyPrivateKey.ToString();
		}

		public string DhiPublicKey() {
			return ExportToPuttyPublicKey();
		}

		public string DhiPrivateKey() {
			if (CryptoServiceProvider == null)
				throw new Exception("Missing key");
			return ExportPrivateKey();
		}

		private string ExportPrivateKey() //TextWriter outputStream)
		{
			StringBuilder pemPrivateKey = new StringBuilder();
			if (CryptoServiceProvider.PublicOnly) throw new ArgumentException("CSP does not contain a private key", "csp");
			var parameters = CryptoServiceProvider.ExportParameters(true);
			using (var stream = new MemoryStream()) {
				var writer = new BinaryWriter(stream);
				writer.Write((byte) 0x30); // SEQUENCE
				using (var innerStream = new MemoryStream()) {
					var innerWriter = new BinaryWriter(innerStream);
					EncodeIntegerBigEndian(innerWriter, new byte[] {0x00}); // Version
					EncodeIntegerBigEndian(innerWriter, parameters.Modulus);
					EncodeIntegerBigEndian(innerWriter, parameters.Exponent);
					EncodeIntegerBigEndian(innerWriter, parameters.D);
					EncodeIntegerBigEndian(innerWriter, parameters.P);
					EncodeIntegerBigEndian(innerWriter, parameters.Q);
					EncodeIntegerBigEndian(innerWriter, parameters.DP);
					EncodeIntegerBigEndian(innerWriter, parameters.DQ);
					EncodeIntegerBigEndian(innerWriter, parameters.InverseQ);
					var length = (int) innerStream.Length;
					EncodeLength(writer, length);
					writer.Write(innerStream.GetBuffer(), 0, length);
				}

				var base64 = Convert.ToBase64String(stream.GetBuffer(), 0, (int) stream.Length).ToCharArray();
				//            outputStream.WriteLine("-----BEGIN RSA PRIVATE KEY-----");
				pemPrivateKey.AppendLine("-----BEGIN RSA PRIVATE KEY-----");
				// Output as Base64 with lines chopped at 64 characters
				for (var i = 0; i < base64.Length; i += 64) {
					//                  outputStream.WriteLine(base64, i, Math.Min(64, base64.Length - i));
					pemPrivateKey.AppendLine(new String(base64, i, Math.Min(64, base64.Length - i)));
				}
				//            outputStream.WriteLine("-----END RSA PRIVATE KEY-----");
				pemPrivateKey.AppendLine("-----END RSA PRIVATE KEY-----");
			}
			return pemPrivateKey.ToString();
		}

		private String ExportPublicKeyToPemFormat(TextWriter outputStream) {
			var parameters = CryptoServiceProvider.ExportParameters(false);
			using (var stream = new MemoryStream()) {
				var writer = new BinaryWriter(stream);
				writer.Write((byte) 0x30); // SEQUENCE
				using (var innerStream = new MemoryStream()) {
					var innerWriter = new BinaryWriter(innerStream);
					EncodeIntegerBigEndian(innerWriter, new byte[] {0x00}); // Version
					EncodeIntegerBigEndian(innerWriter, parameters.Modulus);
					EncodeIntegerBigEndian(innerWriter, parameters.Exponent);

					//All Parameter Must Have Value so Set Other Parameter Value Whit Invalid Data  (for keeping Key Structure  use "parameters.Exponent" value for invalid data)
					EncodeIntegerBigEndian(innerWriter, parameters.Exponent); // instead of parameters.D
					EncodeIntegerBigEndian(innerWriter, parameters.Exponent); // instead of parameters.P
					EncodeIntegerBigEndian(innerWriter, parameters.Exponent); // instead of parameters.Q
					EncodeIntegerBigEndian(innerWriter, parameters.Exponent); // instead of parameters.DP
					EncodeIntegerBigEndian(innerWriter, parameters.Exponent); // instead of parameters.DQ
					EncodeIntegerBigEndian(innerWriter, parameters.Exponent); // instead of parameters.InverseQ

					var length = (int) innerStream.Length;
					EncodeLength(writer, length);
					writer.Write(innerStream.GetBuffer(), 0, length);
				}

				var base64 = Convert.ToBase64String(stream.GetBuffer(), 0, (int) stream.Length).ToCharArray();
				outputStream.WriteLine("-----BEGIN PUBLIC KEY-----");
				// Output as Base64 with lines chopped at 64 characters
				for (var i = 0; i < base64.Length; i += 64) {
					outputStream.WriteLine(base64, i, Math.Min(64, base64.Length - i));
				}
				outputStream.WriteLine("-----END PUBLIC KEY-----");

				return outputStream.ToString();
			}
		}

		private static void EncodeLength(BinaryWriter stream, int length) {
			if (length < 0) throw new ArgumentOutOfRangeException("length", "Length must be non-negative");
			if (length < 0x80) {
				// Short form
				stream.Write((byte) length);
			}
			else {
				// Long form
				var temp = length;
				var bytesRequired = 0;
				while (temp > 0) {
					temp >>= 8;
					bytesRequired++;
				}
				stream.Write((byte) (bytesRequired | 0x80));
				for (var i = bytesRequired - 1; i >= 0; i--) {
					stream.Write((byte) (length >> (8*i) & 0xff));
				}
			}
		}

		public string GetXML(bool withPrivateKey)
		{
			//Console.WriteLine("######################");
			//byte[] aar = CryptoServiceProvider.ExportParameters(false).Modulus;
			//for (int i = 0; i < aar.Length; i++)
			//{
			//    Console.Write(aar[i]);
			//}
			//Console.WriteLine();
			//Console.WriteLine("######################");
			return CryptoServiceProvider.ToXmlString(withPrivateKey);
		}

		public void LoadFromPemPrivateKey(string privateKey, bool IsBase64)
		{
			if (!IsBase64)
				CryptoServiceProvider = DecodeRSAPrivateKey(Convert.FromBase64String(privateKey));
			else
				CryptoServiceProvider = DecodeRSAPrivateKey(Encoding.Unicode.GetBytes(privateKey));
		}
		public void LoadFromPemPublicKey(string publicKey, bool IsBase64)
		{

			if (!IsBase64)
				CryptoServiceProvider = DecodeRSAPublicKey(Convert.FromBase64String(publicKey));
			else
				CryptoServiceProvider = DecodeRSAPublicKey(Encoding.Unicode.GetBytes(publicKey));
		}

		private uint swap(uint num)
		{
			return ((num >> 24) & 0xff) | // move byte 3 to byte 0
					((num << 8) & 0xff0000) | // move byte 1 to byte 2
					((num >> 8) & 0xff00) | // move byte 2 to byte 1
					((num << 24) & 0xff000000 // byte 0 to byte 3
					);
		}

		private RSACryptoServiceProvider DecodeRSAPublicKey(byte[] pubkey)
		{

			var RSA = new RSACryptoServiceProvider();

			var RSAparams = new RSAParameters();

			// ---------  Set up stream to decode the asn.1 encoded RSA private key  ------

			using (BinaryReader binr = new BinaryReader(new MemoryStream(pubkey)))
			{

				uint fourbytes = 0;

				fourbytes = swap(binr.ReadUInt32());

				if (fourbytes == 0x00000007)    //data read as little endian order (actual data order for Sequence is 30 81)

					binr.ReadBytes(Convert.ToInt32(fourbytes));

				else

					throw new Exception("Unexpected value read");



				uint lenExpo = 0;
				lenExpo = swap(binr.ReadUInt32());
				RSAparams.Exponent = binr.ReadBytes(Convert.ToInt32(lenExpo));



				uint lenMod = 0;
				lenMod = swap(binr.ReadUInt32());
				byte[] array = binr.ReadBytes(Convert.ToInt32(lenMod));
				int number = 0;
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i] == 0x00)
						number++;
					else
						break;
				}
				RSAparams.Modulus = new byte[array.Length - number];
				Array.Copy(array, number, RSAparams.Modulus, 0, RSAparams.Modulus.Length);
			}



			RSA.ImportParameters(RSAparams);

			return RSA;

		}

		private RSACryptoServiceProvider DecodeRSAPrivateKey(byte[] privkey)
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

		private static void EncodeIntegerBigEndian(BinaryWriter stream, byte[] value, bool forceUnsigned = true) {
			stream.Write((byte) 0x02); // INTEGER
			var prefixZeros = 0;
			for (var i = 0; i < value.Length; i++) {
				if (value[i] != 0) break;
				prefixZeros++;
			}
			if (value.Length - prefixZeros == 0) {
				EncodeLength(stream, 1);
				stream.Write((byte) 0);
			}
			else {
				if (forceUnsigned && value[prefixZeros] > 0x7f) {
					// Add a prefix zero to force unsigned if the MSB is 1
					EncodeLength(stream, value.Length - prefixZeros + 1);
					stream.Write((byte) 0);
				}
				else {
					EncodeLength(stream, value.Length - prefixZeros);
				}
				for (var i = prefixZeros; i < value.Length; i++) {
					stream.Write(value[i]);
				}
			}
		}
	}
}