using Takasho.Schema.Score.PlayerApi;
using PlayerV1 = Takasho.Schema.Score.ResourceCn.Player.V1;
using SystemV1 = Takasho.Schema.Score.ResourceCn.System.V1;

namespace Pavane.GameServer;

// builds the login responses the client fetches right after AuthorizeCnV1
// LoginV1 carries the whole player snapshot through its `common` (CommonResponse) field; we return a minimal, self-consistent starter profile so the client can leave the title and try to build the lobby.
// ShutdownV1 just tells the client the service is open (no charge/refund windows), which it polls on boot.
public static class LoginHandlers
{
    public static LoginV1.Types.Response Login(string playerId, LoginV1.Types.Request _)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var player = new PlayerV1.Player
        {
            PlayerId = playerId,
            PlayerShortId = playerId,
            Name = "Conductor",
            Exp = 0,
            Stamina = 120,
            StaminaUpdatedTime = now,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var common = new SystemV1.CommonResponse
        {
            Player = player,
            Wallet = new PlayerV1.PlayerWalletResponse(),
            TutorialInfo = new PlayerV1.PlayerTutorialInfo { PlayerId = playerId },
            PlayerPreference = new Takasho.Schema.CommonFeatureset.Resource.PlayerPreference.V1.PlayerPreference(),
        };

        return new LoginV1.Types.Response
        {
            IsNewPlayer = false,
            Common = common,
            System = BuildSystemInfo(now),
            H5AnnouncementUrl = "",
        };
    }

    public static ShutdownV1.Types.Response Shutdown(string _, ShutdownV1.Types.Request __)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return new ShutdownV1.Types.Response
        {
            ShutdownEnable = false,
            ChargeShutdownTime = 0,
            RefundStartTime = 0,
            ServerShutdownTime = 0,
            System = BuildSystemInfo(now),
        };
    }

    // no maintenance window, server clock = now, timezone UTC.
    private static SystemV1.SystemInfo BuildSystemInfo(long now) => new()
    {
        Maintenance = null,
        ServerTime = now,
        InGameAnnouncementUrl = "",
        ServerTimezone = 0,
    };
}
