using Backend_Ingenieria_Purrujas.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Backend_Ingenieria_Purrujas.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PromotionsController : ControllerBase
{
    private static readonly List<Promotion> _promotions =
    [
        new Promotion
        {
            PromotionId = 1,
            Name = "Escapada Romántica",
            Link = "http://localhost:4200/about-us#reservas",
            Discount = 25,
            StartDate = new DateTime(2026, 4, 1),
            EndDate = new DateTime(2026, 5, 31),
            RoomTypeId = 2
        },
        new Promotion
        {
            PromotionId = 2,
            Name = "Semana Ecológica",
            Link = "http://localhost:4200/about-us#reservas",
            Discount = 20,
            StartDate = new DateTime(2026, 4, 15),
            EndDate = new DateTime(2026, 6, 30),
            RoomTypeId = 1
        },
        new Promotion
        {
            PromotionId = 3,
            Name = "Aventura Familiar",
            Link = "http://localhost:4200/about-us#reservas",
            Discount = 30,
            StartDate = new DateTime(2026, 5, 1),
            EndDate = new DateTime(2026, 7, 15),
            RoomTypeId = 3
        },
        new Promotion
        {
            PromotionId = 4,
            Name = "Retiro de Bienestar",
            Link = "http://localhost:4200/about-us#reservas",
            Discount = 15,
            StartDate = new DateTime(2026, 6, 1),
            EndDate = new DateTime(2026, 8, 31),
            RoomTypeId = 2
        }
    ];

    [HttpGet]
    public ActionResult<IEnumerable<Promotion>> GetAll()
    {
        return Ok(_promotions);
    }

    [HttpGet("{id}")]
    public ActionResult<Promotion> GetById(int id)
    {
        var promotion = _promotions.FirstOrDefault(p => p.PromotionId == id);
        if (promotion is null) return NotFound();
        return Ok(promotion);
    }
}
