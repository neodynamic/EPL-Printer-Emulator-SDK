using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.IO;
using System.IO.Compression;
using Neodynamic.SDK.EPLPrinter;
using System.Web;

namespace EPLPrinterAspNetCoreSample.Controllers
{
    public class ProcessEPLController : Controller
    {
        private IHostingEnvironment _env;
        private HttpContext _ctx;

        public ProcessEPLController(IHostingEnvironment env, IHttpContextAccessor ctx)
        {
            _env = env;
            _ctx = ctx.HttpContext;
        }

        public async void ProcessRequest()
        {

            //Get data for rendering process...
            string lstPrinterDpi = _ctx.Request.Form["lstPrinterDpi"];
            string txtLabelWidth = _ctx.Request.Form["txtLabelWidth"];
            string txtLabelHeight = _ctx.Request.Form["txtLabelHeight"];
            string cpRibbonColor = _ctx.Request.Form["cpRibbonColor"];
            string cpBackColor = _ctx.Request.Form["cpBackColor"];
            string lstOutputFormat = _ctx.Request.Form["lstOutputFormat"];
            string lstOutputRotate = _ctx.Request.Form["lstOutputRotate"];
            string rawEplCommands = _ctx.Request.Form["rawEplCommands"];

            var json = new StringBuilder();
            json.Append("{");

            try
            {
                if (string.IsNullOrEmpty(rawEplCommands)) throw new ArgumentException("Please specify some EPL commands.");


                //Create an instance of EPLPrinter class
                using (var eplPrinter = new EPLPrinter("LICENSE OWNER", "LICENSE KEY"))
                {
                    //Set printer DPI
                    //The DPI value to be set must match the value for which 
                    //the EPL commands to be processed were created!!!
                    eplPrinter.Dpi = float.Parse(lstPrinterDpi.Substring(0, 3));
                    //Apply antialiasing?
                    eplPrinter.AntiAlias = (_ctx.Request.Form["chkAntialias"].Count > 0);
                    //set label size
                    eplPrinter.LabelWidth = float.Parse(txtLabelWidth) * eplPrinter.Dpi;
                    if (eplPrinter.LabelWidth <= 0) eplPrinter.LabelWidth = 4;
                    eplPrinter.ForceLabelWidth = (_ctx.Request.Form["chkForceLabelWidth"].Count > 0);
                    eplPrinter.LabelHeight = float.Parse(txtLabelHeight) * eplPrinter.Dpi;
                    if (eplPrinter.LabelHeight <= 0) eplPrinter.LabelHeight = 6;
                    eplPrinter.ForceLabelHeight = (_ctx.Request.Form["chkForceLabelHeight"].Count > 0);
                    //Set Label BackColor
                    eplPrinter.LabelBackColor = cpBackColor;
                    //Set Ribbon Color
                    eplPrinter.RibbonColor = cpRibbonColor;
                    //Set image or doc format for output rendering 
                    eplPrinter.RenderOutputFormat = (RenderOutputFormat)Enum.Parse(typeof(RenderOutputFormat), lstOutputFormat);
                    //Set rotation for output rendering
                    eplPrinter.RenderOutputRotation = (RenderOutputRotation)Enum.Parse(typeof(RenderOutputRotation), lstOutputRotate);
                    //Set text encoding
                    Encoding enc = (_ctx.Request.Form["chkUTF8"].Count > 0 ? Encoding.UTF8 : Encoding.GetEncoding(850));

                    var rawCommands = Convert.FromBase64String(rawEplCommands);

                    var buffer = eplPrinter.ProcessCommands(rawCommands, enc, true);

                    // the buffer variable contains the binary output of the EPL rendering result
                    // The format of this buffer depends on the RenderOutputFormat property setting
                    if (buffer != null && buffer.Count > 0)
                    {
                        if (eplPrinter.RenderOutputFormat == RenderOutputFormat.PNG ||
                            eplPrinter.RenderOutputFormat == RenderOutputFormat.JPG)
                        {
                            json.Append("\"labelImages\":[");

                            for (int i = 0; i < buffer.Count; i++)
                            {

                                json.Append($"\"data:image/{eplPrinter.RenderOutputFormat.ToString().ToLower()};base64,{Convert.ToBase64String(buffer[i])}\"");
                                if (i < buffer.Count - 1) json.Append(",");
                            }

                            json.Append("]");
                        }
                        else if (eplPrinter.RenderOutputFormat == RenderOutputFormat.PDF)
                        {
                            json.Append($"\"labelPDF\":\"data:application/pdf;base64,{Convert.ToBase64String(buffer[0])}\"");
                        }
                        else
                        {
                            string fileExt = eplPrinter.RenderOutputFormat.ToString().ToLower();
                            //If there're more than one file, then zip them...
                            if (buffer.Count > 1)
                            {
                                using (var outStream = new MemoryStream())
                                {
                                    using (var archive = new ZipArchive(outStream, ZipArchiveMode.Create, true))
                                    {
                                        for (int i = 0; i < buffer.Count; i++)
                                        {
                                            var fileInArchive = archive.CreateEntry($"Label{i.ToString()}.{fileExt}", CompressionLevel.Optimal);
                                            using (var entryStream = fileInArchive.Open())
                                            using (var fileToCompressStream = new MemoryStream(buffer[i]))
                                            {
                                                fileToCompressStream.CopyTo(entryStream);
                                            }
                                        }
                                    }
                                    json.Append($"\"labelBinaries\":\"data:application/zip;base64,{Convert.ToBase64String(outStream.ToArray())}\"");
                                }
                            }
                            else
                            {
                                json.Append($"\"labelBinaries\":\"data:application/octet-stream;base64,{Convert.ToBase64String(buffer[0])}\"");
                            }
                        }

                        json.Append($",\"renderedElements\":" + eplPrinter.RenderedElementsAsJson);

                    }
                    else
                        throw new ArgumentException("No output available for the specified EPL commands.");
                }
            }
            catch (Exception ex)
            {
                json.Append("\"error\":");
                json.Append($"\"{HttpUtility.JavaScriptStringEncode(ex.Message)}\"");
            }

            json.Append("}");

            _ctx.Response.ContentType = "application/json";
            await _ctx.Response.WriteAsync(json.ToString());

            
        }
    }
}
