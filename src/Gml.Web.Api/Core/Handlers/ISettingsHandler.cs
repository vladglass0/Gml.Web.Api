using AutoMapper;
using Gml.Web.Api.Core.Models.Settings;
using Gml.Web.Api.Core.Repositories;

namespace Gml.Web.Api.Core.Handlers;

public interface ISettingsHandler
{
    static abstract Task<IResult> UpdateSettings(
        ISettingsRepository settingsService,
        IMapper mapper,
        SettingsUpdateRequest settingsDto);

    static abstract Task<IResult> GetSettings(
        ISettingsRepository settingsService,
        IMapper mapper);
}
