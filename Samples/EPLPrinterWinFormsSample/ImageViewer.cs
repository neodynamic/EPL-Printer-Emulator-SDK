using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Neodynamic.SDK.EPLPrinter;

namespace EPLPrinterWinFormsSample
{
    public partial class ImageViewer : UserControl
    {
        EPLPrinter _eplPrinter = null;

        int iCurrPage = 1, iPages = 1;

        public ImageViewer()
        {
            InitializeComponent();

            this.panelElem.BackColor = Color.FromArgb(128, Color.DeepSkyBlue);
        }

        string[] imgFiles = null;
        public void LoadImages(string folder, ref EPLPrinter eplPrinter)
        {
            _eplPrinter = eplPrinter;

            panelLabel.BackgroundImage = null;

            if (Directory.Exists(folder))
            {
                imgFiles = Directory.GetFiles(folder, "*." + _eplPrinter.RenderOutputFormat.ToString());


                //currentImage = Image.FromStream(imgStream);
                iCurrPage = 1;
                this.RefreshImage();
            }
        }

        public void RefreshImage()
        {
            panelElem.Visible = false;

            iPages = imgFiles.Length;
            btnNext.Visible = btnPrev.Visible = cmdNext.Visible = cmdPrev.Visible = (iPages > 1);
            lblNumOfLabels.Text = "Label " + iCurrPage.ToString() + " of " + iPages.ToString();
            using (FileStream fs = new FileStream(imgFiles[iCurrPage - 1], FileMode.Open, FileAccess.Read))
                panelLabel.BackgroundImage = Image.FromStream(fs);

            if (panelLabel.BackgroundImage != null)
            {
                panelLabel.Width = panelLabel.BackgroundImage.Width;
                panelLabel.Height = panelLabel.BackgroundImage.Height;
            }

            this.SetImageLocation();

            // display rendered elements if any
            this.lstEPLElements.Items.Clear();
            if(_eplPrinter != null && _eplPrinter.RenderedElements != null && _eplPrinter.RenderedElements.Count> 0)
            {
                foreach(var eplElem in _eplPrinter.RenderedElements[iCurrPage - 1])
                {
                    this.lstEPLElements.Items.Add(string.IsNullOrEmpty(eplElem.Content) ? eplElem.Name : string.Format("{0}: `{1}`", eplElem.Name, eplElem.Content));
                }
            }

        }

        public void Clear() {
            if (panelLabel.BackgroundImage != null) panelLabel.BackgroundImage.Dispose();

            this.lstEPLElements.Items.Clear();
        }

        private void btnPrev_Click(object sender, EventArgs e)
        {
            if (iCurrPage > 1)
            {
                iCurrPage--;
                this.RefreshImage();
            }
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            if (iCurrPage < iPages)
            {
                iCurrPage++;
                this.RefreshImage();
            }
        }

        private void SetImageLocation()
        {
            int x = 0;
            int y = 0;

            if (panelLabel.Width > pnlContainer.ClientRectangle.Width)
            {
                x = 0;
            }
            else
            {
                x = (pnlContainer.ClientRectangle.Width - panelLabel.Width) / 2;
            }

            if (panelLabel.Height > pnlContainer.ClientRectangle.Height)
            {
                y = 0;
            }
            else
            {
                y = (pnlContainer.ClientRectangle.Height - panelLabel.Height) / 2;
            }

            panelLabel.Location = new Point(x, y);
        }

        private void lstEPLElements_SelectedIndexChanged(object sender, EventArgs e)
        {
            EPLElement eplElem = null;
            if(this.lstEPLElements.SelectedIndex >= 0)
            {
                eplElem = _eplPrinter.RenderedElements[iCurrPage - 1][this.lstEPLElements.SelectedIndex];
            }
            HighlightEplElem(eplElem);
        }

        private void HighlightEplElem(EPLElement eplElem)
        {
            this.panelElem.Visible = (eplElem != null);

            if (eplElem != null)
            {
                this.panelElem.Location = new Point(eplElem.X, eplElem.Y);
                this.panelElem.Size = new Size(eplElem.Width, eplElem.Height);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            this.SetImageLocation();
        }

    }
}
