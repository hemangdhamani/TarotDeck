using TarotCardsDeck.Assets;
using System;
using System.Drawing;
using System.IO;

namespace TarotCardsDeck
{
    internal static class CardArtwork
    {
        internal static string ResolvePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || AssetStore.Exists(path)) return path;
            string renamed = System.Text.RegularExpressions.Regex.Replace(path,
                @"original-\d{2}-([^\\/]+)", match => match.Value == "original-11-classic-rws" ? "the-illustrated-arcana" : match.Groups[1].Value);
            if (AssetStore.Exists(renamed)) return renamed;
            // Journals written before installation can contain absolute paths from
            // a development folder. Only remap a missing image to its same deck ID.
            var legacy = System.Text.RegularExpressions.Regex.Match(renamed,
                @"(?:^|[\\/])images[\\/]([^\\/]+)[\\/]([^\\/]+)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (legacy.Success)
            {
                string installed = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"images",legacy.Groups[1].Value,legacy.Groups[2].Value);
                if (AssetStore.Exists(installed)) return installed;
            }
            return path;
        }

        // Bounds describe the fitted native canvas, never the decorative card border.
        internal static Bitmap Load(string path)
        {
            using (var stream = AssetStore.OpenRead(path = ResolvePath(path)))
            using (var image = new Bitmap(stream))
            {
                string boundsFile = Path.Combine(Path.GetDirectoryName(path), "DISPLAY-BOUNDS.txt");
                if (AssetStore.Exists(boundsFile))
                    foreach (string line in AssetStore.ReadLines(boundsFile))
                    {
                        var fields = line.Split(',');
                        if (fields.Length != 5 || fields[0] != Path.GetFileNameWithoutExtension(path)) continue;
                        int x, y, width, height;
                        if (int.TryParse(fields[1], out x) && int.TryParse(fields[2], out y) &&
                            int.TryParse(fields[3], out width) && int.TryParse(fields[4], out height) &&
                            x >= 0 && y >= 0 && width > 0 && height > 0 && x + width <= image.Width && y + height <= image.Height)
                            return image.Clone(new Rectangle(x,y,width,height), System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    }
                return new Bitmap(image);
            }
        }
    }
}
