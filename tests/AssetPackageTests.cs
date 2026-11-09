using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;
using System.Drawing;
using TarotCardsDeck.Assets;

class AssetPackageTests
{
    static int checks;
    static void Check(bool valid, string message) { if (!valid) throw new Exception(message); checks++; }
    static void Reject(Action action, string message)
    {
        try { action(); } catch (InvalidDataException) { checks++; return; }
        catch (CryptographicException) { checks++; return; }
        throw new Exception("Accepted invalid package: " + message);
    }
    static void Entry(ZipArchive zip, string name, byte[] bytes)
    { using (var output = zip.CreateEntry(name).Open()) output.Write(bytes,0,bytes.Length); }
    static void Make(string path, string key, bool changedManifest, bool changedPayload, bool extra)
    {
        byte[] payload = AssetPackageFormat.Encrypt(Encoding.UTF8.GetBytes("known artwork bytes"));
        byte[] manifest = Encoding.UTF8.GetBytes(AssetPackageFormat.Header + "\ttest\ncard.png\t" + AssetPackageFormat.Hash(payload) + "\n");
        byte[] signature = AssetPackageFormat.Sign(manifest,key);
        if (changedManifest) manifest[0] ^= 1;
        if (changedPayload) payload[17] ^= 1;
        using (var zip = ZipFile.Open(path,ZipArchiveMode.Create))
        {
            Entry(zip,"manifest.txt",manifest); Entry(zip,"manifest.sig",signature);
            Entry(zip,"card.png.bin",payload);
            if(extra) Entry(zip,"unlisted.bin",payload);
        }
    }
    static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "probe")
        {
            try
            {
                AssetStore.Initialize();
                string root=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"images");
                Check(!Directory.Exists(root),"Loose image directory");
                Check(AssetStore.GetDirectories(root).Length == int.Parse(args[1]),"Wrong installed deck count");
                string front=AssetStore.GetFiles(Path.Combine(root,"the-illustrated-arcana")).First(p=>Path.GetFileNameWithoutExtension(p)=="maj22");
                using(var stream=AssetStore.OpenRead(front)) using(var image=Image.FromStream(stream)) Check(image.Width>100,"Front did not decode");
                string back=AssetStore.GetFiles(Path.Combine(root,"cardbacks")).First(p=>p.EndsWith("emerald-labyrinth.png"));
                using(var stream=AssetStore.OpenRead(back)) using(var image=Image.FromStream(stream)) Check(image.Height>100,"Back did not decode");
                return 0;
            }
            catch(Exception error) { Console.Error.WriteLine(error.Message); return 1; }
            finally { AssetStore.Close(); }
        }
        string temp=Path.Combine(Path.GetTempPath(),"TarotDeck-AssetTests-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            string privateKey,publicKey;
            using(var rsa=new RSACryptoServiceProvider(2048)) { rsa.PersistKeyInCsp=false; privateKey=rsa.ToXmlString(true); publicKey=rsa.ToXmlString(false); }
            string good=Path.Combine(temp,"good.deck"); Make(good,privateKey,false,false,false);
            using(var pack=new SignedAssetPackage(good,"test",publicKey)) Check(Encoding.UTF8.GetString(pack.Read("CARD.PNG"))=="known artwork bytes","Round trip/case mapping failed");
            Reject(()=> { using(var pack=new SignedAssetPackage(good,"wrong",publicKey)) {} },"identity substitution");
            string wrongKey;
            using(var rsa=new RSACryptoServiceProvider(2048)) { rsa.PersistKeyInCsp=false; wrongKey=rsa.ToXmlString(false); }
            Reject(()=> { using(var pack=new SignedAssetPackage(good,"test",wrongKey)) {} },"unknown publisher");
            string bad=Path.Combine(temp,"manifest.deck"); Make(bad,privateKey,true,false,false);
            Reject(()=> { using(var pack=new SignedAssetPackage(bad,"test",publicKey)) {} },"manifest tampering");
            bad=Path.Combine(temp,"payload.deck"); Make(bad,privateKey,false,true,false);
            Reject(()=> { using(var pack=new SignedAssetPackage(bad,"test",publicKey)) pack.Read("card.png"); },"payload tampering");
            bad=Path.Combine(temp,"extra.deck"); Make(bad,privateKey,false,false,true);
            Reject(()=> { using(var pack=new SignedAssetPackage(bad,"test",publicKey)) {} },"unlisted entries");

            string source=AppDomain.CurrentDomain.BaseDirectory;
            string isolated=Path.Combine(temp,"installation"); Directory.CreateDirectory(isolated);
            File.Copy(typeof(AssetPackageTests).Assembly.Location,Path.Combine(isolated,"AssetPackageTests.exe"));
            File.Copy(typeof(AssetStore).Assembly.Location,Path.Combine(isolated,"TarotDeck.exe"));
            string assets=Path.Combine(isolated,"assets"); Directory.CreateDirectory(assets);
            foreach(string id in new[]{"the-illustrated-arcana","cardbacks"}) File.Copy(Path.Combine(source,"assets",id+".deck"),Path.Combine(assets,id+".deck"));
            Probe(isolated,2,true);
            File.Copy(Path.Combine(source,"assets","soimoi.deck"),Path.Combine(assets,"soimoi.deck"));
            Probe(isolated,3,true);
            File.Delete(Path.Combine(assets,"soimoi.deck")); Probe(isolated,2,true);
            File.Delete(Path.Combine(assets,"cardbacks.deck")); Probe(isolated,1,false);
            Console.WriteLine("PASS: {0} asset security and isolated-install checks",checks);
            return 0;
        }
        catch(Exception error) { Console.Error.WriteLine(error); return 1; }
        finally { Directory.Delete(temp,true); }
    }
    static void Probe(string directory,int count,bool expected)
    {
        var info=new ProcessStartInfo(Path.Combine(directory,"AssetPackageTests.exe"),"probe "+count) { UseShellExecute=false,CreateNoWindow=true,RedirectStandardError=true };
        using(var process=Process.Start(info))
        { string error=process.StandardError.ReadToEnd(); process.WaitForExit(); Check((process.ExitCode==0)==expected,"Isolated install probe failed: "+error); }
    }
}
