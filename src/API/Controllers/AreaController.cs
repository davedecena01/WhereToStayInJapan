using Microsoft.AspNetCore.Mvc;
using WhereToStayInJapan.Application.DTOs;
using WhereToStayInJapan.Infrastructure.Persistence.Repositories;

namespace WhereToStayInJapan.API.Controllers;

[ApiController]
[Route("api/areas")]
public class AreaController(IStationAreaRepository areaRepo, IFoodRepository foodRepo, IAttractionRepository attractionRepo) : ControllerBase
{
    [HttpGet("{id:guid}/food")]
    public async Task<ActionResult<List<FoodItemDto>>> GetFoodAsync(Guid id, CancellationToken ct)
    {
        var area = await areaRepo.GetByIdAsync(id, ct);
        if (area == null) return NotFound();

        var food = await foodRepo.GetCuratedFoodAsync(id, ct: ct);
        return Ok(food.Select(f => new FoodItemDto(
            f.Name, f.CuisineType, f.Address,
            f.Lat.HasValue ? (double?)Convert.ToDouble(f.Lat.Value) : null,
            f.Lng.HasValue ? (double?)Convert.ToDouble(f.Lng.Value) : null,
            f.Notes, f.IsFeatured)).ToList());
    }

    [HttpGet("{id:guid}/attractions")]
    public async Task<ActionResult<List<AttractionItemDto>>> GetAttractionsAsync(Guid id, CancellationToken ct)
    {
        var area = await areaRepo.GetByIdAsync(id, ct);
        if (area == null) return NotFound();

        var attractions = await attractionRepo.GetCuratedAttractionsAsync(id, ct: ct);
        return Ok(attractions.Select(a => new AttractionItemDto(
            a.Name, a.Category, a.WalkMinutes, a.Notes)).ToList());
    }
}
