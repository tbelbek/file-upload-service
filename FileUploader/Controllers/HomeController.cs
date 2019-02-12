using FileUploader.Database;
using FileUploader.Helper;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using QRCoder;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

namespace FileUploader.Controllers
{
    public class HomeController : Controller
    {
        public FileUploaderContext DbContext { get; set; }
        public HomeController()
        {
            DbContext = new FileUploaderContext();
        }
        public ActionResult Index()
        {
            return View();
        }

        [System.Web.Mvc.HttpPost]
        public JsonResult UploadFile()
        {
            bool isSavedSuccessfully = false;

            var foldername = $"/UploadedFiles/FileSequence-{Guid.NewGuid().ToString()}/";

            DeleteOldFiles();

            Directory.CreateDirectory(foldername);

            var userId = Request.Headers["UserSessionCookie"].ToString();

            var fileDbName = string.Empty;
            foreach (string fileName in Request.Files)
            {
                HttpPostedFileBase file = Request.Files[fileName];

                string nameAndLocation = foldername + file.FileName;

                fileDbName = file.FileName;

                file.SaveAs(nameAndLocation);

                isSavedSuccessfully = true;
            }

            var downloadFilePath = $"/UploadedFiles/{Guid.NewGuid()}.zip";
            FileStream fsOut = System.IO.File.Create(downloadFilePath);
            ZipOutputStream zipStream = new ZipOutputStream(fsOut);

            zipStream.SetLevel(3); //0-9, 9 being the highest level of compression

            // This setting will strip the leading part of the folder path in the entries, to
            // make the entries relative to the starting folder.
            // To include the full path for each entry up to the drive root, assign folderOffset = 0.
            int folderOffset = foldername.Length + (foldername.EndsWith("/") ? 0 : 1);

            CompressFolder(foldername, zipStream, folderOffset);

            zipStream.IsStreamOwner = true; // Makes the Close also Close the underlying stream
            zipStream.Close();
            var fileHash = Regex.Replace(CalculateMD5(downloadFilePath), "[^0-9.]", "").Substring(0, 9);

            DbContext.Files.RemoveRange(DbContext.Files.Where(t => t.HashVal == fileHash));
            var dbObject = new Files() { FilePath = downloadFilePath, HashVal = fileHash, Id = Guid.NewGuid(), UploadDate = DateTime.Now, FileName = fileDbName, UserSessionId = userId };
            DbContext.Files.Add(dbObject);
            DbContext.SaveChanges();

            var fileUrlId = Bijective.Encode(Convert.ToInt32(dbObject.HashVal), AlphabetTest.Base16);
            string baseUrl = $"{Request.Url.GetLeftPart(UriPartial.Authority)}{Url.Content("~")}FileLink/{fileUrlId}";

            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(baseUrl, QRCodeGenerator.ECCLevel.Q);
            PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeAsPngByteArr = qrCode.GetGraphic(20);

            return Json(new { Url = baseUrl, QrCode = $"data:image/png;base64, {Convert.ToBase64String(qrCodeAsPngByteArr)}" });
        }

        private static void DeleteOldFiles()
        {
            System.IO.DirectoryInfo di = new DirectoryInfo("/UploadedFiles/");

            foreach (FileInfo file in di.GetFiles())
            {
                if (file.CreationTime < DateTime.Now.AddMonths(-3))
                {
                    file.Delete();
                }
            }

            foreach (DirectoryInfo dir in di.GetDirectories())
            {

                foreach (FileInfo file in dir.GetFiles())
                {
                    if (file.CreationTime < DateTime.Now.AddMonths(-3))
                    {
                        file.Delete();
                    }
                }

                dir.Delete(true);
            }
        }

        public FileResult GetFile(string fileId)
        {
            var fileUrlId = Bijective.Decode(fileId, AlphabetTest.Base16);
            var obj = DbContext.Files.FirstOrDefault(t => t.HashVal.EndsWith(fileUrlId.ToString()));
            var bytes = GetFileData(obj.FilePath);
            return File(bytes, System.Net.Mime.MediaTypeNames.Application.Octet, Path.GetFileName(obj.FileName));
        }

        public JsonResult CreateSessionCookie()
        {
            return Json(CookieGenerator(), JsonRequestBehavior.AllowGet);
        }

        private string CookieGenerator()
        {
            Guid g = Guid.NewGuid();
            string GuidString = Convert.ToBase64String(g.ToByteArray());
            GuidString = GuidString.Replace("=", "");
            GuidString = GuidString.Replace("+", "");
            return GuidString;
        }

        public byte[] GetFileData(string s)
        {
            System.IO.FileStream fs = System.IO.File.OpenRead(s);
            byte[] data = new byte[fs.Length];
            int br = fs.Read(data, 0, data.Length);
            if (br != fs.Length)
                throw new System.IO.IOException(s);
            return data;
        }

        static string CalculateMD5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = System.IO.File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        private void CompressFolder(string path, ZipOutputStream zipStream, int folderOffset)
        {

            string[] files = Directory.GetFiles(path);

            foreach (string filename in files)
            {

                FileInfo fi = new FileInfo(filename);

                string entryName = filename.Substring(folderOffset); // Makes the name in zip based on the folder
                entryName = ZipEntry.CleanName(entryName); // Removes drive from name and fixes slash direction
                ZipEntry newEntry = new ZipEntry(entryName);
                newEntry.DateTime = fi.LastWriteTime; // Note the zip format stores 2 second granularity

                // Specifying the AESKeySize triggers AES encryption. Allowable values are 0 (off), 128 or 256.
                // A password on the ZipOutputStream is required if using AES.
                //   newEntry.AESKeySize = 256;

                // To permit the zip to be unpacked by built-in extractor in WinXP and Server2003, WinZip 8, Java, and other older code,
                // you need to do one of the following: Specify UseZip64.Off, or set the Size.
                // If the file may be bigger than 4GB, or you do not need WinXP built-in compatibility, you do not need either,
                // but the zip will be in Zip64 format which not all utilities can understand.
                //   zipStream.UseZip64 = UseZip64.Off;
                newEntry.Size = fi.Length;

                zipStream.PutNextEntry(newEntry);

                // Zip the file in buffered chunks
                // the "using" will close the stream even if an exception occurs
                byte[] buffer = new byte[4096];
                using (FileStream streamReader = System.IO.File.OpenRead(filename))
                {
                    StreamUtils.Copy(streamReader, zipStream, buffer);
                }
                zipStream.CloseEntry();
            }
            string[] folders = Directory.GetDirectories(path);
            foreach (string folder in folders)
            {
                CompressFolder(folder, zipStream, folderOffset);
            }
        }

        private static string SaveSharedFile(int userAlertId, HttpPostedFileBase file)
        {
            string fileName = null;
            fileName = System.IO.Path.GetFileName(file.FileName);
            if (fileName != "")
            {
                const int BufferSize = 65536; // 65536 = 64 Kilobytes
                string Filepath = userAlertId.ToString();
                using (FileStream fs = System.IO.File.Create(Filepath))
                {
                    using (Stream reader = System.Web.HttpContext.Current.Request.GetBufferlessInputStream())
                    {
                        byte[] buffer = new byte[BufferSize];
                        int read = -1, pos = 0;
                        do
                        {
                            int len = (file.ContentLength < pos + BufferSize ?
                                file.ContentLength - pos :
                                BufferSize);
                            read = reader.Read(buffer, 0, len);
                            fs.Write(buffer, 0, len);
                            pos += read;
                        } while (read > 0);
                    }
                }
            }
            return fileName;
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}