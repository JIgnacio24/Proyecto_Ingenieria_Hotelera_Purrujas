using Backend_Ingenieria_Purrujas.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Backend_Ingenieria_Purrujas.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdvertisingController : ControllerBase
{
    private static readonly List<Advertising> _advertising =
    [
        new Advertising
        {
            AdvertisingId = 1,
            Name = "Descuento en Tours al Volcán Irazú",
            Link = "https://www.costaricaadventuretours.com"
        },
        new Advertising
        {
            AdvertisingId = 2,
            Name = "Spa Las Piedras — Tratamientos Termales",
            Link = "https://www.spalaspiedras.com"
        },
        new Advertising
        {
            AdvertisingId = 3,
            Name = "Restaurante La Catarata — Gastronomía Típica",
            Link = "https://www.restaurantelacatarata.com"
        },
        new Advertising
        {
            AdvertisingId = 4,
            Name = "Birdwatching Turrialba — Avistamiento de Aves",
            Link = "https://www.turrialbabirding.com"
        },
        new Advertising
        {
            AdvertisingId = 5,
            Name = "Rafting Río Reventazón — Aventura Extrema",
            Link = "https://www.riostropicales.com"
        },
        new Advertising
        {
            AdvertisingId = 6,
            Name = "Artesanías Cartago — Arte Local",
            Link = "https://www.artesanias-cartago.com"
        }
    ];

    [HttpGet]
    public ActionResult<IEnumerable<Advertising>> GetAll()
    {
        return Ok(_advertising);
    }

    [HttpGet("{id}")]
    public ActionResult<Advertising> GetById(int id)
    {
        var ad = _advertising.FirstOrDefault(a => a.AdvertisingId == id);
        if (ad is null) return NotFound();
        return Ok(ad);
    }
}
