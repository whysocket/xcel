using Xcel.Config;
using Xcel.Config.Options;

namespace Presentation.API.Options;

public class CorsOptions : IOptionsValidator
{
    public required string FrontendUrl { get; set; }

    public void Validate(EnvironmentOptions environmentOptions)
    {
        if (string.IsNullOrEmpty(FrontendUrl))
        {
            throw new ArgumentException("[CorsOptions] FrontendUrl is empty");
        }

        if (!environmentOptions.IsDevelopment())
        {
            if (!Uri.TryCreate(FrontendUrl, UriKind.Absolute, out _))
            {
                throw new ArgumentException("[CorsOptions] FrontendUrl is not a valid URL");
            }
        }
    }
}
