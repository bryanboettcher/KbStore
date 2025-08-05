namespace KbAdmin.Client.Services;

using KbStoreApiClient;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;


public class KbStoreClientFactory
{
    private readonly HttpClient _httpClient;
    private readonly IAuthenticationProvider _authenticationProvider;

    public KbStoreClientFactory(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _authenticationProvider = new AnonymousAuthenticationProvider();
    }

    public KbStoreApiClient GetClient()
    {
        return new KbStoreApiClient(
            new HttpClientRequestAdapter(
                _authenticationProvider,
                httpClient: _httpClient
            )
        );
    }
}
