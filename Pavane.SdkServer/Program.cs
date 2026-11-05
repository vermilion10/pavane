using Pavane.Common;

// Offline helpers for the master-data password; these run and exit without starting the server
//   masterdata-pwd encode <password>          -> the encrypted_password value to serve
//   masterdata-pwd decode <encrypted_password> -> the plaintext password from a captured field
//   assets decode <publicAssetsHex>            -> decodes a captured /public/assets blob and, if present, recovers its master-data password
switch (args)
{
    case ["masterdata-pwd", "encode", var password]:
        Console.WriteLine(MasterDataPwd.Encode(password));
        return;
    case ["masterdata-pwd", "decode", var value]:
        Console.WriteLine(MasterDataPwd.Decode(value));
        return;
    case ["assets", "decode", var hex]:
    {
        var json = System.Text.Encoding.UTF8.GetString(Packer.Unpack(Convert.FromHexString(hex.Trim())));
        Console.WriteLine(json);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("encrypted_password", out var ep) && ep.GetString() is { Length: > 0 } enc)
            Console.WriteLine($"master-data password: {MasterDataPwd.Decode(enc)}");
        else
            Console.WriteLine("(no encrypted_password present in this capture)");
        return;
    }
    case ["masterdata-pwd", ..] or ["assets", ..]:
        Console.Error.WriteLine("usage: masterdata-pwd <encode|decode> <value> | assets decode <hex>");
        return;
}

var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { Args = args, ContentRootPath = AppContext.BaseDirectory });
builder.Logging.InitTracing(LogLevel.Debug);
builder.WebHost.ConfigureKestrel(k =>
{
    k.ListenLocalhost(5000); // plain HTTP
    var pfx = Environment.GetEnvironmentVariable("PAVANE_HTTPS_PFX"); // optional TLS for on-device testing
    if (!string.IsNullOrEmpty(pfx) && File.Exists(pfx))
        k.ListenAnyIP(8443, o => o.UseHttps(System.Security.Cryptography.X509Certificates.X509CertificateLoader.LoadPkcs12FromFile(pfx, null)));
});

var app = builder.Build();
var log = app.Logger;

// The CDN redirected the bare list.html to the region's agreement files; mirror that
const string Agreements = "20230901112934【Score】【英语】【北美等地区】【隐私协议】,20230901112217【Score】【英语】【欧盟北美等地区】【用户协议】";
app.Use(async (ctx, next) =>
{
    var req = ctx.Request;
    if (req.Path == "/65002002-prod/list.html" && !req.Query.ContainsKey("filename"))
    {
        var query = req.QueryString.Add("folder", "ALL").Add("region", "ALL").Add("filename", Agreements);
        ctx.Response.Redirect(req.Path + query.ToUriComponent());
        return;
    }
    await next();
});

// agreement pages from lcmx-pp-cdn.mobage.world, served from wwwroot
app.UseStaticFiles();
app.UseRouting();

const string HttpServerUrl = "http://127.0.0.1:5000";
const string GameServerAddress = "127.0.0.1:5001"; // the client's channel needs a bare host:port

// field names are the keys OutgameService.GetServerList reads from each realm
var realms = System.Text.Json.JsonSerializer.Serialize(new
{
    realms = new[]
    {
        new
        {
            initial_realm_id = "1",
            name = "Pavane",
            game_server = GameServerAddress,
            http_server = HttpServerUrl,
            status = 1,
            extra_info2 = "",
        },
    },
});

IResult Logstores(HttpContext ctx)
{
    log.LogDebug("Received POST request to {Path}", ctx.Request.Path);
    return Results.Ok();
}

app.MapPost("/logstores/prod_lcx_sdk/shards/lb", Logstores);
app.MapPost("/logstores/prod_lcx_request_data/shards/lb", Logstores);
app.MapPost("/logstores/prod_score_data/shards/lb", Logstores);

app.MapGet("/environment", (HttpContext ctx) =>
{
    log.LogDebug("Received GET request to {Path}", ctx.Request.Path);
    return Results.Json(new
    {
        applicationId = "65002002",
        isProd = true,
        cs = new
        {
            url = "https://tickets-prod-ap-live.mobage.world/1",
            accountCancellationUrl = "https://tickets-prod-ap-live.mobage.world/1",
        },
    });
});

app.MapGet("/module/init", (HttpContext ctx) =>
{
    log.LogDebug("Received GET request to {Path}", ctx.Request.Path);
    return Results.Json(new
    {
        auth = new[] { new { type = "DStore", @params = new { } } },
        ad = new object[]
        {
            new { type = "Facebook", @params = new { appid = "593332068848186" } },
            new { type = "AppsFlyer", @params = new { appleAppId = "", appKey = "yQdMGTLck4jiDyS9aSybXQ" } },
        },
        share = new[] { "Facebook", "Instagram", "Line", "System", "Twitter" }
            .Select(t => new { type = t, @params = new { } }),
        agreements = new
        {
            language = "en",
            disable = false,
            mainUrl = "https://lcmx-pp-cdn.mobage.world/65002002-prod/list.html",
            title = (string?)null,
            content = (string?)null,
            version = "20230403153000",
            isNative = false,
            agreement = new[]
            {
                new { title = "Privacy Policy", url = "...", type = "0" },
                new { title = "End User License Agreement", url = "...", type = "1" },
            },
        },
        cs = new
        {
            csUrl = "https://tickets-prod-ap-live.mobage.world/1",
            accountCancellationUrl = "https://tickets-prod-ap-live.mobage.world/1",
        },
        push = "FCM",
        walletEnabled = true,
        customizedUI = false,
        disableRealNameVerify = false,
    });
});

app.MapGet("/limbo/anti-fraud/init", (HttpContext ctx) =>
{
    log.LogDebug("Received GET request to {Path}", ctx.Request.Path);
    return Results.Json(new { code = 10007, message = "not support", data = (object?)null });
});

app.MapPost("/api/v6.10/androidevent", () =>
{
    log.LogDebug("Received POST request");
    return Results.Text("ok", "text/plain");
});

app.MapGet("/install_data/v5.0/com.dgames.g65002002.google", (HttpContext ctx) =>
{
    log.LogDebug("POST to {Path}", ctx.Request.Path);
    return Results.Json(new
    {
        af_message = "organic install",
        af_status = "Organic",
        install_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
    });
});

app.MapGet("/public/realms", (HttpContext ctx) =>
{
    log.LogDebug("Received GET request to {Path}", ctx.Request.Path);
    return Results.Text(Packer.PackToHex(realms), "text/plain");
});

// versions match the client's local `mdv` and `av` files, so it does not treat its install as stale.
var assetLocations = new
{
    domain = HttpServerUrl,
    master_data_url = "/assets/masterdata/",
    assets_bundle_url = "/assets/bundles/",
};
// set PAVANE_MASTERDATA_PWD to the plaintext ZIP password to serve a real encrypted_password; left unset it stays empty (the client then stalls opening the master-data archive).
var masterDataPwd = Environment.GetEnvironmentVariable("PAVANE_MASTERDATA_PWD");
var encryptedPassword = string.IsNullOrEmpty(masterDataPwd) ? "" : MasterDataPwd.Encode(masterDataPwd);
var assets = System.Text.Json.JsonSerializer.Serialize(new
{
    encrypted_password = encryptedPassword,
    assets_info = new
    {
        master_data_version = "ed4d5e1d-c659-4939-8afd-401c459ee3a1",
        assets_bundle_version = "e681396a-4e90-4671-9ad3-c7827eecb0b7",
        assetLocations.domain,
        assetLocations.master_data_url,
        assetLocations.assets_bundle_url,
    },
    backup_assets_info = assetLocations,
});

// pop_type 3 is OutgameAnnouncementPopType.No_Pop.
var announcements = System.Text.Json.JsonSerializer.Serialize(new { pop_type = 3, announcements = Array.Empty<object>() });

app.MapGet("/public/announcements", (HttpContext ctx) =>
{
    log.LogDebug("Received GET request to {Path}", ctx.Request.Path);
    return Results.Text(Packer.PackToHex(announcements), "text/plain");
});

app.MapGet("/public/assets", (HttpContext ctx) =>
{
    log.LogDebug("Received GET request to {Path}{Query}", ctx.Request.Path, ctx.Request.QueryString);
    return Results.Text(Packer.PackToHex(assets), "text/plain");
});

// D-Store account service (lcmx-dstore.mobage.world). Only guest login is offered.
app.MapGet("/tw/initV2", (HttpContext ctx) =>
{
    log.LogDebug("Received GET request to {Path}{Query}", ctx.Request.Path, ctx.Request.QueryString);
    return Results.Json(new { loginList = new[] { new { loginName = "oneClick", priority = "1" } } });
});

app.MapPost("/tw/account/login", async (HttpContext ctx) =>
{
    log.LogDebug("D-Store login: {Body}", await ReadBody(ctx));
    return Results.Json(new
    {
        accessToken = "pavane-access-token",
        userInfo = new { openId = "10000001", mid = "1" }, // the Java SDK parses openId as an integer
        ssoToken = "pavane-sso-token",
        realNameType = 0,
        hasRealName = 1,
        realNameCanSkip = 1,
        newUserFlag = false,
    });
});

// LCX identity service. Every device signs in as the same local guest user.
const string GuestUserId = "10000001";
var guestIdToken = string.Join('.', new[]
{
    """{"alg":"none","typ":"JWT"}""",
    $$"""{"sub":"{{GuestUserId}}"}""",
}.Select(part => System.Buffers.Text.Base64Url.EncodeToString(System.Text.Encoding.UTF8.GetBytes(part)))) + ".";

app.MapPost("/identity/pre-sign-in", async (HttpContext ctx) =>
{
    log.LogDebug("Pre sign-in: {Body}", await ReadBody(ctx));
    return Results.Json(new { activationCodeAnnouncement = (object?)null });
});

app.MapPost("/identity/sign-in", async (HttpContext ctx) =>
{
    log.LogDebug("Sign-in: {Body}", await ReadBody(ctx));
    return Results.Json(new
    {
        accessToken = "pavane-access-token",
        lcxUserIdToken = guestIdToken,
        signInSessionToken = "pavane-session-token",
        signInSessionId = "pavane-session-id",
        analyticsSessionId = "pavane-analytics-session",
        user = new { id = GuestUserId, registeredAt = 1700000000000L },
    });
});

app.MapGet("/identity/user-id-token/{userId}", (string userId) =>
{
    log.LogDebug("User id token requested for {UserId}", userId);
    return Results.Json(new { lcxUserIdToken = guestIdToken });
});

app.MapPost("/analytics/event", () => Results.Ok());

app.MapPost("/auth/user-access-token", () => Results.Json(new
{
    userId = GuestUserId,
    accessToken = "pavane-access-token",
    signInSessionId = "pavane-session-id",
}));

// purchases are not available locally, so the store and wallet are empty
app.MapGet("/iap/product", () => Results.Json(new { products = Array.Empty<object>() }));
app.MapGet("/currency-exchange/wallet/user-balance", () => Results.Json(new { balances = Array.Empty<object>() }));
app.MapGet("/announcement/post", () => Results.Json(new { title = "", content = "", canSkip = true }));

static async Task<string> ReadBody(HttpContext ctx)
{
    using var reader = new StreamReader(ctx.Request.Body);
    var body = await reader.ReadToEndAsync();
    return body.Length > 2000 ? body[..2000] + "..." : body;
}

app.MapFallback(async (HttpContext ctx) =>
{
    log.LogDebug("no route matched: {Method} {Host} {Uri} body={Body}", ctx.Request.Method, ctx.Request.Host, ctx.Request.Path + ctx.Request.QueryString, await ReadBody(ctx));
    return Results.Text("no route", statusCode: StatusCodes.Status404NotFound);
});

app.Lifetime.ApplicationStarted.Register(() => log.LogDebug("SDK listening on {Urls}", string.Join(", ", app.Urls)));
app.Run();
