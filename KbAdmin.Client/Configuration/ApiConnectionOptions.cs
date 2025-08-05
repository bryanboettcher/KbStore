namespace KbAdmin.Client.Configuration;

using Microsoft.Extensions.Options;

public class ApiConnectionOptions
{
    public const string Section = nameof(ApiConnectionOptions);
    public Uri? ApiRoot { get; set; }
}

public class ApiConnectionOptionsConfigurator : IConfigureOptions<ApiConnectionOptions>, IValidateOptions<ApiConnectionOptions>
{
    private readonly IConfiguration _config;

    public ApiConnectionOptionsConfigurator(IConfiguration config)
    {
        _config = config;
    }

    public void Configure(ApiConnectionOptions options)
    {
        _config.GetSection(ApiConnectionOptions.Section).Bind(options);
    }

    public ValidateOptionsResult Validate(string? name, ApiConnectionOptions options)
    {
        if (options.ApiRoot is null)
            return ValidateOptionsResult.Fail($"{nameof(options.ApiRoot)} must be set");

        return ValidateOptionsResult.Success;
    }
}