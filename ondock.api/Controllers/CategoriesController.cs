using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ondock.api.DTOs.Chat;
using ondock.api.Services.Interfaces;

namespace ondock.api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Obsolete("Use gRPC CategoriesService instead. This REST controller is deprecated.")]
public class CategoriesController : ControllerBase
{
    private readonly IProfileCategoryService _categoryService;

    public CategoriesController(IProfileCategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProfileCategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProfileCategoryDto>>> GetCategories()
    {
        var categories = await _categoryService.GetCategoriesAsync();
        return Ok(categories);
    }

    [HttpGet("{categoryId:guid}")]
    [ProducesResponseType(typeof(ProfileCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProfileCategoryDto>> GetCategory(Guid categoryId)
    {
        var category = await _categoryService.GetCategoryAsync(categoryId);
        if (category == null)
        {
            return NotFound();
        }
        return Ok(category);
    }

    [HttpGet("{categoryId:guid}/subcategories")]
    [ProducesResponseType(typeof(IReadOnlyList<ProfileSubCategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProfileSubCategoryDto>>> GetSubCategories(Guid categoryId)
    {
        var subCategories = await _categoryService.GetSubCategoriesAsync(categoryId);
        return Ok(subCategories);
    }

    [HttpGet("subcategories/{subCategoryId:guid}/vehicletypes")]
    [ProducesResponseType(typeof(IReadOnlyList<VehicleTypeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<VehicleTypeDto>>> GetVehicleTypes(Guid subCategoryId)
    {
        var vehicleTypes = await _categoryService.GetVehicleTypesAsync(subCategoryId);
        return Ok(vehicleTypes);
    }

    [HttpGet("vehicletypes/{vehicleTypeId:guid}/brands")]
    [ProducesResponseType(typeof(IReadOnlyList<VehicleBrandDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<VehicleBrandDto>>> GetVehicleBrands(Guid vehicleTypeId)
    {
        var brands = await _categoryService.GetVehicleBrandsAsync(vehicleTypeId);
        return Ok(brands);
    }

    [HttpGet("vehicletypes")]
    [ProducesResponseType(typeof(IReadOnlyList<VehicleTypeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<VehicleTypeDto>>> GetAllVehicleTypes()
    {
        var vehicleTypes = await _categoryService.GetAllVehicleTypesAsync();
        return Ok(vehicleTypes);
    }

    [HttpGet("brands")]
    [ProducesResponseType(typeof(IReadOnlyList<VehicleBrandDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<VehicleBrandDto>>> GetAllVehicleBrands()
    {
        var brands = await _categoryService.GetAllVehicleBrandsAsync();
        return Ok(brands);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ProfileCategoryDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ProfileCategoryDto>> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        var category = await _categoryService.CreateCategoryAsync(request.Name, request.Description);
        return CreatedAtAction(nameof(GetCategory), new { categoryId = category.CategoryId }, category);
    }

    [HttpPost("{categoryId:guid}/subcategories")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ProfileSubCategoryDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ProfileSubCategoryDto>> CreateSubCategory(Guid categoryId, [FromBody] CreateSubCategoryRequest request)
    {
        var subCategory = await _categoryService.CreateSubCategoryAsync(categoryId, request.Name, request.Description);
        return Created($"/api/categories/{categoryId}/subcategories/{subCategory.SubCategoryId}", subCategory);
    }

    [HttpPost("subcategories/{subCategoryId:guid}/vehicletypes")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(VehicleTypeDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<VehicleTypeDto>> CreateVehicleType(Guid subCategoryId, [FromBody] CreateVehicleTypeRequest request)
    {
        var vehicleType = await _categoryService.CreateVehicleTypeAsync(subCategoryId, request.Name, request.Description);
        return Created($"/api/categories/subcategories/{subCategoryId}/vehicletypes/{vehicleType.VehicleTypeId}", vehicleType);
    }

    [HttpPost("vehicletypes/{vehicleTypeId:guid}/brands")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(VehicleBrandDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<VehicleBrandDto>> CreateVehicleBrand(Guid vehicleTypeId, [FromBody] CreateVehicleBrandRequest request)
    {
        var brand = await _categoryService.CreateVehicleBrandAsync(vehicleTypeId, request.Name, request.Description);
        return Created($"/api/categories/vehicletypes/{vehicleTypeId}/brands/{brand.BrandId}", brand);
    }
}

public class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class CreateSubCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class CreateVehicleTypeRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class CreateVehicleBrandRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
