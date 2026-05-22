using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Interfaces;

namespace POS.Application.Features.SendMail
{
    public class VerificationService
    {
        private static readonly ConcurrentDictionary<string, VerificationData> _verificationCodes
            = new ConcurrentDictionary<string, VerificationData>();

        private readonly GmailService _gmailService;
        private readonly IMyAppDbContext _context;

        private const int CODE_EXPIRY_SECONDS = 60;
        private const int RESEND_COOLDOWN_SECONDS = 5;
        private const int MAX_RESEND_ATTEMPTS = 3;

        public VerificationService(
            GmailService gmailService,
            IMyAppDbContext context)
        {
            _gmailService = gmailService;
            _context = context;
        }

        public string GenerateVerificationCode()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        public async Task<SendCodeResult> SendVerificationCodeAsync(string email)
        {
            var personExists = await _context.Persons
                .AnyAsync(p =>
                    p.Email.ToLower() == email.ToLower() &&
                    p.IsActive && p.IsDeleted == false);

            if (!personExists)
            {
                return new SendCodeResult
                {
                    Success = false,
                    Message = "This email is not registered in the system."
                };
            }

            if (_verificationCodes.TryGetValue(email, out var existingData))
            {
                if (DateTime.Now < existingData.ExpiryTime)
                {
                    var timeSinceLastSend = DateTime.Now - existingData.LastSentTime;

                    if (timeSinceLastSend.TotalSeconds < RESEND_COOLDOWN_SECONDS)
                    {
                        var waitSeconds = RESEND_COOLDOWN_SECONDS - (int)timeSinceLastSend.TotalSeconds;
                        return new SendCodeResult
                        {
                            Success = false,
                            Message = $"Please wait {waitSeconds} seconds before requesting a new code",
                            WaitSeconds = waitSeconds
                        };
                    }

                    if (existingData.ResendCount >= MAX_RESEND_ATTEMPTS)
                    {
                        return new SendCodeResult
                        {
                            Success = false,
                            Message = "Maximum resend attempts reached. Please try again later.",
                            MaxAttemptsReached = true
                        };
                    }
                }
                else
                {
                    _verificationCodes.TryRemove(email, out _);
                }
            }

            string code = GenerateVerificationCode();
            DateTime expiryTime = DateTime.Now.AddSeconds(CODE_EXPIRY_SECONDS);

            int resendCount = 0;
            if (_verificationCodes.TryGetValue(email, out var oldData))
            {
                if (DateTime.Now < oldData.ExpiryTime)
                {
                    resendCount = oldData.ResendCount + 1;
                }
            }

            var verificationData = new VerificationData
            {
                Code = code,
                ExpiryTime = expiryTime,
                LastSentTime = DateTime.Now,
                ResendCount = resendCount
            };

            _verificationCodes[email] = verificationData;

            var emailDto = new EmailDto
            {
                To = email,
                Subject = "Your Verification Code - Library System",
                Body = $@"
                Hello,

                Your verification code is: {code}

                This code will expire in {CODE_EXPIRY_SECONDS} seconds.

                If you didn't request this code, please ignore this email.

                Best regards,
                Coffee Management System"
            };

            await _gmailService.SendEmailAsync(emailDto);

            return new SendCodeResult
            {
                Success = true,
                Message = "Verification code sent successfully",
                Code = code,
                ExpirySeconds = CODE_EXPIRY_SECONDS,
                RemainingResends = MAX_RESEND_ATTEMPTS - resendCount
            };
        }

        public VerifyCodeResult VerifyCode(string email, string code)
        {
            if (!_verificationCodes.TryGetValue(email, out var storedData))
            {
                return new VerifyCodeResult
                {
                    Success = false,
                    Message = "No verification code found for this email"
                };
            }

            if (DateTime.Now > storedData.ExpiryTime)
            {
                _verificationCodes.TryRemove(email, out _);
                return new VerifyCodeResult
                {
                    Success = false,
                    Message = "Verification code has expired. Please request a new code.",
                    IsExpired = true
                };
            }

            if (storedData.Code != code)
            {
                return new VerifyCodeResult
                {
                    Success = false,
                    Message = "Invalid verification code"
                };
            }

            _verificationCodes.TryRemove(email, out _);
            return new VerifyCodeResult
            {
                Success = true,
                Message = "Email verified successfully"
            };
        }

        public TimeRemainingResult GetRemainingTime(string email)
        {
            if (!_verificationCodes.TryGetValue(email, out var storedData))
            {
                return new TimeRemainingResult
                {
                    HasCode = false,
                    Message = "No verification code found"
                };
            }

            if (DateTime.Now > storedData.ExpiryTime)
            {
                _verificationCodes.TryRemove(email, out _);
                return new TimeRemainingResult
                {
                    HasCode = false,
                    Message = "Verification code has expired",
                    IsExpired = true
                };
            }

            var remainingTime = storedData.ExpiryTime - DateTime.Now;
            return new TimeRemainingResult
            {
                HasCode = true,
                RemainingSeconds = (int)remainingTime.TotalSeconds,
                RemainingMinutes = (int)remainingTime.TotalMinutes,
                Message = $"Code expires in {(int)remainingTime.TotalMinutes} minutes and {remainingTime.Seconds} seconds"
            };
        }

        public void CleanupExpiredCodes()
        {
            var now = DateTime.Now;
            var expiredKeys = _verificationCodes
                .Where(x => x.Value.ExpiryTime < now)
                .Select(x => x.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _verificationCodes.TryRemove(key, out _);
            }
        }
    }

    public class VerificationData
    {
        public string Code { get; set; } = string.Empty;
        public DateTime ExpiryTime { get; set; }
        public DateTime LastSentTime { get; set; }
        public int ResendCount { get; set; }
    }

    public class SendCodeResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Code { get; set; }
        public int ExpirySeconds { get; set; }
        public int RemainingResends { get; set; }
        public int WaitSeconds { get; set; }
        public bool MaxAttemptsReached { get; set; }
    }

    public class VerifyCodeResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsExpired { get; set; }
    }

    public class TimeRemainingResult
    {
        public bool HasCode { get; set; }
        public int RemainingSeconds { get; set; }
        public int RemainingMinutes { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsExpired { get; set; }
    }
}