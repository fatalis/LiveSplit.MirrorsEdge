using System;
using System.IO;
using System.Windows.Forms;

namespace LiveSplit32BitPatcher
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                string path = "LiveSplit.exe";
                if (!File.Exists(path))
                {
                    ShowMessage("LiveSplit.exe couldn't be found. Please browse to it.", MessageBoxIcon.Exclamation);

                    using (var fd = new OpenFileDialog())
                    {
                        fd.Filter = "LiveSplit.exe|LiveSplit.exe";
                        if (fd.ShowDialog() != DialogResult.OK)
                            return;
                        path = fd.FileName;
                    }
                }

                using (var fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite))
                {
                    const byte FLAG_32_BIT_REQUIRED = 0x02;

                    // .NET Directory - Flags
                    fs.Seek(0x218, SeekOrigin.Begin);
                    var flags = (byte)fs.ReadByte();

                    if ((flags & FLAG_32_BIT_REQUIRED) != 0)
                    {
                        ShowMessage("The patch is already installed! You only need to do this once.", MessageBoxIcon.Exclamation);
                    }
                    else
                    {
                        fs.Seek(-1, SeekOrigin.Current);
                        fs.WriteByte((byte)(flags | FLAG_32_BIT_REQUIRED));

                        ShowMessage("Patch successful!", MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage("Error!" + Environment.NewLine + ex, MessageBoxIcon.Error);
            }
        }

        static void ShowMessage(string message, MessageBoxIcon icon)
        {
            MessageBox.Show(message, "LiveSplit 32-Bit Patcher", MessageBoxButtons.OK, icon);
        }
    }
}
