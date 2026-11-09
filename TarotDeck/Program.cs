using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace TarotCardsDeck
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                TarotCardsDeck.Assets.AssetStore.Initialize();
                Application.Run(new frmTarot());
            }
            catch (Exception error) when (error is System.IO.IOException || error is System.Security.Cryptography.CryptographicException || error is UnauthorizedAccessException)
            {
                MessageBox.Show("TarotDeck could not verify its installed artwork packages. Repair or reinstall the affected package.\r\n\r\n" + error.Message,
                    "Artwork installation", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { TarotCardsDeck.Assets.AssetStore.Close(); }
        }
    }
}
