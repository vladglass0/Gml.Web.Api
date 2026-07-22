using System.Collections.Frozen;
using System.Net;
using AutoMapper;
using Gml.Dto.Messages;
using Gml.Dto.Player;
using Gml.Models.User;
using Gml.Web.Api.Core.Hubs.Controllers;
using Gml.Web.Api.Core.Services;
using GmlCore.Interfaces;
using GmlCore.Interfaces.Auth;
using GmlCore.Interfaces.User;

namespace Gml.Web.Api.Core.Handlers;

public class PlayersHandler : IPlayersHandler
{
    public static async Task<IResult> GetPlayers(
        IGmlManager gmlManager,
        IMapper mapper,
        PlayersController playersController,
        int? take,
        int? offset,
        string? findName,
        string? findUuid,
        string? findIp,
        string? findHwid,
        bool? onlyBlocked,
        bool? onlyDeviceBlocked,
        PlayersSortBy? sortBy,
        bool? sortDesc)
    {
        var hasAdvancedFilters =
            !string.IsNullOrWhiteSpace(findUuid) ||
            !string.IsNullOrWhiteSpace(findIp) ||
            !string.IsNullOrWhiteSpace(findHwid) ||
            (onlyBlocked.HasValue && onlyBlocked.Value) ||
            (onlyDeviceBlocked.HasValue && onlyDeviceBlocked.Value) ||
            sortBy.HasValue;

        IEnumerable<IUser> players;

        if (hasAdvancedFilters)
        {
            players = await gmlManager.Users.GetUsers();

            if (!string.IsNullOrWhiteSpace(findName))
            {
                var namePart = findName.Trim();
                players = players.Where(u =>
                    (!string.IsNullOrEmpty(u.Name) && u.Name.Contains(namePart, StringComparison.OrdinalIgnoreCase)));
            }

            if (!string.IsNullOrWhiteSpace(findUuid))
            {
                var uuidPart = findUuid.Trim();
                players = players.Where(u =>
                    !string.IsNullOrEmpty(u.Uuid) && u.Uuid!.Contains(uuidPart, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(findIp))
            {
                var ipPart = findIp.Trim();
                players = players.Where(u => (u is AuthUser au) && au.AuthHistory.Any(h =>
                    !string.IsNullOrEmpty(h.Address) &&
                    h.Address!.Contains(ipPart, StringComparison.OrdinalIgnoreCase)));
            }

            if (!string.IsNullOrWhiteSpace(findHwid))
            {
                var hwidPart = findHwid.Trim();
                players = players.Where(u => (u is AuthUser au) && au.AuthHistory.Any(h =>
                    !string.IsNullOrEmpty(h.Hwid) && h.Hwid!.Contains(hwidPart, StringComparison.OrdinalIgnoreCase)));
            }

            if (onlyDeviceBlocked == true)
            {
                players = players.Where(u => u.IsBannedPermanent);
            }
            else if (onlyBlocked == true)
            {
                players = players.Where(u => u.IsBanned);
            }

            var desc = sortDesc ?? true;
            if (sortBy.HasValue)
            {
                switch (sortBy.Value)
                {
                    case PlayersSortBy.AuthCount:
                        players = desc
                            ? players.OrderByDescending(u => (u is AuthUser au) ? au.AuthHistory.Count : 0)
                            : players.OrderBy(u => (u is AuthUser au) ? au.AuthHistory.Count : 0);
                        break;
                    case PlayersSortBy.SessionExpiry:
                        players = desc
                            ? players.OrderByDescending(u => u.ServerExpiredDate)
                            : players.OrderBy(u => u.ServerExpiredDate);
                        break;
                    case PlayersSortBy.Name:
                    default:
                        players = desc
                            ? players.OrderByDescending(u => u.Name)
                            : players.OrderBy(u => u.Name);
                        break;
                }
            }

            var skip = Math.Max(0, offset ?? 0);
            var takeCount = Math.Max(1, take ?? 20);
            players = players.Skip(skip).Take(takeCount);
        }
        else
        {
            players = await gmlManager.Users.GetUsers(take ?? 20, offset ?? 0, findName ?? string.Empty);
        }

        var result = mapper.Map<List<ExtendedPlayerReadDto>>(players);
        foreach (var dto in result)
        {
            dto.IsLauncherStarted = playersController.GetLauncherConnection(dto.Name, out _);
        }

        return Results.Ok(ResponseMessage.Create(result, "Список пользователей успешно получен", HttpStatusCode.OK));
    }

    public static async Task<IResult> BanPlayer(
        IGmlManager gmlManager,
        IMapper mapper,
        IList<string> playerUuids,
        bool deviceBlock = false)
    {
        if (!playerUuids.Any())
        {
            return Results.BadRequest(ResponseMessage.Create("Не передан ни один пользователь для блокировки",
                HttpStatusCode.BadRequest));
        }

        foreach (var playerUuid in playerUuids)
        {
            var player = await gmlManager.Users.GetUserByUuid(playerUuid);

            if (player is null)
                continue;

            await player.Block(deviceBlock);
        }

        return Results.Ok(ResponseMessage.Create("Пользователь(и) успешно заблокированы", HttpStatusCode.OK));
    }

    public static async Task<IResult> RemovePlayer(
        IGmlManager gmlManager,
        IMapper mapper,
        IList<string> playerUuids)
    {
        if (!playerUuids.Any())
        {
            return Results.BadRequest(ResponseMessage.Create("Не передан ни один пользователь для блокировки",
                HttpStatusCode.BadRequest));
        }

        var profiles = (await gmlManager.Profiles.GetProfiles()).ToFrozenSet();


        foreach (var playerUuid in playerUuids)
        {
            var player = await gmlManager.Users.GetUserByUuid(playerUuid);

            if (player is null) continue;

            if (profiles.Any(c => c.UserWhiteListGuid.Contains(playerUuid)))
            {
                return Results.BadRequest(ResponseMessage.Create(
                    $"Пользователь \"{player.Name}\" находится в белом списке, удалите его из всех профилей перед удалением!",
                    HttpStatusCode.BadRequest));
            }

            await gmlManager.Users.RemoveUser(player);
        }

        return Results.Ok(ResponseMessage.Create("Пользователь(и) успешно заблокированы", HttpStatusCode.OK));
    }

    public static async Task<IResult> PardonPlayer(
        IGmlManager gmlManager,
        IMapper mapper,
        IList<string> playerUuids,
        bool deviceUnblock = false)
    {
        if (!playerUuids.Any())
        {
            return Results.BadRequest(ResponseMessage.Create("Не передан ни один пользователь для блокировки",
                HttpStatusCode.BadRequest));
        }

        foreach (var playerUuid in playerUuids)
        {
            var player = await gmlManager.Users.GetUserByUuid(playerUuid);

            if (player is null) continue;

            await player.Unblock(deviceUnblock);
        }

        return Results.Ok(ResponseMessage.Create("Пользователь(и) успешно разблокированы", HttpStatusCode.OK));
    }

    public static async Task<IResult> GetUnicoreCabinet(
        string userUuid,
        UnicorePlayerCabinetService cabinetService)
    {
        if (string.IsNullOrWhiteSpace(userUuid))
        {
            return Results.BadRequest(ResponseMessage.Create("Не передан UUID игрока", HttpStatusCode.BadRequest));
        }

        var data = await cabinetService.GetCabinetAsync(userUuid);
        return Results.Ok(ResponseMessage.Create(data, string.Empty, HttpStatusCode.OK));
    }

    /// <summary>
    /// Launcher/player self-service Unicore cabinet.
    /// Resolves the player by AccessToken (Gml JWT or Unicore JWT stored on the user).
    /// </summary>
    public static async Task<IResult> GetMyUnicoreCabinet(
        HttpContext httpContext,
        IGmlManager gmlManager,
        IAccessTokenService accessTokenService,
        UnicorePlayerCabinetService cabinetService)
    {
        var authHeader = httpContext.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authHeader)
            || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Results.Unauthorized();
        }

        var token = authHeader["Bearer ".Length..].Trim();
        if (string.IsNullOrWhiteSpace(token))
            return Results.Unauthorized();

        IUser? user = await gmlManager.Users.GetUserByAccessToken(token);

        if (user is null)
        {
            var uuid = TryReadUuidFromJwt(token);
            if (!string.IsNullOrWhiteSpace(uuid))
                user = await gmlManager.Users.GetUserByUuid(uuid);
        }

        if (user is null && accessTokenService.ValidateToken(token))
        {
            var uuid = TryReadUuidFromJwt(token);
            if (!string.IsNullOrWhiteSpace(uuid))
                user = await gmlManager.Users.GetUserByUuid(uuid);
        }

        if (user is null)
            return Results.Unauthorized();

        var data = await cabinetService.GetCabinetAsync(user.Uuid);
        return Results.Ok(ResponseMessage.Create(data, string.Empty, HttpStatusCode.OK));
    }

    private static string? TryReadUuidFromJwt(string jwt)
    {
        try
        {
            var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(jwt);
            return token.Claims.FirstOrDefault(c =>
                       c.Type is "uuid" or "sub" or "nameid"
                       || c.Type == System.Security.Claims.ClaimTypes.NameIdentifier
                       || c.Type.EndsWith("/nameidentifier", StringComparison.OrdinalIgnoreCase))
                   ?.Value;
        }
        catch
        {
            return null;
        }
    }
}

public enum PlayersSortBy
{
    Name = 0,
    AuthCount = 1,
    SessionExpiry = 2
}