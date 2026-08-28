using ClimaPanel.Web.Common;
using ClimaPanel.Web.Models.ViewModels;
using ClimaPanel.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClimaPanel.Web.Controllers;

public sealed class FavoritosController : Controller
{
    private readonly FavoriteService _service;
    private readonly ICurrentUser _currentUser;
    //se agrega para la nueva funcionalidad
    private readonly WeatherAlertService _alertService;

    //se cambia el constructor para la nueva funcionalidad
    public FavoritosController(
        FavoriteService service,
        WeatherAlertService alertService,
        ICurrentUser currentUser)
    {
        _service = service;
        _alertService = alertService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var user = _currentUser.GetCurrent();
        var model = await _service.ListAsync(
            user.Id,
            search,
            page,
            pageSize,
            cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(
        CreateFavoriteInput input,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "No fue posible agregar la ciudad. Revise los datos recibidos.";
            return RedirectToAction("Index", "Home", new { q = input.Name });
        }

        try
        {
            var user = _currentUser.GetCurrent();
            var entity = await _service.CreateAsync(user.Id, input, cancellationToken);
            TempData["Success"] = $"{entity.Name} fue agregada a sus ciudades.";
            return RedirectToAction(nameof(Detalle), new { id = entity.Id });
        }
        catch (UserMessageException exception)
        {
            TempData["Error"] = exception.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    //se cambia detalle para la nueva funcionalidad
    [HttpGet]
    public async Task<IActionResult> Detalle(
        Guid id,
        CancellationToken cancellationToken)
    {
        var user = _currentUser.GetCurrent();

        var city = await _service.GetAsync(
            user.Id,
            id,
            cancellationToken);

        var weather = await _service.GetWeatherAsync(
            user.Id,
            id,
            cancellationToken);

        // evalua las alertas con el clima obtenido
        await _alertService.EvaluateAsync(
            user.Id,
            id,
            weather,
            cancellationToken);

        var alerts = await _alertService.ListAsync(
            user.Id,
            id,
            cancellationToken);

        return View(new FavoriteDetailsViewModel
        {
            City = city,
            Weather = weather,
            Alerts = alerts,
            NewAlert = new CreateWeatherAlertInput
            {
                FavoriteId = id
            }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Refrescar(
        Guid id,
        CancellationToken cancellationToken)
    {
        // se agrega manejo de errores en la actualizacion manual
        try
        {
            var user = _currentUser.GetCurrent();

            await _service.RefreshAsync(
                user.Id,
                id,
                cancellationToken);

            TempData["Success"] = "El pronóstico fue actualizado.";
        }
        catch (UserMessageException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Detalle), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = _currentUser.GetCurrent();
            await _service.DeleteAsync(user.Id, id, cancellationToken);
            TempData["Success"] = "La ciudad fue eliminada de sus favoritos.";
        }
        catch (UserMessageException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    // crea una nueva alerta
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearAlerta(
        CreateWeatherAlertInput input,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = _currentUser.GetCurrent();

            await _alertService.CreateAsync(
                user.Id,
                input,
                cancellationToken);

            TempData["Success"] = "La alerta fue creada.";
        }
        catch (UserMessageException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(
            nameof(Detalle),
            new { id = input.FavoriteId });
    }

    // activa o desactiva una alerta
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstadoAlerta(
        Guid favoriteId,
        Guid alertId,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = _currentUser.GetCurrent();

            await _alertService.ToggleAsync(
                user.Id,
                favoriteId,
                alertId,
                cancellationToken);

            TempData["Success"] = "La alerta fue actualizada.";
        }
        catch (UserMessageException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(
            nameof(Detalle),
            new { id = favoriteId });
    }

    // elimina una alerta
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarAlerta(
        Guid favoriteId,
        Guid alertId,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = _currentUser.GetCurrent();

            await _alertService.DeleteAsync(
                user.Id,
                favoriteId,
                alertId,
                cancellationToken);

            TempData["Success"] = "La alerta fue eliminada.";
        }
        catch (UserMessageException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(
            nameof(Detalle),
            new { id = favoriteId });
    }
}
