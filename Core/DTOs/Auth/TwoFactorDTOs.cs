namespace Core.DTOs.Auth
{
    public class EnableTwoFactorResponse
    {
        public string SharedKey { get; set; } = string.Empty;
        public string AuthenticatorUri { get; set; } = string.Empty;
    }

    public class VerifyTwoFactorRequest
    {
        public string Code { get; set; } = string.Empty;
    }

    public class TwoFactorLoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}
