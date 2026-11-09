using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TarotCardsDeck.Assets;

class AssetPackager
{
    static string KeyPath { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"TarotDeck","BuildKeys","asset-signing.dpapi"); } }
    static int Main(string[] args)
    {
        try
        {
            if(args.Length==2 && args[0]=="initialize") { Initialize(args[1]); return 0; }
            if(args.Length!=3) throw new ArgumentException("Usage: AssetPackager initialize public-key.xml | source-images output-assets public-key.xml");
            string source=Path.GetFullPath(args[0]), output=Path.GetFullPath(args[1]);
            string privateKey=ReadPrivateKey(), publicKey=File.ReadAllText(args[2]);
            using(var rsa=AssetPackageFormat.Rsa(privateKey))
                if(rsa.ToXmlString(false)!=publicKey.Trim()) throw new InvalidDataException("Signing key does not match the application's public key.");
            Directory.CreateDirectory(output);
            var catalog=new StringBuilder();
            foreach(string folder in Directory.GetDirectories(source).OrderBy(path=>path,StringComparer.Ordinal))
            {
                string id=Path.GetFileName(folder);
                if(!AssetPackageFormat.SafeName(id)) throw new InvalidDataException("Invalid deck folder name.");
                var files=Directory.GetFiles(folder).OrderBy(path=>path,StringComparer.Ordinal).ToArray();
                if(id!="cardbacks")
                {
                    var ids=Enumerable.Range(1,22).Select(i=>"maj"+i.ToString("00")).Concat(new[]{"cups","pents","swords","wands"}.SelectMany(suit=>Enumerable.Range(1,14).Select(i=>suit+i.ToString("00"))));
                    var fronts=files.Where(IsImage).Select(Path.GetFileNameWithoutExtension).ToArray();
                    if(fronts.Length!=78 || !ids.All(card=>fronts.Contains(card,StringComparer.OrdinalIgnoreCase))) throw new InvalidDataException("Incomplete front deck: " + id);
                }
                string target=Path.Combine(output,id+".deck");
                bool fresh=File.Exists(target) && files.All(file=>File.GetLastWriteTimeUtc(file)<=File.GetLastWriteTimeUtc(target)) &&
                    File.GetLastWriteTimeUtc(args[2])<=File.GetLastWriteTimeUtc(target) && File.GetLastWriteTimeUtc(typeof(AssetPackager).Assembly.Location)<=File.GetLastWriteTimeUtc(target);
                if(fresh)
                    using(var previous=new SignedAssetPackage(target,id,publicKey)) fresh=previous.Names.OrderBy(n=>n,StringComparer.Ordinal).SequenceEqual(files.Select(Path.GetFileName).OrderBy(n=>n,StringComparer.Ordinal));
                if(!fresh)
                {
                    string temporary=target+".tmp";
                    try
                    {
                        var manifest=new StringBuilder(AssetPackageFormat.Header+"\t"+id+"\n");
                        using(var zip=ZipFile.Open(temporary,ZipArchiveMode.Create))
                        {
                            foreach(string file in files)
                            {
                                string name=Path.GetFileName(file);
                                if(!AssetPackageFormat.SafeName(name) || new FileInfo(file).Length>AssetPackageFormat.MaxEntryBytes-32) throw new InvalidDataException("Invalid source asset: " + name);
                                byte[] encrypted=AssetPackageFormat.Encrypt(File.ReadAllBytes(file));
                                Write(zip,name+".bin",encrypted); manifest.Append(name).Append('\t').Append(AssetPackageFormat.Hash(encrypted)).Append('\n');
                            }
                            byte[] bytes=Encoding.UTF8.GetBytes(manifest.ToString());
                            Write(zip,"manifest.txt",bytes); Write(zip,"manifest.sig",AssetPackageFormat.Sign(bytes,privateKey));
                        }
                        if(File.Exists(target)) File.Replace(temporary,target,null); else File.Move(temporary,target);
                    }
                    finally { if(File.Exists(temporary)) File.Delete(temporary); }
                    Console.WriteLine("Packaged " + id);
                }
                using(var verified=new SignedAssetPackage(target,id,publicKey)) catalog.Append(id).Append('\t').Append(verified.ManifestHash).Append('\n');
            }
            byte[] catalogBytes=Encoding.UTF8.GetBytes(catalog.ToString());
            File.WriteAllBytes(Path.Combine(output,"catalog.txt"),catalogBytes);
            File.WriteAllBytes(Path.Combine(output,"catalog.sig"),AssetPackageFormat.Sign(catalogBytes,privateKey));
            var notices=new StringBuilder("THIRD-PARTY ARTWORK NOTICES\r\nExisting artwork licences and permissions remain unchanged.\r\n");
            foreach(string folder in Directory.GetDirectories(source))
                foreach(string file in Directory.GetFiles(folder).Where(file=>new[]{"LICENSE","LICENSE.txt","LICENSE.md","CREDITS.txt","ATTRIBUTION.txt"}.Contains(Path.GetFileName(file))))
                    notices.AppendLine().AppendLine(Path.GetFileName(folder)+" / "+Path.GetFileName(file)).AppendLine(File.ReadAllText(file));
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(output),"THIRD-PARTY-NOTICES.txt"),notices.ToString(),Encoding.UTF8);
            string legacy=Path.GetFullPath(Path.Combine(Path.GetDirectoryName(output),"images"));
            string sourcePrefix=source.TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
            if(legacy.Equals(source,StringComparison.OrdinalIgnoreCase) || legacy.StartsWith(sourcePrefix,StringComparison.OrdinalIgnoreCase) || source.StartsWith(legacy+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Refusing to clean a build output that overlaps source artwork.");
            if(Directory.Exists(legacy)) Directory.Delete(legacy,true);
            Console.WriteLine("Signed asset packages ready; legacy loose build images removed. Source artwork is unchanged.");
            return 0;
        }
        catch(Exception error) { Console.Error.WriteLine(error.Message); return 1; }
    }
    static bool IsImage(string path) { return new[]{".png",".jpg",".jpeg",".bmp"}.Contains(Path.GetExtension(path).ToLowerInvariant()); }
    static void Write(ZipArchive zip,string name,byte[] bytes)
    { using(var stream=zip.CreateEntry(name,CompressionLevel.NoCompression).Open()) stream.Write(bytes,0,bytes.Length); }
    static string ReadPrivateKey()
    {
        if(!File.Exists(KeyPath)) throw new FileNotFoundException("Asset signing key is unavailable. See docs/asset-packaging.md. Do not generate a replacement key for an existing release identity.");
        return Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(KeyPath),null,DataProtectionScope.CurrentUser));
    }
    static void Initialize(string publicPath)
    {
        if(File.Exists(publicPath)) throw new IOException("Public signing identity already exists. Restore its matching private key instead of replacing it.");
        string privateKey;
        if(File.Exists(KeyPath)) privateKey=ReadPrivateKey();
        else
        {
            using(var rsa=new RSACryptoServiceProvider(3072,new CspParameters { ProviderType=24,Flags=CspProviderFlags.CreateEphemeralKey }))
            { rsa.PersistKeyInCsp=false; privateKey=rsa.ToXmlString(true); }
            Directory.CreateDirectory(Path.GetDirectoryName(KeyPath));
            File.WriteAllBytes(KeyPath,ProtectedData.Protect(Encoding.UTF8.GetBytes(privateKey),null,DataProtectionScope.CurrentUser));
        }
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(publicPath)));
        using(var rsa=AssetPackageFormat.Rsa(privateKey)) File.WriteAllText(publicPath,rsa.ToXmlString(false),new UTF8Encoding(false));
        Console.WriteLine("Public identity written. Private key is DPAPI-protected outside the repository at " + KeyPath);
    }
}
