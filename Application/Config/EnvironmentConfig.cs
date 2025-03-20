namespace Application.Config;

public enum EnvironmentType
{
    Development,
    Staging,
    Production
}

public class EnvironmentConfig(EnvironmentType type)
{
    public EnvironmentType Type { get; } = type;

    public bool IsDevelopment() => Type == EnvironmentType.Development;
    public bool IsStaging() => Type == EnvironmentType.Staging;
    public bool IsProduction() => Type == EnvironmentType.Production;
}