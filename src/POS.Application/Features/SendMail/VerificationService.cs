using System.Collections.Concurrent;

namespace POS.Application.Features.SendMail
{
    public class VerificationService
    {
        // Store codes: email -> (code, expiry time, last sent time, resend count)
        private static readonly ConcurrentDictionary<string, VerificationData> _verificationCodes 
            = new ConcurrentDictionary<string, VerificationData>();

        private readonly GmailService _gmailService;

        // Configuration - FOR TESTING (change these for production)
        private const int CODE_EXPIRY_SECONDS = 60;  // ⏱️ 10 seconds for testing (change to 300 for 5 minutes)
        private const int RESEND_COOLDOWN_SECONDS = 120;  // ⏱️ 5 seconds cooldown for testing (change to 60 for production)
        private const int MAX_RESEND_ATTEMPTS = 3;  // Maximum 3 resends

        public VerificationService(GmailService gmailService)
        {
            _gmailService = gmailService;
        }

        // Generate random 6-digit code
        public string GenerateVerificationCode()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        // Send verification code to email
        public async Task<SendCodeResult> SendVerificationCodeAsync(string email)
        {
            // Check if there's an existing code
            if (_verificationCodes.TryGetValue(email, out var existingData))
            {
                // Check if still valid (not expired)
                if (DateTime.Now < existingData.ExpiryTime)
                {
                    // Check cooldown period (prevent spam)
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

                    // Check max resend attempts
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
                    // ✅ FIX: Code expired, remove it so we can start fresh
                    _verificationCodes.TryRemove(email, out _);
                }
            }

            // Generate new code
            string code = GenerateVerificationCode();

            // Calculate expiry time
            DateTime expiryTime = DateTime.Now.AddSeconds(CODE_EXPIRY_SECONDS);

            // ✅ FIX: Calculate resend count properly
            int resendCount = 0;
            if (_verificationCodes.TryGetValue(email, out var oldData))
            {
                // Only increment if the old code hasn't expired
                if (DateTime.Now < oldData.ExpiryTime)
                {
                    resendCount = oldData.ResendCount + 1;
                }
                // If expired, resendCount stays 0 (fresh start)
            }

            var verificationData = new VerificationData
            {
                Code = code,
                ExpiryTime = expiryTime,
                LastSentTime = DateTime.Now,
                ResendCount = resendCount
            };

            _verificationCodes[email] = verificationData;

            // Create email content
            var emailDto = new EmailDto
            {
                To = email,
                Subject = "Your Verification Code - Coffee Shop",
                Body = $@"Hello,

                Your verification code is: {code}

                This code will expire in {CODE_EXPIRY_SECONDS} seconds.

                If you didn't request this code, please ignore this email.

                Best regards,
                Coffee Management System"
            };

            // Send email
            await _gmailService.SendEmailAsync(emailDto);

            return new SendCodeResult
            {
                Success = true,
                Message = "Verification code sent successfully",
                Code = code, // For testing only - remove in production
                ExpirySeconds = CODE_EXPIRY_SECONDS,
                RemainingResends = MAX_RESEND_ATTEMPTS - resendCount
            };
        }

        // Verify the code
        public VerifyCodeResult VerifyCode(string email, string code)
        {
            // Check if code exists
            if (!_verificationCodes.TryGetValue(email, out var storedData))
            {
                return new VerifyCodeResult
                {
                    Success = false,
                    Message = "No verification code found for this email"
                };
            }

            // Check if expired
            if (DateTime.Now > storedData.ExpiryTime)
            {
                _verificationCodes.TryRemove(email, out _); // Remove expired code
                return new VerifyCodeResult
                {
                    Success = false,
                    Message = "Verification code has expired. Please request a new code.",
                    IsExpired = true
                };
            }

            // Check if code matches
            if (storedData.Code != code)
            {
                return new VerifyCodeResult
                {
                    Success = false,
                    Message = "Invalid verification code"
                };
            }

            // Success! Remove code (one-time use)
            _verificationCodes.TryRemove(email, out _);
            return new VerifyCodeResult
            {
                Success = true,
                Message = "Email verified successfully"
            };
        }

        // Get remaining time for verification
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

        // Clean up expired codes (call this periodically)
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

    // Data model for storing verification info
    public class VerificationData
    {
        public string Code { get; set; } = string.Empty;
        public DateTime ExpiryTime { get; set; }
        public DateTime LastSentTime { get; set; }
        public int ResendCount { get; set; }
    }

    // Result models
    public class SendCodeResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Code { get; set; }  // Only for testing
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