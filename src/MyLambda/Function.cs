using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.Parameters;
using AWS.Lambda.Powertools.Parameters.Provider;
using AWS.Lambda.Powertools.Parameters.Transform;
using System.Diagnostics;
using System.Text.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace MyLambda;

public class Function
{
    public async Task<APIGatewayHttpApiV2ProxyResponse> FunctionHandler(APIGatewayHttpApiV2ProxyRequest input, ILambdaContext context)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var ssmProvider = ParametersManager.SsmProvider;

        var value = await ssmProvider
            .DefaultMaxAge(TimeSpan.FromMinutes(1))
            .GetAsync("basicparameter");

        var values = await ssmProvider
            .Recursive()
            .GetMultipleAsync("/mylambda");

        var secureValue = await ssmProvider
            .WithDecryption()
            .GetAsync("secureparameter");

        var secretsProvider = ParametersManager.SecretsProvider;

        var secret = await secretsProvider
            .DefaultMaxAge(TimeSpan.FromMinutes(1))
            .GetAsync("secret");

        var configuration = await ssmProvider
            .WithTransformation(Transformation.Json)
            .GetAsync<Configuration>("jsonparameter");

        stopwatch.Stop();

        var body = JsonSerializer.Serialize(new Response()
        {
            BasicParameterValue = value,
            MultipleParameterValues = values,
            SecureParameterValue = secureValue,
            Secret = secret,
            Configuration = configuration,
            ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
        });

        return new APIGatewayHttpApiV2ProxyResponse
        {
            Body = body,
            StatusCode = 200,
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
        };
    }

    public class Response
    {
        public string? BasicParameterValue { get; set; }
        public IDictionary<string, string?>? MultipleParameterValues { get; set; }
        public string? SecureParameterValue { get; set; }
        public Configuration? Configuration { get; set; }
        public long ElapsedMilliseconds { get; set; }
        public string? Secret { get; set; }
    }

    public record Configuration(string Parameter1, string Parameter2);

}
