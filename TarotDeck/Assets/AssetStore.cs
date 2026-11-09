using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TarotCardsDeck.Assets
{
    // Existing image paths become logical IDs inside packages, preserving journal compatibility.
    public static class AssetStore
    {
        private static readonly object Gate = new object();
        private static Dictionary<string,SignedAssetPackage> packages;
        private static string Root { get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"images"); } }
        public static string PackageDirectory { get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets"); } }
        private static string Key(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            string full = Path.GetFullPath(path), prefix = Path.GetFullPath(Root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (full.Equals(prefix.TrimEnd(Path.DirectorySeparatorChar),StringComparison.OrdinalIgnoreCase)) return "";
            return full.StartsWith(prefix,StringComparison.OrdinalIgnoreCase) ? full.Substring(prefix.Length).Replace('\\','/') : null;
        }
        public static void Initialize()
        {
            lock (Gate)
            {
                if (packages != null) return;
                var loaded = new Dictionary<string,SignedAssetPackage>(StringComparer.OrdinalIgnoreCase);
                try
                {
                    string publicKey;
                    using (var stream = typeof(AssetStore).Assembly.GetManifestResourceStream("TarotDeck.AssetPublicKey.xml"))
                    using (var reader = new StreamReader(stream)) publicKey = reader.ReadToEnd();
                    foreach (string path in Directory.GetFiles(PackageDirectory,"*.deck").OrderBy(path=>path,StringComparer.Ordinal))
                    {
                        string identity = Path.GetFileNameWithoutExtension(path);
                        if (!AssetPackageFormat.SafeName(identity) || loaded.ContainsKey(identity)) throw new InvalidDataException("Invalid asset package filename.");
                        loaded.Add(identity,new SignedAssetPackage(path,identity,publicKey));
                    }
                    if (!loaded.ContainsKey("cardbacks") || !loaded.ContainsKey("the-illustrated-arcana"))
                        throw new InvalidDataException("The default front deck or card-back package is missing. Repair the TarotDeck installation.");
                    packages = loaded;
                }
                catch { foreach (var package in loaded.Values) package.Dispose(); throw; }
            }
        }
        public static bool Exists(string path)
        {
            string key = Key(path); if (key == null) return File.Exists(path);
            Initialize(); string[] parts = key.Split('/');
            return parts.Length == 2 && packages.ContainsKey(parts[0]) && packages[parts[0]].Names.Contains(parts[1],StringComparer.OrdinalIgnoreCase);
        }
        public static bool DirectoryExists(string path)
        {
            string key=Key(path); if(key==null) return Directory.Exists(path);
            Initialize(); return key=="" || packages.ContainsKey(key.TrimEnd('/'));
        }
        public static string[] GetDirectories(string path)
        {
            string key=Key(path); if(key==null) return Directory.GetDirectories(path);
            Initialize(); return key=="" ? packages.Keys.OrderBy(id=>id,StringComparer.Ordinal).Select(id=>Path.Combine(Root,id)).ToArray() : new string[0];
        }
        public static string[] GetFiles(string path)
        {
            string key=Key(path); if(key==null) return Directory.GetFiles(path);
            Initialize(); SignedAssetPackage package;
            return packages.TryGetValue(key.TrimEnd('/'),out package) ? package.Names.Select(name=>Path.Combine(path,name)).ToArray() : new string[0];
        }
        public static Stream OpenRead(string path)
        {
            string key=Key(path); if(key==null) return File.OpenRead(path);
            Initialize(); string[] parts=key.Split('/');
            lock(Gate)
            {
                SignedAssetPackage package;
                if(parts.Length!=2 || !packages.TryGetValue(parts[0],out package)) throw new FileNotFoundException("Unknown packaged artwork: " + key);
                return new MemoryStream(package.Read(parts[1]),false);
            }
        }
        public static string ReadAllText(string path)
        { using(var stream=OpenRead(path)) using(var reader=new StreamReader(stream,Encoding.UTF8,true)) return reader.ReadToEnd(); }
        public static IEnumerable<string> ReadLines(string path)
        { return ReadAllText(path).Split(new[]{"\r\n","\n"},StringSplitOptions.None); }
        public static void Close()
        { lock(Gate) { if(packages!=null) { foreach(var package in packages.Values) package.Dispose(); packages=null; } } }
    }
}
