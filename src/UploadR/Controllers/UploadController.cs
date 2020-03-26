﻿using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UploadR.Enums;
using UploadR.Models;
using UploadR.Services;

namespace UploadR.Controllers
{
    [Route("api/[controller]"), ApiController]
    public class UploadController : Controller
    {
        private readonly UploadService _uploadService;

        /// <summary>
        ///     Controller related to uploads management.
        /// </summary>
        /// <param name="uploadService">Service for uploads management.</param>
        public UploadController(UploadService uploadService)
        {
            _uploadService = uploadService;
        }
        
        /// <summary>
        ///     Upload the given files to the server. Returns a model representing failed and succeeded uploads.
        /// </summary>
        /// <param name="model">Model containing the password to put on every upload. Null if no password.</param>
        [HttpPost, Authorize]
        public async Task<IActionResult> UploadAsync(
            [FromForm] UploadModel model)
        {
            var userId = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
            return Json(await _uploadService.UploadAsync(userId?.Value, Request.Form.Files, model.Password));
        }

        /// <summary>
        ///     Deletes the upload by its name or guid.
        /// </summary>
        /// <param name="filename">Complete name of the file or its guid.</param>
        [HttpDelete("{filename}"), Authorize]
        public async Task<IActionResult> DeleteUploadAsync(string filename)
        {
            var userId = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
            var result = await _uploadService.DeleteUploadAsync(userId?.Value, filename);

            return result switch
            {
                ResultCode.Ok => Ok(),
                ResultCode.NotFound => BadRequest(),
                ResultCode.Unauthorized => Unauthorized(),
                _ => BadRequest()
            };
        }

        /// <summary>
        ///     Gets the details of an upload.
        /// </summary>
        /// <param name="filename">Complete name of the file or its guid.</param>
        [HttpGet("{filename}/details"), Authorize]
        public async Task<IActionResult> GetUploadDetailsAsync(string filename)
        {
            var result = await _uploadService.GetUploadDetailsAsync(filename);
            if (result is null)
            {
                return NotFound();
            }
            
            return Json(result);
        }

        /// <summary>
        ///     Gets the details of all the uploads sent by the specified user.
        /// </summary>
        /// <param name="userId">Id of the user to lookup.</param>
        [HttpGet("{userId}/uploads"), Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUserUploadsDetailsAsync(string userId)
        {
            var result = await _uploadService.GetUploadsDetailsAsync(userId);
            if (result is null)
            {
                return BadRequest();
            }
            
            return Json(result);
        }
        
        /// <summary>
        ///     Gets the details of all the uploads sent by the current user.
        /// </summary>
        [HttpGet("uploads"), Authorize]
        public async Task<IActionResult> GetUserUploadsDetailsAsync()
        {
            var userId = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
            var result = await _uploadService.GetUploadsDetailsAsync(userId?.Value);
            if (result is null)
            {
                return BadRequest();
            }
            
            return Json(result);
        }

        /// <summary>
        ///     Gets the details of an upload.
        /// </summary>
        /// <param name="filename">Complete name of the file or its guid.</param>
        /// <param name="password">Password of the file, if any is set.</param>
        [HttpGet("{filename}")]
        public async Task<IActionResult> GetUploadContentAsync(
            string filename, 
            [FromQuery(Name = "password")] string password = null)
        {
            var (content, type) = await _uploadService.GetUploadAsync(filename, password);

            if (content.Length != 0)
            {
                return File(content, type);
            }
            
            if (string.IsNullOrWhiteSpace(type))
            {
                return NotFound();
            }

            return Unauthorized();
        }
    }
}