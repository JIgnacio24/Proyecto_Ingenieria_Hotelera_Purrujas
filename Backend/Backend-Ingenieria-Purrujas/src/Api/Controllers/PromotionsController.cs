using Microsoft.AspNetCore.Mvc;

namespace Backend_Ingenieria_Purrujas.Api.Controllers;

/// <summary>
/// Endpoint público (sin autenticación) para que el sitio cliente
/// muestre las promociones en la página de publicidad.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PromotionsController : ControllerBase
{
    private static readonly List<PublicPromotion> _promotions =
    [
        new(1, "Escapada Romántica",  "http://localhost:4200/about-us#reservas", 25,
            new DateTime(2026, 4, 1),  new DateTime(2026, 5, 31),  2),
        new(2, "Semana Ecológica",    "http://localhost:4200/about-us#reservas", 20,
            new DateTime(2026, 4, 15), new DateTime(2026, 6, 30),  1),
        new(3, "Aventura Familiar",   "http://localhost:4200/about-us#reservas", 30,
            new DateTime(2026, 5, 1),  new DateTime(2026, 7, 15),  3),
        new(4, "Retiro de Bienestar", "http://localhost:4200/about-us#reservas", 15,
            new DateTime(2026, 6, 1),  new DateTime(2026, 8, 31),  2),
    ];

    [HttpGet]
    public ActionResult<IEnumerable<PublicPromotion>> GetAll() => Ok(_promotions);

    [HttpGet("{id}")]
    public ActionResult<PublicPromotion> GetById(int id)
    {
        var p = _promotions.FirstOrDefault(x => x.PromotionId == id);
        return p is null ? NotFound() : Ok(p);
    }
}

public sealed record PublicPromotion(
    int PromotionId,
    string Name,
    string Link,
    int Discount,
    DateTime StartDate,
    DateTime EndDate,
    int RoomTypeId);
