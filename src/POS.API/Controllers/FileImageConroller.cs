
// using CloudinaryDotNet;
// using CloudinaryDotNet.Actions;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.AspNetCore.Authorization;

// namespace POS.API.Controllers
// {
//     [Route("api/[controller]")]
//     [ApiController]
//     public class FileStorageController : ControllerBase
//     {
//         private readonly Cloudinary _cloudinary;
//         private readonly IWebHostEnvironment _env;
//         private readonly string _uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");

//         public FileStorageController(IConfiguration config, IWebHostEnvironment env)
//         {
//             _env = env;
//             var account = new Account(
//                 config["Cloudinary:CloudName"],
//                 config["Cloudinary:ApiKey"],
//                 config["Cloudinary:ApiSecret"]
//             );
//             _cloudinary = new Cloudinary(account);
//         }

//         [HttpPost("upload")]
//         [AllowAnonymous]
//         public async Task<IActionResult> Upload(IFormFile file)
//         {
//             try
//             {
//                 if (file == null || file.Length == 0)
//                     return BadRequest(new { error = "No file uploaded" });

//                 if (_env.IsDevelopment())
//                 {
//                     // 🖥️ Local → Save to Folder
//                     if (!Directory.Exists(_uploadFolder))
//                         Directory.CreateDirectory(_uploadFolder);

//                     var uniqueFileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
//                     var filePath = Path.Combine(_uploadFolder, uniqueFileName);

//                     using (var stream = new FileStream(filePath, FileMode.Create))
//                     {
//                         await file.CopyToAsync(stream);
//                     }

//                     var localUrl = $"{Request.Scheme}://{Request.Host}/Uploads/{uniqueFileName}";
//                     return Ok(new { fileName = uniqueFileName, url = localUrl });
//                 }
//                 else
//                 {
//                     // 🚀 Production → Save to Cloudinary
//                     using var stream = file.OpenReadStream();
//                     var uploadParams = new ImageUploadParams
//                     {
//                         File = new FileDescription(file.FileName, stream),
//                         Folder = "sarana-pos"
//                     };

//                     var result = await _cloudinary.UploadAsync(uploadParams);
//                     return Ok(new { fileName = result.PublicId, url = result.SecureUrl.ToString() });
//                 }
//             }
//             catch (Exception ex)
//             {
//                 return StatusCode(500, new { error = $"Upload failed: {ex.Message}" });
//             }
//         }
//     }
// }






using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace POS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileStorageController : ControllerBase
    {
        private readonly Cloudinary _cloudinary;
        private readonly IWebHostEnvironment _env;
        private readonly string _uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");

        public FileStorageController(IConfiguration config, IWebHostEnvironment env)
        {
            _env = env;
            var account = new Account(
                config["Cloudinary:CloudName"],
                config["Cloudinary:ApiKey"],
                config["Cloudinary:ApiSecret"]
            );
            _cloudinary = new Cloudinary(account);
        }

        // ✅ Upload — Local or Cloudinary
        [HttpPost("upload")]
        [AllowAnonymous]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { error = "No file uploaded" });

                if (_env.IsDevelopment())
                {
                    if (!Directory.Exists(_uploadFolder))
                        Directory.CreateDirectory(_uploadFolder);

                    var uniqueFileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                    var filePath = Path.Combine(_uploadFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var localUrl = $"{Request.Scheme}://{Request.Host}/Uploads/{uniqueFileName}";
                    return Ok(new { fileName = uniqueFileName, url = localUrl });
                }
                else
                {
                    using var stream = file.OpenReadStream();
                    var uploadParams = new ImageUploadParams
                    {
                        File = new FileDescription(file.FileName, stream),
                        Folder = "sarana-pos"
                    };

                    var result = await _cloudinary.UploadAsync(uploadParams);
                    return Ok(new { fileName = result.PublicId, url = result.SecureUrl.ToString() });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Upload failed: {ex.Message}" });
            }
        }

        // ✅ Delete — Local Folder or Cloudinary
        [HttpDelete("delete")]
        [AllowAnonymous]
        public async Task<IActionResult> Delete([FromQuery] string fileUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(fileUrl))
                    return BadRequest(new { error = "File URL is required" });

                if (_env.IsDevelopment())
                {
                    // 🖥️ Local → Delete ពី Folder
                    var fileName = Path.GetFileName(new Uri(fileUrl).LocalPath);
                    var filePath = Path.Combine(_uploadFolder, fileName);

                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);

                    return Ok(new { message = "File deleted successfully" });
                }
                else
                {
                    // 🚀 Production → Delete ពី Cloudinary
                    // URL: https://res.cloudinary.com/cloud/image/upload/v123456/sarana-pos/filename.jpg
                    // PublicId: sarana-pos/filename
                    var uri = new Uri(fileUrl);
                    var segments = uri.AbsolutePath.Split('/');
                    var uploadIndex = Array.IndexOf(segments, "upload");

                    // Skip "upload" + version (v123456) → ចាប់ Folder/FileName
                    var publicIdWithExt = string.Join("/", segments.Skip(uploadIndex + 2));
                    var publicId = Path.ChangeExtension(publicIdWithExt, null); // លុប Extension

                    var deleteParams = new DeletionParams(publicId);
                    var result = await _cloudinary.DestroyAsync(deleteParams);

                    if (result.Result == "ok")
                        return Ok(new { message = "File deleted from Cloudinary successfully" });
                    else
                        return BadRequest(new { error = $"Cloudinary delete failed: {result.Result}" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Delete failed: {ex.Message}" });
            }
        }
    }
}