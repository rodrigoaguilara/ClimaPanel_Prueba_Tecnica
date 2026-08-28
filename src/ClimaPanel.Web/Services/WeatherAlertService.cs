using ClimaPanel.Web.Data;
using ClimaPanel.Web.Models;
using ClimaPanel.Web.Models.ViewModels;
// se agregan using para implementar alertas por umbral
using ClimaPanel.Web.Common;
using Microsoft.EntityFrameworkCore;

namespace ClimaPanel.Web.Services;

/// <summary>
/// Contrato inicial de la funcionalidad nueva. Debe implementar el flujo
/// completo sin cambiar estas firmas públicas.
/// </summary>
public sealed class WeatherAlertService
{
    private readonly AppDbContext _db;

    public WeatherAlertService(AppDbContext db)
    {
        _db = db;
    }

    // lista solo alertas de una ciudad del usuario
    public async Task<IReadOnlyList<WeatherAlertItem>> ListAsync(
        string userId,
        Guid favoriteId,
        CancellationToken cancellationToken)
    {
        var favoriteExists = await _db.FavoriteCities
            .AsNoTracking()
            .AnyAsync(
                x => x.Id == favoriteId && x.UserId == userId,
                cancellationToken);

        if (!favoriteExists)
        {
            throw new UserMessageException(
                "No se encontró la ciudad solicitada.");
        }

        return await _db.WeatherAlerts
            .AsNoTracking()
            .Where(x => x.FavoriteId == favoriteId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new WeatherAlertItem(
                x.Id,
                x.FavoriteId,
                x.Metric,
                x.Operator,
                x.Threshold,
                x.IsEnabled,
                x.IsTriggered,
                x.CreatedAtUtc,
                x.LastEvaluatedAtUtc,
                x.LastTriggeredAtUtc))
            .ToArrayAsync(cancellationToken);
    }

    // crea una alerta validando usuario, limite y rango
    public async Task<WeatherAlertItem> CreateAsync(
        string userId,
        CreateWeatherAlertInput input,
        CancellationToken cancellationToken)
    {
        var favoriteExists = await _db.FavoriteCities
            .AsNoTracking()
            .AnyAsync(
                x => x.Id == input.FavoriteId &&
                    x.UserId == userId,
                cancellationToken);

        if (!favoriteExists)
        {
            throw new UserMessageException(
                "No se encontró la ciudad solicitada.");
        }

        var activeAlerts = await _db.WeatherAlerts
            .CountAsync(
                x => x.FavoriteId == input.FavoriteId &&
                    x.IsEnabled,
                cancellationToken);

        if (activeAlerts >= 5)
        {
            throw new UserMessageException(
                "La ciudad ya tiene 5 alertas activas.");
        }

        // valida el rango segun la metrica
        var validThreshold = input.Metric switch
        {
            WeatherMetric.TemperatureC =>
                input.Threshold >= -80 && input.Threshold <= 80,

            WeatherMetric.HumidityPercent =>
                input.Threshold >= 0 && input.Threshold <= 100,

            WeatherMetric.PrecipitationMm =>
                input.Threshold >= 0 && input.Threshold <= 500,

            WeatherMetric.WindSpeedKmh =>
                input.Threshold >= 0 && input.Threshold <= 300,

            _ => false
        };

        if (!validThreshold)
        {
            throw new UserMessageException(
                "El valor de la alerta está fuera del rango permitido.");
        }

        // valida el operador recibido
        if (!Enum.IsDefined(input.Operator))
        {
            throw new UserMessageException(
                "El operador de la alerta no es válido.");
        }

        var entity = new WeatherAlert
        {
            FavoriteId = input.FavoriteId,
            Metric = input.Metric,
            Operator = input.Operator,
            Threshold = input.Threshold,
            IsEnabled = true,
            IsTriggered = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.WeatherAlerts.Add(entity);

        await _db.SaveChangesAsync(cancellationToken);

        return new WeatherAlertItem(
            entity.Id,
            entity.FavoriteId,
            entity.Metric,
            entity.Operator,
            entity.Threshold,
            entity.IsEnabled,
            entity.IsTriggered,
            entity.CreatedAtUtc,
            entity.LastEvaluatedAtUtc,
            entity.LastTriggeredAtUtc);
    }

    // activa o desactiva una alerta del usuario
    public async Task ToggleAsync(
        string userId,
        Guid favoriteId,
        Guid alertId,
        CancellationToken cancellationToken)
    {
        var favoriteExists = await _db.FavoriteCities
            .AsNoTracking()
            .AnyAsync(
                x => x.Id == favoriteId &&
                    x.UserId == userId,
                cancellationToken);

        if (!favoriteExists)
        {
            throw new UserMessageException(
                "No se encontró la ciudad solicitada.");
        }

        var alert = await _db.WeatherAlerts
            .FirstOrDefaultAsync(
                x => x.Id == alertId &&
                    x.FavoriteId == favoriteId,
                cancellationToken);

        if (alert is null)
        {
            throw new UserMessageException(
                "No se encontró la alerta solicitada.");
        }

        // al activar controla el limite de 5
        if (!alert.IsEnabled)
        {
            var activeAlerts = await _db.WeatherAlerts
                .CountAsync(
                    x => x.FavoriteId == favoriteId &&
                        x.IsEnabled,
                    cancellationToken);

            if (activeAlerts >= 5)
            {
                throw new UserMessageException(
                    "La ciudad ya tiene 5 alertas activas.");
            }
        }

        alert.IsEnabled = !alert.IsEnabled;

        // una alerta desactivada no queda marcada como disparada
        if (!alert.IsEnabled)
        {
            alert.IsTriggered = false;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    // elimina una alerta de una ciudad del usuario
    public async Task DeleteAsync(
        string userId,
        Guid favoriteId,
        Guid alertId,
        CancellationToken cancellationToken)
    {
        var favoriteExists = await _db.FavoriteCities
            .AsNoTracking()
            .AnyAsync(
                x => x.Id == favoriteId &&
                    x.UserId == userId,
                cancellationToken);

        if (!favoriteExists)
        {
            throw new UserMessageException(
                "No se encontró la ciudad solicitada.");
        }

        var alert = await _db.WeatherAlerts
            .FirstOrDefaultAsync(
                x => x.Id == alertId &&
                    x.FavoriteId == favoriteId,
                cancellationToken);

        if (alert is null)
        {
            throw new UserMessageException(
                "No se encontró la alerta solicitada.");
        }

        _db.WeatherAlerts.Remove(alert);

        await _db.SaveChangesAsync(cancellationToken);
    }

    // evalua las alertas activas con el clima actual
    public async Task<AlertEvaluationResult> EvaluateAsync(
        string userId,
        Guid favoriteId,
        WeatherCard weather,
        CancellationToken cancellationToken)
    {
        var favoriteExists = await _db.FavoriteCities
            .AsNoTracking()
            .AnyAsync(
                x => x.Id == favoriteId &&
                    x.UserId == userId,
                cancellationToken);

        if (!favoriteExists)
        {
            throw new UserMessageException(
                "No se encontró la ciudad solicitada.");
        }

        var alerts = await _db.WeatherAlerts
            .Where(x => x.FavoriteId == favoriteId &&
                        x.IsEnabled)
            .ToListAsync(cancellationToken);

        var evaluatedAt = DateTime.UtcNow;
        var triggered = 0;

        foreach (var alert in alerts)
        {
            var currentValue = alert.Metric switch
            {
                WeatherMetric.TemperatureC => weather.TemperatureC,
                WeatherMetric.HumidityPercent => weather.HumidityPercent,
                WeatherMetric.PrecipitationMm => weather.PrecipitationMm,
                WeatherMetric.WindSpeedKmh => weather.WindSpeedKmh,
                _ => double.NaN
            };

            var isTriggered = alert.Operator switch
            {
                ThresholdOperator.GreaterThanOrEqual =>
                    currentValue >= alert.Threshold,

                ThresholdOperator.LessThanOrEqual =>
                    currentValue <= alert.Threshold,

                _ => false
            };

            alert.IsTriggered = isTriggered;
            alert.LastEvaluatedAtUtc = evaluatedAt;

            if (isTriggered)
            {
                alert.LastTriggeredAtUtc = evaluatedAt;
                triggered++;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new AlertEvaluationResult(
            alerts.Count,
            triggered);
    }
}
