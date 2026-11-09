using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace TarotCardsDeck.Assets
{
    // Shared by the build tool and reader. Encryption discourages casual extraction;
    // the signing private key, which is NEVER shipped, establishes integrity.
    public static class AssetPackageFormat
    {
        public const string Header = "TAROT-ASSETS-1";
        public const int MaxEntryBytes = 32 * 1024 * 1024;
        public static string Hash(byte[] bytes)
        { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
        public static byte[] Read(ZipArchiveEntry entry, int maximum)
        {
            if (entry == null || entry.Length < 0 || entry.Length > maximum) throw new InvalidDataException("Invalid asset entry size.");
            using (var input = entry.Open())
            using (var output = new MemoryStream())
            {
                byte[] buffer = new byte[65536]; int count;
                while ((count = input.Read(buffer,0,buffer.Length)) > 0)
                { if (output.Length + count > maximum) throw new InvalidDataException("Oversized asset entry."); output.Write(buffer,0,count); }
                return output.ToArray();
            }
        }
        public static byte[] Encrypt(byte[] bytes)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = EncryptionKey(); aes.GenerateIV();
                using (var encryptor = aes.CreateEncryptor())
                {
                    byte[] encrypted = encryptor.TransformFinalBlock(bytes,0,bytes.Length);
                    return aes.IV.Concat(encrypted).ToArray();
                }
            }
        }
        public static byte[] Decrypt(byte[] bytes)
        {
            if (bytes.Length < 32) throw new InvalidDataException("Incomplete encrypted asset.");
            using (var aes = Aes.Create())
            {
                aes.Key = EncryptionKey(); aes.IV = bytes.Take(16).ToArray();
                using (var decryptor = aes.CreateDecryptor()) return decryptor.TransformFinalBlock(bytes,16,bytes.Length-16);
            }
        }
        private static byte[] EncryptionKey()
        {
            // An offline client must carry this key. It is not a copy-protection boundary.
            using (var sha = SHA256.Create()) return sha.ComputeHash(Encoding.UTF8.GetBytes("TarotDeck.asset-container.v1.memory-only-images"));
        }
        public static RSACryptoServiceProvider Rsa(string xml)
        {
            var rsa = new RSACryptoServiceProvider(new CspParameters { ProviderType = 24, Flags = CspProviderFlags.CreateEphemeralKey });
            rsa.PersistKeyInCsp = false; rsa.FromXmlString(xml); return rsa;
        }
        public static bool Verify(byte[] bytes, byte[] signature, string publicKey)
        { using (var rsa = Rsa(publicKey)) return rsa.VerifyData(bytes, CryptoConfig.MapNameToOID("SHA256"), signature); }
        public static byte[] Sign(byte[] bytes, string privateKey)
        { using (var rsa = Rsa(privateKey)) return rsa.SignData(bytes, CryptoConfig.MapNameToOID("SHA256")); }
        public static bool SafeName(string name)
        { return !string.IsNullOrEmpty(name) && name != "." && name != ".." && name.IndexOfAny(new[] {'/','\\',':','\t','\r','\n'}) < 0; }
    }

    public sealed class SignedAssetPackage : IDisposable
    {
        private readonly ZipArchive archive;
        private readonly Dictionary<string,string> hashes = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        public string ManifestHash { get; private set; }
        public IEnumerable<string> Names { get { return hashes.Keys; } }
        public SignedAssetPackage(string path, string identity, string publicKey)
        {
            archive = ZipFile.OpenRead(path);
            try
            {
                byte[] manifest = AssetPackageFormat.Read(archive.GetEntry("manifest.txt"), 1024*1024);
                byte[] signature = AssetPackageFormat.Read(archive.GetEntry("manifest.sig"),1024);
                if (!AssetPackageFormat.Verify(manifest,signature,publicKey)) throw new InvalidDataException("Asset package signature is invalid: " + identity);
                ManifestHash = AssetPackageFormat.Hash(manifest);
                string[] lines = Encoding.UTF8.GetString(manifest).Split(new[] {'\n'},StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length < 2 || lines[0] != AssetPackageFormat.Header + "\t" + identity) throw new InvalidDataException("Incorrect asset package identity.");
                foreach (string line in lines.Skip(1))
                {
                    var fields = line.Split('\t');
                    if (fields.Length != 2 || !AssetPackageFormat.SafeName(fields[0]) || fields[1].Length != 64 || hashes.ContainsKey(fields[0]))
                        throw new InvalidDataException("Invalid asset manifest entry.");
                    hashes.Add(fields[0],fields[1]);
                }
                if (archive.Entries.Count != hashes.Count + 2 || archive.Entries.Select(entry=>entry.FullName).Distinct(StringComparer.OrdinalIgnoreCase).Count() != archive.Entries.Count ||
                    hashes.Keys.Any(name=>archive.GetEntry(name+".bin") == null)) throw new InvalidDataException("Asset package entries do not match the signed manifest.");
            }
            catch { archive.Dispose(); throw; }
        }
        public byte[] Read(string name)
        {
            string expected;
            if (!hashes.TryGetValue(name,out expected)) throw new FileNotFoundException("Asset not in signed package: " + name);
            name = hashes.Keys.First(key=>string.Equals(key,name,StringComparison.OrdinalIgnoreCase));
            byte[] encrypted = AssetPackageFormat.Read(archive.GetEntry(name+".bin"),AssetPackageFormat.MaxEntryBytes);
            if (AssetPackageFormat.Hash(encrypted) != expected) throw new InvalidDataException("Asset was modified: " + name);
            return AssetPackageFormat.Decrypt(encrypted);
        }
        public void Dispose() { archive.Dispose(); }
    }
}
