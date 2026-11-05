using Microsoft.AspNetCore.Server.Kestrel.Core;
using Pavane.Common;
using Pavane.GameServer;
using Takasho.Schema.CommonFeatureset.PlayerApi;
using Takasho.Schema.Score.PlayerApi;

const string SystemService = "takasho.schema.common_featureset.player_api.System";
const string LoginService = "takasho.schema.score.player_api.Login";
const string GuestPlayerId = "10000001";

var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { Args = args });
builder.Logging.InitTracing(LogLevel.Debug);
builder.Services.AddSingleton<RpcRouter>();
// the client's game channel defaults to a secure (TLS) gRPC connection
// set PAVANE_GAME_PFX to a CA-signed cert that covers 127.0.0.1 to serve HTTP/2 over TLS (no on-device frida needed to force cleartext), without it, fall back to cleartext h2c for the frida path
var gamePfx = Environment.GetEnvironmentVariable("PAVANE_GAME_PFX");
builder.WebHost.ConfigureKestrel(k => k.ListenLocalhost(5001, o =>
{
    o.Protocols = HttpProtocols.Http2;
    if (!string.IsNullOrEmpty(gamePfx) && File.Exists(gamePfx))
        o.UseHttps(System.Security.Cryptography.X509Certificates.X509CertificateLoader.LoadPkcs12FromFile(gamePfx, null));
}));

var app = builder.Build();

var router = app.Services.GetRequiredService<RpcRouter>();
router.Map<SystemAuthorizeCnV1.Types.Request, SystemAuthorizeCnV1.Types.Response>(SystemService, "AuthorizeCnV1", request =>
{
    app.Logger.LogDebug("Authorize realm {Realm}", request.InitialRealmId);
    return new SystemAuthorizeCnV1.Types.Response { PlayerId = GuestPlayerId, SessionToken = "pavane-game-session" };
});

router.Map<LoginV1.Types.Request, LoginV1.Types.Response>(LoginService, "LoginV1", request =>
{
    app.Logger.LogDebug("LoginV1 for player {Player}", GuestPlayerId);
    return LoginHandlers.Login(GuestPlayerId, request);
});

router.Map<ShutdownV1.Types.Request, ShutdownV1.Types.Response>(LoginService, "ShutdownV1", request =>
{
    app.Logger.LogDebug("ShutdownV1");
    return LoginHandlers.Shutdown(GuestPlayerId, request);
});

app.Run(router.Handle);
app.Run();
