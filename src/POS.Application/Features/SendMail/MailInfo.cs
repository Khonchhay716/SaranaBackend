namespace POS.Application.Features.SendMail
{
    public class EmailDto
    {
        public string To { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
    }

    // New DTO for verification request
    public class SendVerificationCodeRequest
    {
        public string Email { get; set; }
    }

    // New DTO for verification
    public class VerifyCodeRequest
    {
        public string Email { get; set; }
        public string Code { get; set; }
    }
}