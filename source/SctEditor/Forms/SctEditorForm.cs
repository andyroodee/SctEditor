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
       
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
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

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
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
                    dataStreamReader = new DataStream(AKLZ.Decompress(ms), Endianness.BigEndian);
                }
                else
                {
                    dataStreamReader = new DataStream(ms, Endianness.LittleEndian);
                }
                currentFile = SctFile.CreateFromStream(dataStreamReader);
            }
        }
    }
}
