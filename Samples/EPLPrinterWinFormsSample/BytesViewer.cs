using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ComponentModel.Design;

namespace EPLPrinterWinFormsSample
{
    public partial class BytesViewer : UserControl
    {
        private ByteViewer byteviewer;

        public BytesViewer()
        {
            InitializeComponent();

            // Initialize the ByteViewer.
            byteviewer = new ByteViewer();
            byteviewer.Dock = DockStyle.Fill;
            byteviewer.SetBytes(new byte[] { });
            this.panel1.Controls.Add(byteviewer);


            this.cboDisplayMode.DataSource = Enum.GetNames(typeof(DisplayMode));
        }

        private void cboDisplayMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            byteviewer.SetDisplayMode((DisplayMode)Enum.Parse(typeof(DisplayMode), cboDisplayMode.SelectedItem.ToString()));
        }

        public void SetBytes(byte[] buffer)
        {
            byteviewer.SetBytes(buffer);
        }
    }
}
