namespace Xcel.Services.Auth.Interfaces.Services.RefreshTokens.Facade;

internal interface IRefreshTokenService :
    IGenerateRefreshTokenCommand,
    IValidateRefreshTokenCommand,
    IRevokeRefreshTokenCommand;