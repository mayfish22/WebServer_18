using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using System.Globalization;
using System.Net;
using System.Text;
using WebServer.Extensions;
using WebServer.Models.WebServerDB;

namespace WebServer.Controllers
{
    public class StreamingController : Controller
    {
        private readonly ILogger<StreamingController> _logger;
        private static readonly FormOptions _defaultFormOptions = new FormOptions();
        private string[] _permittedExtensions = new string[] { ".jpg", ".png" }; //允許的檔案類型
        private long _fileSizeLimit = 50 * 1024 * 1024; // 50MB, 檔案大小限制
        private string _targetFilePath; // 儲存路徑
        private readonly WebServerDBContext _WebServerDBContext;

        public StreamingController(ILogger<StreamingController> logger
            , WebServerDBContext WebServerDBContext)
        {
            _logger = logger;
            _WebServerDBContext = WebServerDBContext;
            //檔案儲存路徑
            _targetFilePath = Path.GetTempPath();

            _logger.LogInformation("FilePath: " + _targetFilePath);
        }

        [HttpPost]
        [DisableFormValueModelBinding]
        public async Task<IActionResult> Upload()
        {
            try
            {
                //記錄本次上傳的檔案
                var ids = new List<string>();

                if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
                {
                    ModelState.AddModelError("File",
                        $"The request couldn't be processed (Error 1).");
                    // Log error

                    return BadRequest(ModelState);
                }

                // Accumulate the form data key-value pairs in the request (formAccumulator).
                var formAccumulator = new KeyValueAccumulator();
                var trustedFileNameForDisplay = string.Empty;
                var untrustedFileNameForStorage = string.Empty;
                var streamedFileContent = Array.Empty<byte>();

                var boundary = MultipartRequestHelper.GetBoundary(
                    MediaTypeHeaderValue.Parse(Request.ContentType),
                    _defaultFormOptions.MultipartBoundaryLengthLimit);
                var reader = new MultipartReader(boundary, HttpContext.Request.Body);

                var section = await reader.ReadNextSectionAsync();

                while (section != null)
                {
                    var hasContentDispositionHeader =
                        ContentDispositionHeaderValue.TryParse(
                            section.ContentDisposition, out var contentDisposition);

                    if (hasContentDispositionHeader)
                    {
                        if (MultipartRequestHelper
                            .HasFileContentDisposition(contentDisposition))
                        {
                            untrustedFileNameForStorage = contentDisposition.FileName.Value;
                            // Don't trust the file name sent by the client. To display
                            // the file name, HTML-encode the value.
                            trustedFileNameForDisplay = WebUtility.HtmlEncode(
                                    contentDisposition.FileName.Value);

                            streamedFileContent =
                                await FileHelpers.ProcessStreamedFile(section, contentDisposition,
                                    ModelState, _permittedExtensions, _fileSizeLimit);

                            if (!ModelState.IsValid)
                            {
                                return BadRequest(ModelState);
                            }

                            var fileId = Guid.NewGuid().ToString().ToUpper();
                            var fileName = trustedFileNameForDisplay;
                            var filePath = Path.Combine(_targetFilePath, fileId);
                            //儲存檔案
                            using (var targetStream = System.IO.File.Create(filePath))
                            {
                                await targetStream.WriteAsync(streamedFileContent);
                            }
                            //寫入資料表
                            await _WebServerDBContext.File.AddAsync(new WebServer.Models.WebServerDB.File
                            {
                                ID = fileId,
                                Name = fileName,
                                Path = filePath,
                            });
                            await _WebServerDBContext.SaveChangesAsync();
                            ids.Add(fileId);
                        }
                        else if (MultipartRequestHelper
                            .HasFormDataContentDisposition(contentDisposition))
                        {
                            // Don't limit the key name length because the 
                            // multipart headers length limit is already in effect.
                            var key = HeaderUtilities
                                .RemoveQuotes(contentDisposition.Name).Value;
                            var encoding = GetEncoding(section);

                            if (encoding == null)
                            {
                                ModelState.AddModelError("File",
                                    $"The request couldn't be processed (Error 2).");
                                // Log error

                                return BadRequest(ModelState);
                            }

                            using (var streamReader = new StreamReader(
                                section.Body,
                                encoding,
                                detectEncodingFromByteOrderMarks: true,
                                bufferSize: 1024,
                                leaveOpen: true))
                            {
                                // The value length limit is enforced by 
                                // MultipartBodyLengthLimit
                                var value = await streamReader.ReadToEndAsync();

                                if (string.Equals(value, "undefined",
                                    StringComparison.OrdinalIgnoreCase))
                                {
                                    value = string.Empty;
                                }

                                formAccumulator.Append(key, value);

                                if (formAccumulator.ValueCount >
                                    _defaultFormOptions.ValueCountLimit)
                                {
                                    // Form key count limit of 
                                    // _defaultFormOptions.ValueCountLimit 
                                    // is exceeded.
                                    ModelState.AddModelError("File",
                                        $"The request couldn't be processed (Error 3).");
                                    // Log error

                                    return BadRequest(ModelState);
                                }
                            }
                        }
                    }

                    // Drain any remaining section body that hasn't been consumed and
                    // read the headers for the next section.
                    section = await reader.ReadNextSectionAsync();
                }

                // Bind form data to the model
                var formData = new FormData();
                var formValueProvider = new FormValueProvider(
                    BindingSource.Form,
                    new FormCollection(formAccumulator.GetResults()),
                    CultureInfo.CurrentCulture);
                var bindingSuccessful = await TryUpdateModelAsync(formData, prefix: "",
                    valueProvider: formValueProvider);

                if (!bindingSuccessful)
                {
                    ModelState.AddModelError("File",
                        "The request couldn't be processed (Error 5).");
                    // Log error
                    return BadRequest(ModelState);
                }

                return Json(new { message = formData.Message, ids = ids });
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        public class FormData
        {
            public string Message { get; set; }
        }

        public static Encoding GetEncoding(MultipartSection section)
        {
            MediaTypeHeaderValue mediaType;
            var hasMediaTypeHeader = MediaTypeHeaderValue.TryParse(section.ContentType, out mediaType);
            if (!hasMediaTypeHeader || Encoding.UTF7.Equals(mediaType.Encoding))
            {
                return Encoding.UTF8;
            }
            return mediaType.Encoding;
        }

        //https://localhost:7120/Streaming/Download/{id}
        [Route("[controller]/[action]/{id}")]
        [HttpGet]
        public async Task<IActionResult> Download(string id)
        {
            try
            {
                id = (id ?? "").ToUpper();
                var file = await _WebServerDBContext.File.FindAsync(id);
                if(file == null)
                    throw new Exception("找不到檔案編號");

                var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
                string contentType;
                //取得檔案 MIME Type
                if (!provider.TryGetContentType(file.Name, out contentType))
                {
                    contentType = "application/octet-stream";
                }
                //讀取檔案
                using (FileStream fsSource = new FileStream(file.Path, FileMode.Open, FileAccess.Read))
                {
                    // Read the source file into a byte array.
                    byte[] bytes = new byte[fsSource.Length];
                    int numBytesToRead = (int)fsSource.Length;
                    int numBytesRead = 0;
                    while (numBytesToRead > 0)
                    {
                        // Read may return anything from 0 to numBytesToRead.
                        int n = fsSource.Read(bytes, numBytesRead, numBytesToRead);
                        // Break when the end of the file is reached.
                        if (n == 0)
                            break;
                        numBytesRead += n;
                        numBytesToRead -= n;
                    }
                    return new FileStreamResult(new MemoryStream(bytes), contentType)
                    {
                        FileDownloadName = file.Name,
                    };
                }
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}