using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iTextSharp;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using Path = System.IO.Path;
using iTextSharp.text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using DossierExplorer.TextExtractOCR.Models;
using System.Web.Routing;
using System.Web;
using System.Threading;

namespace DossierExplorer
{
    public class OCR
    {
        //static void Main(string[] args)
        //{
        
        //    //    //    Stopwatch stopwatch = new Stopwatch();
        //    //    //    stopwatch.Start();




        //    //    //    var testFiles = Directory.EnumerateFiles(@"C:\Users\Mustafa\Desktop","*.jpg");

        //    //    //    var maxDegreeOfParallelism = Environment.ProcessorCount;
        //    //    //    Parallel.ForEach(testFiles, new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism }, (fileName) =>
        //    //    //    {
        //    //    //        var imageFile = File.ReadAllBytes(fileName);
        //    //    //        var text = OCR(imageFile);
        //    //    //        Console.WriteLine("File:" + fileName + "\n" + text + "\n");
        //    //    //    });

        //    //    //    stopwatch.Stop();
        //    //    //    Console.WriteLine("Duration: " + stopwatch.Elapsed);
        //    //    //    Console.WriteLine("Press enter to continue...");
        //    //    //    Console.ReadLine();
        //    Stopwatch stopwatch = new Stopwatch();
        //    stopwatch.Start();
        //    string output = PdfToText(@"C:\Users\Mustafa\Downloads\2019_yıliçi_hazırlık.pdf");
        //    //    //string x = OCRImage(@"D:\DosyaYönetim\DMS\TempImages\Page-26.png");
        //    //string output1 = GetTextFromPDF(@"D:\DosyaYönetim\DMS\DMSRootDirectory\bilgisayar kitapları\JAVA\JAVA BİLGİSAYAR DİLİYLE PROGRAMLAMA.pdf");
        //    //    //System.IO.File.WriteAllText(@"d:\result", output);
        //    Console.Write(output);
        //    stopwatch.Stop();

        //    Console.WriteLine("Duration: " + stopwatch.Elapsed);
        //    Console.Write("done");
        //    Console.ReadKey();

        //}

        public static string OCRImage(string path)
        {
            try
            {
                byte[] imageFile = File.ReadAllBytes(@path);
                //System.Web.HttpContext currentContext = System.Web.HttpContext.Current;
                string projectPath = GlobalVariables.MyAppPath;
                
                string[] lang = { "eng", "tur" };
                string output = string.Empty;
                var tempOutputFile = Path.GetTempPath() + Guid.NewGuid();
                var tempImageFile = Path.GetTempFileName();

                try
                {
                    File.WriteAllBytes(tempImageFile, imageFile);

                    ProcessStartInfo info = new ProcessStartInfo();
                    info.WorkingDirectory = projectPath + @"\Tesseract";
                    info.WindowStyle = ProcessWindowStyle.Hidden;
                    info.UseShellExecute = false;
                    info.FileName = "cmd.exe";
                    info.Arguments =
                        "/c tesseract.exe " + "--oem 1 " +
                        // Image file.
                        tempImageFile + " " +
                        // Output file (tesseract add '.txt' at the end)
                        tempOutputFile +
                        // Languages.
                        " -l " + string.Join("+", lang);

                    // Start tesseract.
                    Process process = Process.Start(info);
                    process.WaitForExit();
                    if (process.ExitCode == 0)
                    {
                        // Exit code: success.
                        output = File.ReadAllText(tempOutputFile + ".txt");
                        File.Delete(path);
                    }
                    else
                    {
                        //File.Delete(path);
                        File.Delete(tempImageFile);
                        File.Delete(tempOutputFile + ".txt");
                        throw new Exception("Error. Tesseract stopped with an error code = " + process.ExitCode);
                    }
                }
                finally
                {
                    //if(File.Exists(path))File.Delete(path);
                    File.Delete(tempImageFile);
                    File.Delete(tempOutputFile + ".txt");
                }
                return output;
            }
            catch (FileNotFoundException)
            {
                

                return null;
                
            }
            finally
            {
                if(File.Exists(path))File.Delete(path);



            }


        }

        public static string OCRPdf(string path)
        {
            //int desired_x_dpi = 96;
            //int desired_y_dpi = 96;

            //string inputPdfPath = path;
            //string outputPath = @"D:\DosyaYönetim\DMS\TempImages";


            //using (var rasterizer = new GhostscriptRasterizer())
            //{
            //    byte[] buffer = File.ReadAllBytes(inputPdfPath);
            //    MemoryStream ms = new MemoryStream(buffer);
            //    rasterizer.Open(ms);

            //    for (var pageNumber = 1; pageNumber <= rasterizer.PageCount; pageNumber++)
            //    {
            //        var pageFilePath = Path.Combine(outputPath, string.Format("{0}.png", pageNumber));

            //        var img = rasterizer.GetPage(desired_x_dpi, desired_y_dpi, pageNumber);
            //        img.Save(pageFilePath, ImageFormat.Png);

            //        Console.WriteLine(pageFilePath);
            //    }
            //}
            //var maxDegreeOfParallelism = Environment.ProcessorCount;
            //var pngs = Directory.GetFiles(outputPath, "*.png");

            //string output = string.Empty;
            //List<PdfFile> pdflist = new List<PdfFile>();

            //Parallel.ForEach(pngs, new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism }, (fileName) =>
            //{
            //    var pdf = new PdfFile();

            //    string name = Path.GetFileNameWithoutExtension(fileName);
            //    pdf.page_number = int.Parse(name); 
            //    pdf.content = OCRImage(fileName);
            //    pdflist.Add(pdf);
            //    File.Delete(fileName);
            //});

            //var listresult = pdflist.OrderBy(x => x.page_number).ToList();
            //foreach (var x in listresult)
            //{
            //    string result = x.page_number.ToString() + "\n" + x.content;
            //    Console.Write(result);
            //    output= output + result;

            //}
            //return output;

            //string pdfFilePath = @"C:\Users\Mustafa\Desktop\Signal and Systems-Simon Haykin-Wiley.pdf";
            //string notOcrText = GetTextFromPDF(path);
            //string notOcrText = "";
            //bool xabc = PDFHasImages(path);  Wont Control That because its try almost all pdf  . we decide If pdf has already text in it we will just use that PDFHasImages(path) || 
            //if (Regex.Matches(notOcrText, @"[a-zA-Z]").Count == 0)
            //{

                string projectPath = GlobalVariables.MyAppPath;
                string filename = Path.GetFileNameWithoutExtension(path);
                // Create sub directory
                string subdir = projectPath + @"\TempFolder" + @"\TempImages\" + filename;
                if (!Directory.Exists(subdir))
                {
                    Directory.CreateDirectory(subdir);
                }
                string exFilePath = subdir + @"\" + " % d.png";
                string exFolderPath = subdir;
                //int psLanguageLevel = 3;
                var coreCount = Environment.ProcessorCount;

                while(true)
                {
                    try
                    {
                        GhostscriptAPI gs = new GhostscriptAPI();



                        gs.AddParam("-q");

                        gs.AddParam("-dPARANOIDSAFER");
                        gs.AddParam("-dNOPAUSE");
                        gs.AddParam("-dBATCH");
                        gs.AddParam("-dNOPROMPT");

                        gs.AddParam("-dMaxBitmap = 500000000");
                        //gs.AddParam("-c 30000000 setvmthreshold");
                        gs.AddParam("-dNumRenderingThreads =" + coreCount.ToString());
                        gs.AddParam("-dNOGC");
                        gs.AddParam("-dQUIET");
                        gs.AddParam("-o singlepage-pnggray-%03d.png");
                        gs.AddParam("-sDEVICE=pnggray");
                        gs.AddParam("-r300");
                        //gs.AddParam("-dGraphicsAlphaBits = 4");
                        //gs.AddParam("-dTextAlphaBits=4");

                        gs.AddParam("-dNOINTERPOLATE");
                        //gs.AddParam("-dLanguageLevel=" + psLanguageLevel.ToString());
                        //gs.AddParam("-dSetPageSize");
                        gs.AddParam("-sOutputFile=" + exFilePath);

                        //gs.AddParam("dFirstPage=1");
                        //gs.AddParam("dLastPage=1");
                        //string filepath= ${path//\\}
                        gs.AddParam(path);
                        gs.Execute();
                    var images = Directory.GetFiles(subdir, "*.png"    , SearchOption.TopDirectoryOnly);
                    if (!images.Any())
                        throw new DirectoryNotFoundException();
                        //File.Delete(path);
                        break;
                    }
                    catch (System.BadImageFormatException )
                    {
                        
                        GhostscriptAPI.CopyGhostScriptDll();//copy the dll file for ghostscript if app x64 replace with 64bit dll else replace with 32bit !! necessary for Apply OCR TO PDF GHOSTSCRIPT CONVERT PDFPAGES TO IMAGES(PNG IN THAT CASE)
                       Thread.Sleep(1000);

                    }
                    catch (System.DllNotFoundException ) 
                    {
                        
                        GhostscriptAPI.CopyGhostScriptDll();//copy the dll file for ghostscript if app x64 replace with 64bit dll else replace with 32bit !! necessary for Apply OCR TO PDF GHOSTSCRIPT CONVERT PDFPAGES TO IMAGES(PNG IN THAT CASE)
                        Thread.Sleep(1000);

                    }
                catch(DirectoryNotFoundException)
                {
                    Thread.Sleep(10000);
                }

                }
                var pngs = Directory.GetFiles(exFolderPath, "*.png");

                string output = string.Empty;
                List<PdfFile> pdflist = new List<PdfFile>();

                //Parallel.ForEach(pngs, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount-2}, (fileName) =>
                //{
                foreach (var fileName in pngs)
                {


                    var pdf = new PdfFile();

                    pdf.page_number = Int32.Parse(Path.GetFileNameWithoutExtension(fileName));

                    pdf.content = OCRImage(fileName);


                    pdflist.Add(pdf);
                    //File.Delete(fileName);
                }
                //});

                var listresult = pdflist.OrderBy(x => x.page_number).ToList();
                foreach (var x in listresult)
                {
                    string result =  "\n" + x.content;

                    output = output + result;

                }
                //foreach (string file in pngs)
                //{
                //    File.Delete(file);

                //}

                Directory.Delete(exFolderPath);
                return output;

           








        }

        public static string GetTextFromPDF(String pdfPath)
        {
            try
            {
                PdfReader reader = new PdfReader(pdfPath);

                StringWriter output = new StringWriter();
                string[] words = null;
                string line = null;
                string result = null;
                for (int i = 1; i <= reader.NumberOfPages; i++)
                {
                    var text = PdfTextExtractor.GetTextFromPage(reader, i, new LocationTextExtractionStrategy());
                    //output.WriteLine(PdfTextExtractor.GetTextFromPage(reader, i, new SimpleTextExtractionStrategy()));
                    words = text.Split('\n'); ;

                    for (int j = 0, len = words.Length; j < len; j++)
                    {
                        line = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(words[j]));
                        result += "\n" + line;
                    }
                }
                output.Close();
                reader.Dispose();
                return result;
            }
            catch
            {
                
                return "";
            }
        }

        public static bool PDFHasImages(string sourcePdf)
        {
            try
            {
                // NOTE:  This will only get the first image it finds per page.
                PdfReader pdf = new PdfReader(sourcePdf);
                RandomAccessFileOrArray raf = new iTextSharp.text.pdf.RandomAccessFileOrArray(sourcePdf);

                try
                {
                    for (int pageNumber = 1; pageNumber <= pdf.NumberOfPages; pageNumber++)
                    {
                        PdfDictionary pg = pdf.GetPageN(pageNumber);

                        // recursively search pages, forms and groups for images.
                        PdfObject obj = FindImageInPDFDictionary(pg);
                        if (obj != null)
                        {

                            int XrefIndex = Convert.ToInt32(((PRIndirectReference)obj).Number.ToString(System.Globalization.CultureInfo.InvariantCulture));
                            PdfObject pdfObj = pdf.GetPdfObject(XrefIndex);
                            PdfStream pdfStrem = (PdfStream)pdfObj;
                            byte[] bytes = PdfReader.GetStreamBytesRaw((PRStream)pdfStrem);
                            if ((bytes != null))
                            {
                                //using (System.IO.MemoryStream memStream = new System.IO.MemoryStream(bytes))
                                //{
                                //    memStream.Position = 0;
                                //    System.Drawing.Image img = System.Drawing.Image.FromStream(memStream);
                                //    // must save the file while stream is open.
                                //    if (!Directory.Exists(outputPath))
                                //        Directory.CreateDirectory(outputPath);

                                //    string path = Path.Combine(outputPath, String.Format(@"{0}.jpg", pageNumber));
                                //    System.Drawing.Imaging.EncoderParameters parms = new System.Drawing.Imaging.EncoderParameters(1);
                                //    parms.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Compression, 0);
                                //    //System.Drawing.Imaging.ImageCodecInfo jpegEncoder = ImageCodecInfo.GetImageEncoders("JPEG");
                                //    img.Save(path);
                                //}
                                return true;
                            }
                        }
                    }
                }
                catch
                {
                    return false;
                }
                finally
                {
                    pdf.Close();
                    raf.Close();
                }
                return false;

            }
            catch { return false; }
        }







        private static PdfObject FindImageInPDFDictionary(PdfDictionary pg)
        {
            PdfDictionary res =
                (PdfDictionary)PdfReader.GetPdfObject(pg.Get(PdfName.RESOURCES));


            PdfDictionary xobj =
              (PdfDictionary)PdfReader.GetPdfObject(res.Get(PdfName.XOBJECT));
            if (xobj != null)
            {
                foreach (PdfName name in xobj.Keys)
                {

                    PdfObject obj = xobj.Get(name);
                    if (obj.IsIndirect())
                    {
                        PdfDictionary tg = (PdfDictionary)PdfReader.GetPdfObject(obj);

                        PdfName type =
                          (PdfName)PdfReader.GetPdfObject(tg.Get(PdfName.SUBTYPE));

                        //image at the root of the pdf
                        if (PdfName.IMAGE.Equals(type))
                        {
                            return obj;
                        }// image inside a form
                        else if (PdfName.FORM.Equals(type))
                        {
                            return FindImageInPDFDictionary(tg);
                        } //image inside a group
                        else if (PdfName.GROUP.Equals(type))
                        {
                            return FindImageInPDFDictionary(tg);
                        }

                    }
                }
            }

            return null;

        }







    }
}
