using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace PrintPdf_Console
{
    public class Program
    {
        static void Main(string[] args)
        {
            if (Directory.Exists("segments"))
                Directory.Delete("segments", true);

            if (Directory.Exists("out"))
                Directory.Delete("out", true);


            Directory.CreateDirectory("segments");
            Directory.CreateDirectory("out");

            List<string> originPath = Directory.GetFiles("input", "*.png").OrderBy(x => x).ToList();

            var oneFile = CombineImagesToSingle(originPath.Select(x => new FileInfo(x)).ToArray());

            var splitToSegments = SplitToSegments(oneFile);
            var smallerPages = ConvertToSmallerPages(splitToSegments);
            GeneratePdfFromImage(smallerPages);
        }

        public static string GeneratePdfFromImage(List<Tuple<string, bool>> imgList)
        {
            PdfSharp.Pdf.PdfDocument document = new PdfSharp.Pdf.PdfDocument();
            int width = 595;
            foreach (var data in imgList)
            {
                var imagePath = data.Item1;
                var isBreakPage = data.Item2;
                using (PdfSharp.Drawing.XImage img = PdfSharp.Drawing.XImage.FromGdiPlusImage(Image.FromFile(imagePath)))
                {
                    var height = (int)(((double)width / (double)img.PixelWidth) * img.PixelHeight);
                    PdfSharp.Pdf.PdfPage page = document.AddPage();
                    page.Size = PdfSharp.PageSize.A4;
                    page.Orientation = PdfSharp.PageOrientation.Portrait;
                    if (isBreakPage)
                    {
                        page.TrimMargins.Top = 50;
                    }

                    page.TrimMargins.Right = 50;
                    page.TrimMargins.Left = 50;

                    PdfSharp.Drawing.XGraphics gfx = PdfSharp.Drawing.XGraphics.FromPdfPage(page);
                    gfx.DrawImage(img, 0, 0, width, height);
                    gfx.Dispose();
                }
            }

            string fileName = "te2_profile_" + Guid.NewGuid().ToString("N").Substring(0, 4) + ".pdf";
            document.Save(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName));

            return fileName;
        }

        // bởi vì get từ chrome, có nhiều file lẻ, nên merge lại thành 1 file lớn trước đã (whole page)
        public static string CombineImagesToSingle(FileInfo[] files)
        {

            string finalImage = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), @"all.jpg");
            List<int> imageHeights = new List<int>();
            int nIndex = 0;
            int width = 0;
            foreach (FileInfo file in files)
            {
                Image img = Image.FromFile(file.FullName);
                imageHeights.Add(img.Height);
                width = img.Width;
                img.Dispose();
            }

            int height = imageHeights.Sum();
            Bitmap img3 = new Bitmap(width, height);
            Graphics g = Graphics.FromImage(img3);
            g.Clear(SystemColors.AppWorkspace);
            foreach (FileInfo file in files)
            {
                Image img = Image.FromFile(file.FullName);
                if (nIndex == 0)
                {
                    g.DrawImage(img, new Point(0, 0));
                    nIndex++;
                    height = img.Height;
                }
                else
                {
                    g.DrawImage(img, new Point(0, height));
                    height += img.Height;
                }
                img.Dispose();
            }
            g.Dispose();
            img3.Save(finalImage, ImageFormat.Jpeg);
            img3.Dispose();

            return finalImage;
        }

        public static List<Tuple<string, bool>> ConvertToSmallerPages(string[] segments)
        {
            int pageWidth = 595;
            int pageHeight = 842;
            var pdfImages = new List<Tuple<string, bool>>();
            int index = 0;

            for (var pageIndex = 0; pageIndex < segments.Length; pageIndex++)
            {
                var item = segments[pageIndex];

                Bitmap originalImage = (Bitmap)Image.FromFile(item);

                var scale = pageWidth * 1.0f / originalImage.Width;
                var actualHeigh = (int)Math.Floor(pageHeight / scale);
                var heightPosition = 0;

                while (heightPosition < originalImage.Height)
                {
                    var currentPageHeight = actualHeigh;
                    var remainHeight = originalImage.Height - heightPosition;

                    if (remainHeight < actualHeigh)
                    {
                        currentPageHeight = remainHeight;
                    }

                    Rectangle rect = new Rectangle(0, heightPosition, originalImage.Width, currentPageHeight);
                    Bitmap segmentBitmap = originalImage.Clone(rect, originalImage.PixelFormat);

                    index++;
                    segmentBitmap.Save("out\\" + index + ".jpg", ImageFormat.Jpeg);

                    //var mstream = new MemoryStream();
                    //segmentBitmap.Save(mstream, ImageFormat.Jpeg);
                    pdfImages.Add(new Tuple<string, bool>("out\\" + index + ".jpg", heightPosition == 0));
                    // do not dispose mstream here.

                    segmentBitmap.Dispose();
                    heightPosition += actualHeigh;
                }
                originalImage.Dispose();
            }

            return pdfImages;
        }

        //tách 1 file lớn thành các điểm file nhỏ
        public static string[] SplitToSegments(string oneFile)
        {
            Console.WriteLine("Open file 'segment_point.txt' and input 'break point'. Then enter to continue");

#if CONSOLE
            var key = Console.ReadKey();
#endif
            int[] locations = File.ReadAllLines("segment_point.txt").Select(x => int.Parse(x)).ToArray();
            if (locations.Length < 2) throw new Exception("Must enter 2+ segment_point.txt");
            List<string> files = new List<string>();
            Bitmap img = (Bitmap)Image.FromFile(oneFile);
            for (int i = 0; i < locations.Length; i++)
            {
                var fileOut = @"segments\" + i + ".jpeg";
                if (i == 0)
                {
                    Rectangle rect = new Rectangle(0, 0, img.Width, locations[i]);
                    var img1 = img.Clone(rect, img.PixelFormat);
                    img1.Save(fileOut, ImageFormat.Jpeg);
                }
                else
                {
                    Rectangle rect = new Rectangle(0, locations[i - 1], img.Width, locations[i] - locations[i - 1]);
                    var img1 = img.Clone(rect, img.PixelFormat);
                    img1.Save(fileOut, ImageFormat.Jpeg);
                }

                files.Add(fileOut);
            }

            Rectangle rect2 = new Rectangle(0, locations[locations.Length - 1], img.Width, img.Height - locations[locations.Length - 1]);
            var img2 = img.Clone(rect2, img.PixelFormat);
            img2.Save(@"segments\" + locations.Length + ".jpeg", ImageFormat.Jpeg);
            files.Add(@"segments\" + locations.Length + ".jpeg");

            return files.ToArray();
        }
    }
}
