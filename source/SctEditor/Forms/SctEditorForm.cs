using Extensions;
using SctEditor.Aklz;
using SctEditor.Sct;
using SctEditor.Util;
using System;
using System.IO;
using System.Windows.Forms;

namespace SctEditor
{
    public partial class SctEditorForm : Form
    {
        private SctFile currentFile;
        private DataStream dataStreamReader;

        public SctEditorForm()
        {
            InitializeComponent();
        }

        private void openSctButton_Click(object sender, EventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.CheckFileExists = true;
            openFileDialog.Filter = "SCT Files (*.sct)|*.sct|All files (*.*)|*.*";            
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                var fileName = openFileDialog.FileName;
                MemoryStream ms = new MemoryStream(File.ReadAllBytes(fileName));
                // The first 4 bytes in the file can tell us if this is AKLZ compressed or not.
                if (ms.ReadString(0, 4) == "AKLZ")
                {
                    dataStreamReader = new DataStream(AKLZ.Decompress(ms), Util.Endianness.BigEndian);
                }
                else
                {
                    dataStreamReader = new DataStream(ms, Util.Endianness.LittleEndian);
                }
                currentFile = SctFile.CreateFromStream(dataStreamReader);

                // We first need to decompress the file before getting the SctFile class to parse it.
                //using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                //{
                //    MemoryStream ms = new MemoryStream();
                //    // The first 4 bytes in the file can tell us if this is AKLZ compressed or not.
                //    if (fs.ReadString(0, 4) == "AKLZ")
                //    {
                //        ms = AKLZ.Decompress(fs);
                //    }
                //    fs.Position = 0;
                //    var sctFile = SctFile.CreateFromStream(ms);

                //    // Write out the decompressed version for comparison.
                //    //string decompFileName = Path.GetFileNameWithoutExtension(fileName) + "_decompoo.sct";
                //    //using (BufferedStream bs = new BufferedStream(new FileStream(decompFileName, FileMode.Create, FileAccess.Write)))
                //    //{
                //    //    decompStream.WriteTo(bs);
                //    //}
                //}
            }
        }

        private void saveSctButton_Click(object sender, EventArgs e)
        {
            if (currentFile == null)
            {
                return;
            }
            var saveDialog = new SaveFileDialog();
            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                currentFile.SaveToFile(saveDialog.FileName, dataStreamReader.Endianness);
            }
        }
    }
}
