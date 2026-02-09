using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using ondock.api.Services.Interfaces;

// Aliases to resolve ambiguity
using GrpcEmpty = Ondock.Api.Grpc.V1.Empty;
using GrpcVehicleType = Ondock.Api.Grpc.V1.VehicleType;
using GrpcVehicleBrand = Ondock.Api.Grpc.V1.VehicleBrand;
using Ondock.Api.Grpc.V1;

namespace ondock.api.Grpc.Services;

[Authorize]
public class CategoriesGrpcService : CategoriesService.CategoriesServiceBase
{
    private readonly IProfileCategoryService _categoryService;
    private readonly ILogger<CategoriesGrpcService> _logger;

    public CategoriesGrpcService(IProfileCategoryService categoryService, ILogger<CategoriesGrpcService> logger)
    {
        _categoryService = categoryService;
        _logger = logger;
    }

    public override async Task<GetCategoriesResponse> GetCategories(GrpcEmpty request, ServerCallContext context)
    {
        var categories = await _categoryService.GetGrpcCategoriesAsync();

        var response = new GetCategoriesResponse();
        foreach (var cat in categories)
        {
            response.Categories.Add(new ProfileCategory
            {
                Id = cat.Id.ToString(),
                Name = cat.Name ?? "",
                Description = cat.Description ?? "",
                Icon = cat.Icon ?? "",
                SortOrder = cat.SortOrder
            });
        }

        return response;
    }

    public override async Task<GetSubCategoriesResponse> GetSubCategories(GetSubCategoriesRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.CategoryId, out var categoryId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid category ID"));
        }

        var subCategories = await _categoryService.GetGrpcSubCategoriesAsync(categoryId);

        var response = new GetSubCategoriesResponse();
        foreach (var sub in subCategories)
        {
            response.SubCategories.Add(new ProfileSubCategory
            {
                Id = sub.Id.ToString(),
                CategoryId = sub.CategoryId.ToString(),
                Name = sub.Name ?? "",
                Description = sub.Description ?? "",
                SortOrder = sub.SortOrder
            });
        }

        return response;
    }

    public override async Task<GetVehicleTypesResponse> GetVehicleTypes(GrpcEmpty request, ServerCallContext context)
    {
        var vehicleTypes = await _categoryService.GetGrpcVehicleTypesAsync();

        var response = new GetVehicleTypesResponse();
        foreach (var vt in vehicleTypes)
        {
            response.VehicleTypes.Add(new GrpcVehicleType
            {
                Id = vt.Id.ToString(),
                Name = vt.Name ?? "",
                Description = vt.Description ?? "",
                Icon = vt.Icon ?? "",
                IsOther = vt.IsOther,
                SortOrder = vt.SortOrder
            });
        }

        return response;
    }

    public override async Task<GetVehicleBrandsResponse> GetVehicleBrands(GrpcEmpty request, ServerCallContext context)
    {
        var vehicleBrands = await _categoryService.GetGrpcVehicleBrandsAsync();

        var response = new GetVehicleBrandsResponse();
        foreach (var vb in vehicleBrands)
        {
            response.VehicleBrands.Add(new GrpcVehicleBrand
            {
                Id = vb.Id.ToString(),
                Name = vb.Name ?? "",
                LogoUrl = vb.LogoUrl ?? "",
                IsOther = vb.IsOther,
                SortOrder = vb.SortOrder
            });
        }

        return response;
    }
}
