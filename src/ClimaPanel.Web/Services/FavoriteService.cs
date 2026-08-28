using ClimaPanel.Web.Common;
using ClimaPanel.Web.Data;
using ClimaPanel.Web.Models;
using ClimaPanel.Web.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ClimaPanel.Web.Services;

public sealed class FavoriteService
{
    private readonly AppDbContext _db;
    private readonly WeatherCacheService _weatherCache;

    public FavoriteService(AppDbContext db, WeatherCacheService weatherCache)
    {
        _db = db;
        _weatherCache = weatherCache;
    }

    public async Task<FavoriteCity> CreateAsync(
        string userId,
        CreateFavoriteInput input,
        CancellationToken cancellationToken)
    {
        var alreadyExists = await _db.FavoriteCities.AnyAsync(
            x => x.UserId == userId && x.LocationId == input.LocationId,
            cancellationToken);

        if (alreadyExists)
        {
            throw new UserMessageException("La ciudad ya se encuentra en sus favoritos.");
        }

        var entity = new FavoriteCity
        {
            UserId = userId,
            LocationId = input.LocationId,
            Name = input.Name.Trim(),
            Country = input.Country.Trim(),
            CountryCode = input.CountryCode.Trim().ToUpperInvariant(),
            Latitude = input.Latitude,
            Longitude = input.Longitude,
            Timezone = input.Timezone.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.FavoriteCities.Add(entity);

        //se agrega el manejo errores duplicado de concurrencia
        //await _db.SaveChangesAsync(cancellationToken);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException?.Message.Contains("UNIQUE constraint failed") == true)
        {
            throw new UserMessageException(
                "La ciudad ya se encuentra en sus favoritos.");
        }

        return entity;
    }

    // se agrega paginacion desde la base de datos
    public async Task<FavoriteListViewModel> ListAsync(
        string userId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        // filtra primero por usuario
        var query = _db.FavoriteCities
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(x =>
                EF.Functions.Like(x.Name, $"%{search}%") ||
                EF.Functions.Like(x.Country, $"%{search}%"));
        }

        // cuenta los resultados antes de paginar
        var total = await query.CountAsync(cancellationToken);

        // ordena y trae solo la pagina solicitada
        var items = await query
            .OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new FavoriteListItem(
                x.Id,
                x.Name,
                x.Country,
                x.CountryCode,
                x.Timezone,
                x.CreatedAtUtc))
            .ToArrayAsync(cancellationToken);

        return new FavoriteListViewModel
        {
            Items = items,
            Search = search ?? string.Empty,
            Page = page,
            PageSize = pageSize,
            Total = total,
            TotalPages = Math.Max(
                1,
                (int)Math.Ceiling(total / (double)pageSize))
        };
    }

    public async Task<FavoriteCity> GetAsync(
        string userId,
        Guid id,
        CancellationToken cancellationToken)
    {
        return await _db.FavoriteCities
            .AsNoTracking()
            //se modifica la busqueda riegosa
            //.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            .FirstOrDefaultAsync(
                x => x.Id == id && x.UserId == userId,
                cancellationToken)
            ?? throw NotFound();
    }

    public async Task DeleteAsync(
        string userId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var entity = await _db.FavoriteCities
            //se modifica busqueda riesgosa
            //.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            .FirstOrDefaultAsync(
                x => x.Id == id && x.UserId == userId,
                cancellationToken)
            ?? throw NotFound();

        _db.FavoriteCities.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<WeatherCard> GetWeatherAsync(
        string userId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var city = await _db.FavoriteCities
            .AsNoTracking()
            //se modifica busqueda riesgosa
            //.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            .FirstOrDefaultAsync(
                x => x.Id == id && x.UserId == userId,
                cancellationToken)
            ?? throw NotFound();

        return await _weatherCache.GetAsync(city, false, cancellationToken);
    }

    public Task<WeatherCard> RefreshAsync(
        string userId,
        Guid id,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException("La actualización manual todavía no está implementada.");
    }

    private static FavoriteListItem ToListItem(FavoriteCity entity) => new(
        entity.Id,
        entity.Name,
        entity.Country,
        entity.CountryCode,
        entity.Timezone,
        entity.CreatedAtUtc);

    private static UserMessageException NotFound() =>
        new("No se encontró la ciudad solicitada.");
}
