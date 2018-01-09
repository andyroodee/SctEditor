using Extensions;
using SctEditor.Aklz;
using SctEditor.Sct;
using SctEditor.Util;
using System;
using System.IO;
using System.Windows.Forms;
using System.Linq;

namespace SctEditor
{
    public partial class SctEditorForm : Form
    {
        private SctFile _currentFile;
        private DataStream _dataStream;
        private int _selectedItemIndex;
        private DialogItem[] _dialogItems;

        public SctEditorForm()
        {
            InitializeComponent();
            messageNumLabel.Visible = false;
        }
       
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_currentFile == null)
            {
                return;
            }
            var saveDialog = new SaveFileDialog();
            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                _currentFile.SaveToFile(saveDialog.FileName, _dataStream.Endianness);
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
                    _dataStream = new DataStream(AKLZ.Decompress(ms), Endianness.BigEndian);
                }
                else
                {
                    _dataStream = new DataStream(ms, Endianness.LittleEndian);
                }
                _currentFile = SctFile.CreateFromStream(_dataStream);
                _dialogItems = _currentFile.Items.OfType<DialogItem>().ToArray();
                messageNumLabel.Visible = true;
                DisplayDialogItems();
            }
        }

        private void DisplayDialogItems()
        {
            nameTextBox.Text = _dialogItems[_selectedItemIndex].Name;
            messageTextBox.Text = _dialogItems[_selectedItemIndex].Message;
            messageNumLabel.Text = string.Format("Message {0} of {1}", _selectedItemIndex + 1, _dialogItems.Length);
        }

        private void nextButton_Click(object sender, EventArgs e)
        {
            _selectedItemIndex = (_selectedItemIndex + 1) % _dialogItems.Length;
            DisplayDialogItems();
        }

        private void previousButton_Click(object sender, EventArgs e)
        {
            _selectedItemIndex--;
            if (_selectedItemIndex < 0)
            {
                _selectedItemIndex = _dialogItems.Length - 1;
            }
            DisplayDialogItems();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void messageTextBox_TextChanged(object sender, EventArgs e)
        {
            _dialogItems[_selectedItemIndex].Message = messageTextBox.Text;
        }

        private void nameTextBox_TextChanged(object sender, EventArgs e)
        {
            _dialogItems[_selectedItemIndex].Name = nameTextBox.Text;
        }
    }
}
