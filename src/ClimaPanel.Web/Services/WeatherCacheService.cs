using System.Collections.Concurrent;
using ClimaPanel.Web.Common;
using ClimaPanel.Web.Models;
using Microsoft.Extensions.Caching.Memory;

namespace ClimaPanel.Web.Services;

public sealed class WeatherCacheService
{
    private readonly IMemoryCache _cache;
    private readonly IWeatherClient _weatherClient;
    private readonly IConfiguration _configuration;

    // controla solicitudes simultaneas por ciudad
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

    public WeatherCacheService(
        IMemoryCache cache,
        IWeatherClient weatherClient,
        IConfiguration configuration)
    {
        _cache = cache;
        _weatherClient = weatherClient;
        _configuration = configuration;
    }

    public async Task<WeatherCard> GetAsync(
        FavoriteCity city,
        bool forceRefresh,
        CancellationToken cancellationToken)
    {
        // cache independiente por ciudad
        var cacheKey = $"forecast-{city.Id}";

        //respaldo para usar si falla el proveedor
        var staleKey = $"forecast-stale-{city.Id}";

        if (!forceRefresh &&
            _cache.TryGetValue(cacheKey, out WeatherCard? cached) &&
            cached is not null)
        {
            return cached with { Source = "CACHE" };
        }

        // evita llamadas duplicadas para la misma ciudad
        var cityLock = _locks.GetOrAdd(
            city.Id,
            _ => new SemaphoreSlim(1, 1));

        await cityLock.WaitAsync(cancellationToken);

        try
        {
            // revisa nuevamente el cache despues de esperar
            if (!forceRefresh &&
                _cache.TryGetValue(cacheKey, out cached) &&
                cached is not null)
            {
                return cached with { Source = "CACHE" };
            }

            try
            {
                var reading = await _weatherClient.GetForecastAsync(
                    city.Latitude,
                    city.Longitude,
                    city.Timezone,
                    cancellationToken);

                var response = new WeatherCard(
                    "LIVE",
                    reading.FetchedAtUtc,
                    reading.TemperatureC,
                    reading.HumidityPercent,
                    reading.PrecipitationMm,
                    reading.WindSpeedKmh,
                    reading.Daily);

                var cacheSeconds = _configuration.GetValue(
                    "OpenMeteo:CacheSeconds",
                    90);

                _cache.Set(
                    cacheKey,
                    response,
                    TimeSpan.FromSeconds(cacheSeconds));

                // mantiene el ultimo dato como respaldo
                _cache.Set(
                    staleKey,
                    response,
                    TimeSpan.FromMinutes(15));

                return response;
            }
            catch (UserMessageException)
            {
                // si falla el proveedor usa el ultimo dato disponible
                if (_cache.TryGetValue(
                    staleKey,
                    out WeatherCard? stale) &&
                    stale is not null)
                {
                    return stale with { Source = "STALE" };
                }

                throw;
            }
        }
        finally
        {
            cityLock.Release();
        }
    }
}