using Google.Authenticator;
using Microsoft.AspNetCore.Mvc;

namespace TheSKZWeb.Services
{
    public interface IMFAService
    {
        string GenerateSecret(string value);
        bool ValidatePin(string secret, string pin);
        bool ValidateMFARequest(string? userHashId, string? pin);
        OkObjectResult GenerateMFAResponse(bool validated);
    }

    public class MFAService : IMFAService
    {
        private readonly string _secret = "";

        public MFAService(IConfiguration configuration)
        {
            _secret = configuration.GetSection("2FA:Key").Value;

        }

        public OkObjectResult GenerateMFAResponse(bool validated)
        {
            return new OkObjectResult(
                new
                {
                    Message = "Validate with 2 factor!",
                    Tags = new string[]
                    {
                        "doMFA"
                    }
                }
            );
        }

        public string GenerateSecret(string value)
        {
            return $"{_secret}-{value}";
        }

        public bool ValidateMFARequest(string? userHashId, string? pin)
        {
            if (userHashId == null || pin == null)
            {
                return false;
            }

            return ValidatePin(
                GenerateSecret(userHashId),
                pin
            );
        }

        public bool ValidatePin(string secret, string pin)
        {
            TwoFactorAuthenticator twoFactorAuthenticator = new TwoFactorAuthenticator();
            return twoFactorAuthenticator.ValidateTwoFactorPIN(secret, pin);
        }
    }


}
