using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Application.Features.SendMail;
using MailKit.Net.Smtp;

namespace EmailApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MailController : ControllerBase
    {
        private readonly GmailService _gmailService;
        private readonly VerificationService _verificationService;

        public MailController(GmailService gmailService, VerificationService verificationService)
        {
            _gmailService = gmailService;
            _verificationService = verificationService;
        }

        [HttpPost("send-verification")]
        [AllowAnonymous]
        public async Task<IActionResult> SendVerificationCode([FromBody] SendVerificationCodeRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.Email))
                    return BadRequest(new { error = "Email is required" });

                if (!IsValidEmail(request.Email))
                    return BadRequest(new { error = "Invalid email format" });

                var result = await _verificationService.SendVerificationCodeAsync(request.Email);

                if (!result.Success)
                {
                    if (result.MaxAttemptsReached)
                        return StatusCode(429, new { error = result.Message, maxAttemptsReached = true });

                    return BadRequest(new { error = result.Message, waitSeconds = result.WaitSeconds });
                }

                return Ok(new
                {
                    message = result.Message,
                    email = request.Email,
                    expirySeconds = result.ExpirySeconds,
                    remainingResends = result.RemainingResends,
                    codeForTesting = result.Code
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to send verification code: {ex.Message}" });
            }
        }

        [HttpPost("resend-verification")]
        [AllowAnonymous]
        public async Task<IActionResult> ResendVerificationCode([FromBody] SendVerificationCodeRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.Email))
                    return BadRequest(new { error = "Email is required" });

                var result = await _verificationService.SendVerificationCodeAsync(request.Email);

                if (!result.Success)
                {
                    if (result.MaxAttemptsReached)
                        return StatusCode(429, new { error = result.Message, maxAttemptsReached = true });

                    return BadRequest(new { error = result.Message, waitSeconds = result.WaitSeconds });
                }

                return Ok(new
                {
                    message = "Verification code resent successfully",
                    email = request.Email,
                    expirySeconds = result.ExpirySeconds,
                    remainingResends = result.RemainingResends,
                    codeForTesting = result.Code
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to resend verification code: {ex.Message}" });
            }
        }

        [HttpPost("verify-code")]
        [AllowAnonymous]
        public IActionResult VerifyCode([FromBody] VerifyCodeRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.Email) || string.IsNullOrWhiteSpace(request?.Code))
                    return BadRequest(new { error = "Email and code are required" });

                var result = _verificationService.VerifyCode(request.Email, request.Code);

                if (!result.Success)
                {
                    if (result.IsExpired)
                        return BadRequest(new { error = result.Message, expired = true, canResend = true });

                    return BadRequest(new { error = result.Message, verified = false });
                }

                return Ok(new { message = result.Message, verified = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Verification failed: {ex.Message}" });
            }
        }

        [HttpGet("check-time/{email}")]
        [AllowAnonymous]
        public IActionResult CheckRemainingTime(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return BadRequest(new { error = "Email is required" });

                var result = _verificationService.GetRemainingTime(email);

                if (!result.HasCode)
                    return NotFound(new { message = result.Message, expired = result.IsExpired });

                return Ok(new
                {
                    message = result.Message,
                    remainingSeconds = result.RemainingSeconds,
                    remainingMinutes = result.RemainingMinutes
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to check time: {ex.Message}" });
            }
        }

        [HttpPost("send")]
        [AllowAnonymous]
        public async Task<IActionResult> SendEmail([FromBody] EmailDto email)
        {
            try
            {
                await _gmailService.SendEmailAsync(email);
                return Ok(new { message = "Email sent successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to send email: {ex.Message}" });
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}