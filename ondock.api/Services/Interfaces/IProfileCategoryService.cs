using ondock.api.DTOs.Chat;

namespace ondock.api.Services.Interfaces;

public interface IProfileCategoryService
{
    Task<IReadOnlyList<ProfileCategoryDto>> GetCategoriesAsync();
    Task<ProfileCategoryDto?> GetCategoryAsync(Guid categoryId);
    Task<IReadOnlyList<ProfileSubCategoryDto>> GetSubCategoriesAsync(Guid categoryId);
    Task<IReadOnlyList<VehicleTypeDto>> GetVehicleTypesAsync(Guid subCategoryId);
    Task<IReadOnlyList<VehicleTypeDto>> GetAllVehicleTypesAsync();
    Task<IReadOnlyList<VehicleBrandDto>> GetVehicleBrandsAsync(Guid vehicleTypeId);
    Task<IReadOnlyList<VehicleBrandDto>> GetAllVehicleBrandsAsync();

    // gRPC support methods
    Task<IReadOnlyList<GrpcProfileCategoryDto>> GetGrpcCategoriesAsync();
    Task<IReadOnlyList<GrpcProfileSubCategoryDto>> GetGrpcSubCategoriesAsync(Guid categoryId);
    Task<IReadOnlyList<GrpcVehicleTypeDto>> GetGrpcVehicleTypesAsync();
    Task<IReadOnlyList<GrpcVehicleBrandDto>> GetGrpcVehicleBrandsAsync();

    // Admin operations
    Task<ProfileCategoryDto> CreateCategoryAsync(string name, string? description = null);
    Task<ProfileSubCategoryDto> CreateSubCategoryAsync(Guid categoryId, string name, string? description = null);
    Task<VehicleTypeDto> CreateVehicleTypeAsync(Guid subCategoryId, string name, string? description = null);
    Task<VehicleBrandDto> CreateVehicleBrandAsync(Guid vehicleTypeId, string name, string? description = null);
}

// gRPC-specific DTOs with additional fields
public record GrpcProfileCategoryDto(Guid Id, string? Name, string? Description, string? Icon, int SortOrder);
public record GrpcProfileSubCategoryDto(Guid Id, Guid CategoryId, string? Name, string? Description, int SortOrder);
public record GrpcVehicleTypeDto(Guid Id, string? Name, string? Description, string? Icon, bool IsOther, int SortOrder);
public record GrpcVehicleBrandDto(Guid Id, string? Name, string? LogoUrl, bool IsOther, int SortOrder);
