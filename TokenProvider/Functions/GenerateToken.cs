using Azure.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TokenProvider.Infrastructure.Models;
using TokenProvider.Infrastructure.Services;

namespace TokenProvider.Functions
{
    public class GenerateToken
    {
        private readonly ILogger<GenerateToken> _logger;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly ITokenGeneratorService _tokenGeneratorService;

        public GenerateToken(ILogger<GenerateToken> logger, IRefreshTokenService refreshTokenService, ITokenGeneratorService tokenGeneratorService)
        {
            _logger = logger;
            _refreshTokenService = refreshTokenService;
            _tokenGeneratorService = tokenGeneratorService;
        }

        [Function("GenerateToken")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "token/generate")] HttpRequest req)
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var tokenRequest = JsonConvert.DeserializeObject<TokenRequest>(body);

            if (tokenRequest == null || tokenRequest.UserId == null || tokenRequest.Email == null)
            {
                return new BadRequestObjectResult(new { Error = "Please provide a valid userId and email address" });
            }

            try
            {
                RefreshTokenResult refreshTokenResult = null!;
                AccessTokenResult accessTokenResult = null!;
                using var ctsTimeOut = new CancellationTokenSource(TimeSpan.FromSeconds(120 * 1000));
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ctsTimeOut.Token, req.HttpContext.RequestAborted);

                req.HttpContext.Request.Cookies.TryGetValue("refreshToken", out var refreshToken);
                if (!string.IsNullOrEmpty(refreshToken))
                    refreshTokenResult = await _refreshTokenService.GetRefreshTokenAsync(refreshToken, cts.Token);
                if (refreshTokenResult == null || refreshTokenResult.ExpiryDate < DateTime.Now.AddDays(1))
                    refreshTokenResult = await _tokenGeneratorService.GenerateRefreshTokenAsync(tokenRequest.UserId, cts.Token);

                accessTokenResult = _tokenGeneratorService.GenerateAccessToken(tokenRequest, refreshTokenResult.Token);

                if(refreshTokenResult.cookieOptions != null && refreshTokenResult.Token != null)
                    req.HttpContext.Response.Cookies.Append("refreshToken", refreshTokenResult.Token, refreshTokenResult.cookieOptions);

                if (accessTokenResult != null && accessTokenResult.Token != null && refreshTokenResult.Token != null)
                    return new OkObjectResult(new { AccessToken = accessTokenResult.Token, RefreshToken = refreshTokenResult.Token});

            }
            catch { }


            return new ObjectResult(new { Error = "Unexptected error while generating token" }) { StatusCode = 500 };
        }
    }
}
