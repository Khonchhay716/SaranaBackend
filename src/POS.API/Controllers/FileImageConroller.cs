// using Microsoft.AspNetCore.Mvc;
// using Microsoft.AspNetCore.Authorization;

// namespace POS.API.Controllers
// {
//     [Route("api/[controller]")]
//     [ApiController]
//     public class FileStorageController : ControllerBase
//     {
//         private readonly string _uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");

//         public FileStorageController()
//         {
//             // ✅ Create folder if it doesn't exist
//             if (!Directory.Exists(_uploadFolder))
//             {
//                 Directory.CreateDirectory(_uploadFolder);
//             }
//         }

//         [HttpPost("upload")]
//         [AllowAnonymous]
//         public async Task<IActionResult> Upload(IFormFile file)
//         {
//             try
//             {
//                 if (file == null || file.Length == 0)
//                     return BadRequest(new { error = "No file uploaded" });

//                 // ✅ Generate unique filename to prevent overwrite
//                 var extension = Path.GetExtension(file.FileName);
//                 var uniqueFileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{Guid.NewGuid()}{extension}";
//                 var filePath = Path.Combine(_uploadFolder, uniqueFileName);

//                 // ✅ Save file
//                 using (var stream = new FileStream(filePath, FileMode.Create))
//                 {
//                     await file.CopyToAsync(stream);
//                 }

//                 // ✅ Generate URL
//                 var fileUrl = $"{Request.Scheme}://{Request.Host}/Uploads/{uniqueFileName}";

//                 return Ok(new
//                 {
//                     fileName = uniqueFileName,
//                     url = fileUrl
//                 });
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
                    // 🖥️ Local → Save to Folder
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
                    // 🚀 Production → Save to Cloudinary
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
    }
}