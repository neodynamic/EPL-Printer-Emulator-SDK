using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.IO;

using Neodynamic.SDK.EPLPrinter;
using SkiaSharp;

namespace EPLPrinterWinFormsSample
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        //Create an instance of EPLPrinter class
        EPLPrinter eplPrinter = new EPLPrinter("", "");

        private void Form1_Load(object sender, EventArgs e)
        {
            //Load supported output rendering formats and rotation options
            cboOutputFormat.DataSource = Enum.GetNames(typeof(RenderOutputFormat));
            cboOutputRotation.DataSource = Enum.GetNames(typeof(RenderOutputRotation));

            //Select default DPI printer
            this.cboDpi.SelectedIndex = 1; //203 dpi

            //set default label size
            this.nudLabelWidth.Value = 4;
            this.nudLabelHeight.Value = 6;

            
        }

        OpenFileDialog ofd = new OpenFileDialog();
        byte[] commandsBuffer = null;

        private void btnPreviewEpl_Click(object sender, EventArgs e)
        {
            //Prepare EPLPrinter
            this.PrepareEPLPrinter();

            //try
            //{
                //Let EPLPrinter to process the specified EPL commands
                //and display rendering output if any...
                DisplayRenderOutput(eplPrinter.ProcessCommands(commandsBuffer, true));
            //}
            //catch (Exception ex)
            //{
            //    this.imgViewer.Clear();
            //    MessageBox.Show(ex.Message);
            //}

        }

        private void btnOpenEplFile_Click(object sender, EventArgs e)
        {
            //Open file containing EPL commands...
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                
                //display commands from file
                commandsBuffer = System.IO.File.ReadAllBytes(ofd.FileName);

                bytesViewer1.SetBytes(commandsBuffer);

                //Let EPLPrinter to process the specified file containing EPL commands
                //and display rendering output if any...
                btnPreviewEpl_Click(null, EventArgs.Empty);
            }
        }

        private void DisplayRenderOutput(List<byte[]> buffer)
        {
            // the buffer param contains the binary output of the EPL rendering result
            // The format of this buffer depends on the RenderOutputFormat property setting
            if (buffer != null && buffer.Count > 0)
            {
                if (eplPrinter.RenderOutputFormat == RenderOutputFormat.PNG ||
                    eplPrinter.RenderOutputFormat == RenderOutputFormat.JPG)
                {
                    //temp folder for holding thermal label images
                    this.imgViewer.Clear();
                    string myDir = Directory.GetCurrentDirectory() + @"\temp\";
                    if (Directory.Exists(myDir) == false) Directory.CreateDirectory(myDir);
                    DirectoryInfo di = new DirectoryInfo(myDir);
                    foreach (FileInfo file in di.GetFiles()) file.Delete();

                    try
                    {
                        //save images on disk 
                        for(int i = 0; i < buffer.Count; i++)
                        {
                            File.WriteAllBytes(myDir + "Image" + i.ToString().PadLeft(buffer.Count, '0') + "." + eplPrinter.RenderOutputFormat.ToString(), buffer[i]);
                        }
                        //preview them
                        this.imgViewer.LoadImages(myDir, ref eplPrinter);
                        
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                    

                }
                else if (eplPrinter.RenderOutputFormat == RenderOutputFormat.PDF)
                {
                    var sd = new SaveFileDialog();
                    sd.Filter = "Portable Document Format (*.pdf)|*.pdf";
                    sd.DefaultExt = "pdf";
                    sd.AddExtension = true;
                    if (sd.ShowDialog() == DialogResult.OK)
                        System.IO.File.WriteAllBytes(sd.FileName, buffer[0]);
                }
                else 
                {
                    var sd = new SaveFileDialog();
                    if (eplPrinter.RenderOutputFormat == RenderOutputFormat.PCX)
                    {
                        sd.Filter = "PiCture eXchange (*.pcx)|*.pcx";
                        sd.DefaultExt = "pcx";
                    }
                    else if (eplPrinter.RenderOutputFormat == RenderOutputFormat.GRF)
                    {
                        sd.Filter = "Zebra GRF ASCII hexadecimal (*.grf)|*.grf";
                        sd.DefaultExt = "grf";
                    }
                    else if (eplPrinter.RenderOutputFormat == RenderOutputFormat.EPL)
                    {
                        sd.Filter = "Zebra EPL Binary (*.epl)|*.epl";
                        sd.DefaultExt = "epl";
                    }
                    else if (eplPrinter.RenderOutputFormat == RenderOutputFormat.FP)
                    {
                        sd.Filter = "Honeywell-Intermec Fingerprint Binary (*.fp)|*.fp";
                        sd.DefaultExt = "fp";
                    }
                    else if (eplPrinter.RenderOutputFormat == RenderOutputFormat.NV)
                    {
                        sd.Filter = "EPSON ESC/POS NV Binary (*.nv)|*.nv";
                        sd.DefaultExt = "nv";
                    }

                    sd.AddExtension = true;
                    if (sd.ShowDialog() == DialogResult.OK)
                        System.IO.File.WriteAllBytes(sd.FileName, buffer[0]);
                }
            }
        }


        private void PrepareEPLPrinter()
        {
            //EPLPrinter acts as a virtual printer...
            //Some EPL commands could upload labels, graphics, fonts 
            //and /or modify global printer settings
            //that can be used by other commands...
            //So, by Resetting the printer before processing EPL commands
            //will make the printer to clear settings and any resources uploaded by previous commands
            if (this.chkResetPrinter.Checked)
                eplPrinter.PowerOnReset();

            //Set printer DPI
            //The DPI value to be set must match the value for which 
            //the EPL commands to be processed were created!!!
            eplPrinter.Dpi = float.Parse(cboDpi.SelectedItem.ToString().Substring(0, 3));

            //set label size
            eplPrinter.LabelWidth = (float)this.nudLabelWidth.Value * eplPrinter.Dpi;
            eplPrinter.LabelHeight = (float)this.nudLabelHeight.Value * eplPrinter.Dpi;

            eplPrinter.ForceLabelWidth = this.chkForceLabelWidth.Checked;
            eplPrinter.ForceLabelHeight = this.chkForceLabelHeight.Checked;


            //Apply antialiasing?
            eplPrinter.AntiAlias = this.chkAntiAlias.Checked;
            
            //Set image or doc format for output rendering 
            eplPrinter.RenderOutputFormat = (RenderOutputFormat)Enum.Parse(typeof(RenderOutputFormat), cboOutputFormat.SelectedValue.ToString());

            //Set rotation for output rendering
            eplPrinter.RenderOutputRotation = (RenderOutputRotation)Enum.Parse(typeof(RenderOutputRotation), cboOutputRotation.SelectedValue.ToString());

            //Set Ribbon Color
            eplPrinter.RibbonColor = ColorToHex(this.btnRibbonColor.BackColor);

            //Set Label BackColor
            if(chkTransparent.Checked)
                eplPrinter.LabelBackColor = ColorToHex(Color.Transparent);
            else
                eplPrinter.LabelBackColor = ColorToHex(this.btnLabelBackColor.BackColor);

            //Set Background Image
            eplPrinter.BackgroudImageFile = this.txtBackgroundImage.Text;

            //Set Watermark Image
            eplPrinter.WatermarkImageFile = this.txtWatermarkImage.Text;
            eplPrinter.WatermarkOpacity = 50;

            //Set Thumbnail Size
            //zplPrinter.ThumbnailSize = 300;
        }

        private string ColorToHex(Color c)
        {
            return string.Format("{0}{1}{2}{3}",
                Convert.ToString(c.A, 16).PadLeft(2, '0'),
                Convert.ToString(c.R, 16).PadLeft(2, '0'),
                Convert.ToString(c.G, 16).PadLeft(2, '0'),
                Convert.ToString(c.B, 16).PadLeft(2, '0'));
        }

        
        private void btnRibbonColor_Click(object sender, EventArgs e)
        {
            var cd = new ColorDialog();
            cd.Color = this.btnRibbonColor.BackColor;
            if(cd.ShowDialog() == DialogResult.OK)
            {
                this.btnRibbonColor.BackColor = cd.Color;
            }
        }

        private void btnLabelBackColor_Click(object sender, EventArgs e)
        {
            var cd = new ColorDialog();
            cd.Color = this.btnLabelBackColor.BackColor;
            if (cd.ShowDialog() == DialogResult.OK)
            {
                this.btnLabelBackColor.BackColor = cd.Color;
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {   
            eplPrinter.Dispose();
        }

        private void btnExamine_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = "Image Files(*.JPG;*.PNG)|*.JPG;*.PNG";
            if(ofd.ShowDialog() == DialogResult.OK)
            {
                txtBackgroundImage.Text = ofd.FileName;
            }
            else
            {
                txtBackgroundImage.Text = "";
            }

        }

        private void btnPrinterStorage_Click(object sender, EventArgs e)
        {
            //var psd = new PrinterStorage();
            //psd.VirtualPrinter = eplPrinter;
            //psd.ShowDialog();
        }

        private void btnExamineWatermark_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = "Image Files(*.JPG;*.PNG)|*.JPG;*.PNG";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtWatermarkImage.Text = ofd.FileName;
            }
            else
            {
                txtWatermarkImage.Text = "";
            }
        }
    }
}
