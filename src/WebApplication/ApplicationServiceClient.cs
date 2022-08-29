using Flurl;
using MSA.BuildingBlocks.ServiceClient;


namespace WebApplication1;

public class TokenResponse
{
    public string AccessToken { get; set; }
    public int ExpiresIn { get; set; }
    public long ExpiresAt { get; set; }
}


public interface IApplicationServiceClient
{
    Task<ServiceResponse<TokenResponse>> IssueToken(string clientId, string clientSecret);
}
public class ApplicationServiceClient : ServiceClientBase, IApplicationServiceClient
{
    public ApplicationServiceClient(HttpClient httpClient, ILogger<ApplicationServiceClient> logger)
        : base(httpClient, logger)
    {
    }


    public async Task<ServiceResponse<TokenResponse>> IssueToken(string clientId, string clientSecret)
    {
        var requestMessage = ServiceUri.AbsoluteUri
            .AppendPathSegments("identity", "token")
            .WithHttpMethod(HttpMethod.Post)
            .WithJsonContent(new
            {
                
                clientSecret
            });

        return await Send<TokenResponse>(requestMessage).ConfigureAwait(false);
    }
}