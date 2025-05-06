namespace Presentation.API.Extensions;

internal static class ConfigurationExtensions
{
    internal static TOptions GetOptions<TOptions>(this IConfiguration configuration)
        where TOptions : class
    {
        var optionType = typeof(TOptions).Name.Replace("Options", "");

        var parsedOptions =
            configuration.GetRequiredSection(optionType).Get<TOptions>()
            ?? throw new InvalidOperationException(
                $"It's mandatory to have the {optionType} configuration"
            );

        return parsedOptions;
    }
}
