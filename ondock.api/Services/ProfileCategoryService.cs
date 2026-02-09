using Microsoft.EntityFrameworkCore;
using ondock.api.Data;
using ondock.api.Data.Entities;
using ondock.api.DTOs.Chat;
using ondock.api.Services.Interfaces;

namespace ondock.api.Services;

public class ProfileCategoryService : IProfileCategoryService
{
    private readonly OnDockDbContext _db;

    public ProfileCategoryService(OnDockDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ProfileCategoryDto>> GetCategoriesAsync()
    {
        var categories = await _db.ProfileCategories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();

        return categories.Select(c => new ProfileCategoryDto
        {
            CategoryId = c.CategoryId,
            Name = c.Name,
            SortOrder = c.SortOrder,
            IsOther = c.IsOther
        }).ToList();
    }

    public async Task<ProfileCategoryDto?> GetCategoryAsync(Guid categoryId)
    {
        var category = await _db.ProfileCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CategoryId == categoryId);

        if (category == null) return null;

        return new ProfileCategoryDto
        {
            CategoryId = category.CategoryId,
            Name = category.Name,
            SortOrder = category.SortOrder,
            IsOther = category.IsOther
        };
    }

    public async Task<IReadOnlyList<ProfileSubCategoryDto>> GetSubCategoriesAsync(Guid categoryId)
    {
        var subCategories = await _db.ProfileSubCategories
            .AsNoTracking()
            .Where(s => s.CategoryId == categoryId && s.IsActive)
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Name)
            .ToListAsync();

        return subCategories.Select(s => new ProfileSubCategoryDto
        {
            SubCategoryId = s.SubCategoryId,
            CategoryId = s.CategoryId,
            Name = s.Name,
            SortOrder = s.SortOrder,
            IsOther = s.IsOther
        }).ToList();
    }

    public async Task<IReadOnlyList<VehicleTypeDto>> GetVehicleTypesAsync(Guid subCategoryId)
    {
        // Return vehicle types that either belong to this subcategory OR are global (null SubCategoryId)
        var vehicleTypes = await _db.VehicleTypes
            .AsNoTracking()
            .Where(v => (v.SubCategoryId == subCategoryId || v.SubCategoryId == null) && v.IsActive)
            .OrderBy(v => v.SortOrder)
            .ThenBy(v => v.Name)
            .ToListAsync();

        return vehicleTypes.Select(v => new VehicleTypeDto
        {
            VehicleTypeId = v.VehicleTypeId,
            SubCategoryId = v.SubCategoryId,
            Name = v.Name,
            SortOrder = v.SortOrder,
            IsOther = v.IsOther
        }).ToList();
    }

    public async Task<IReadOnlyList<VehicleTypeDto>> GetAllVehicleTypesAsync()
    {
        var vehicleTypes = await _db.VehicleTypes
            .AsNoTracking()
            .Where(v => v.IsActive)
            .OrderBy(v => v.SortOrder)
            .ThenBy(v => v.Name)
            .ToListAsync();

        return vehicleTypes.Select(v => new VehicleTypeDto
        {
            VehicleTypeId = v.VehicleTypeId,
            SubCategoryId = v.SubCategoryId,
            Name = v.Name,
            SortOrder = v.SortOrder,
            IsOther = v.IsOther
        }).ToList();
    }

    public async Task<IReadOnlyList<VehicleBrandDto>> GetVehicleBrandsAsync(Guid vehicleTypeId)
    {
        // Return brands that either belong to this vehicle type OR are global (null VehicleTypeId)
        var brands = await _db.VehicleBrands
            .AsNoTracking()
            .Where(b => (b.VehicleTypeId == vehicleTypeId || b.VehicleTypeId == null) && b.IsActive)
            .OrderBy(b => b.SortOrder)
            .ThenBy(b => b.Name)
            .ToListAsync();

        return brands.Select(b => new VehicleBrandDto
        {
            BrandId = b.BrandId,
            VehicleTypeId = b.VehicleTypeId,
            Name = b.Name,
            SortOrder = b.SortOrder,
            IsOther = b.IsOther
        }).ToList();
    }

    public async Task<IReadOnlyList<VehicleBrandDto>> GetAllVehicleBrandsAsync()
    {
        var brands = await _db.VehicleBrands
            .AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.SortOrder)
            .ThenBy(b => b.Name)
            .ToListAsync();

        return brands.Select(b => new VehicleBrandDto
        {
            BrandId = b.BrandId,
            VehicleTypeId = b.VehicleTypeId,
            Name = b.Name,
            SortOrder = b.SortOrder,
            IsOther = b.IsOther
        }).ToList();
    }

    public async Task<ProfileCategoryDto> CreateCategoryAsync(string name, string? description = null)
    {
        var maxSortOrder = await _db.ProfileCategories.MaxAsync(c => (int?)c.SortOrder) ?? 0;

        var category = new ProfileCategory
        {
            CategoryId = Guid.NewGuid(),
            Name = name,
            SortOrder = maxSortOrder + 1,
            IsActive = true
        };

        _db.ProfileCategories.Add(category);
        await _db.SaveChangesAsync();

        return new ProfileCategoryDto
        {
            CategoryId = category.CategoryId,
            Name = category.Name,
            SortOrder = category.SortOrder
        };
    }

    public async Task<ProfileSubCategoryDto> CreateSubCategoryAsync(Guid categoryId, string name, string? description = null)
    {
        var maxSortOrder = await _db.ProfileSubCategories
            .Where(s => s.CategoryId == categoryId)
            .MaxAsync(s => (int?)s.SortOrder) ?? 0;

        var subCategory = new ProfileSubCategory
        {
            SubCategoryId = Guid.NewGuid(),
            CategoryId = categoryId,
            Name = name,
            SortOrder = maxSortOrder + 1,
            IsActive = true
        };

        _db.ProfileSubCategories.Add(subCategory);
        await _db.SaveChangesAsync();

        return new ProfileSubCategoryDto
        {
            SubCategoryId = subCategory.SubCategoryId,
            CategoryId = subCategory.CategoryId,
            Name = subCategory.Name,
            SortOrder = subCategory.SortOrder
        };
    }

    public async Task<VehicleTypeDto> CreateVehicleTypeAsync(Guid subCategoryId, string name, string? description = null)
    {
        var maxSortOrder = await _db.VehicleTypes
            .Where(v => v.SubCategoryId == subCategoryId)
            .MaxAsync(v => (int?)v.SortOrder) ?? 0;

        var vehicleType = new VehicleType
        {
            VehicleTypeId = Guid.NewGuid(),
            SubCategoryId = subCategoryId,
            Name = name,
            SortOrder = maxSortOrder + 1,
            IsActive = true
        };

        _db.VehicleTypes.Add(vehicleType);
        await _db.SaveChangesAsync();

        return new VehicleTypeDto
        {
            VehicleTypeId = vehicleType.VehicleTypeId,
            SubCategoryId = vehicleType.SubCategoryId,
            Name = vehicleType.Name,
            SortOrder = vehicleType.SortOrder
        };
    }

    public async Task<VehicleBrandDto> CreateVehicleBrandAsync(Guid vehicleTypeId, string name, string? description = null)
    {
        var maxSortOrder = await _db.VehicleBrands
            .Where(b => b.VehicleTypeId == vehicleTypeId)
            .MaxAsync(b => (int?)b.SortOrder) ?? 0;

        var brand = new VehicleBrand
        {
            BrandId = Guid.NewGuid(),
            VehicleTypeId = vehicleTypeId,
            Name = name,
            SortOrder = maxSortOrder + 1,
            IsActive = true
        };

        _db.VehicleBrands.Add(brand);
        await _db.SaveChangesAsync();

        return new VehicleBrandDto
        {
            BrandId = brand.BrandId,
            VehicleTypeId = brand.VehicleTypeId,
            Name = brand.Name,
            SortOrder = brand.SortOrder
        };
    }

    #region gRPC Support Methods

    public async Task<IReadOnlyList<GrpcProfileCategoryDto>> GetGrpcCategoriesAsync()
    {
        var categories = await _db.ProfileCategories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();

        return categories.Select(c => new GrpcProfileCategoryDto(
            c.CategoryId,
            c.Name,
            null, // Description not stored
            null, // Icon not stored
            c.SortOrder
        )).ToList();
    }

    public async Task<IReadOnlyList<GrpcProfileSubCategoryDto>> GetGrpcSubCategoriesAsync(Guid categoryId)
    {
        var subCategories = await _db.ProfileSubCategories
            .AsNoTracking()
            .Where(s => s.CategoryId == categoryId && s.IsActive)
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Name)
            .ToListAsync();

        return subCategories.Select(s => new GrpcProfileSubCategoryDto(
            s.SubCategoryId,
            s.CategoryId,
            s.Name,
            null, // Description not stored
            s.SortOrder
        )).ToList();
    }

    public async Task<IReadOnlyList<GrpcVehicleTypeDto>> GetGrpcVehicleTypesAsync()
    {
        var vehicleTypes = await _db.VehicleTypes
            .AsNoTracking()
            .Where(v => v.IsActive)
            .OrderBy(v => v.SortOrder)
            .ThenBy(v => v.Name)
            .ToListAsync();

        return vehicleTypes.Select(v => new GrpcVehicleTypeDto(
            v.VehicleTypeId,
            v.Name,
            null, // Description not stored
            null, // Icon not stored
            v.IsOther,
            v.SortOrder
        )).ToList();
    }

    public async Task<IReadOnlyList<GrpcVehicleBrandDto>> GetGrpcVehicleBrandsAsync()
    {
        var brands = await _db.VehicleBrands
            .AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.SortOrder)
            .ThenBy(b => b.Name)
            .ToListAsync();

        return brands.Select(b => new GrpcVehicleBrandDto(
            b.BrandId,
            b.Name,
            null, // LogoUrl not stored
            b.IsOther,
            b.SortOrder
        )).ToList();
    }

    #endregion
}
