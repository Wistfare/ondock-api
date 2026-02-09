using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ondock.api.Migrations
{
    /// <inheritdoc />
    public partial class SeedProfileCategoriesAndVehicleData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ProfileCategories",
                columns: new[] { "CategoryId", "IsActive", "Name", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111001"), true, "Commercial Truck Driver", 1 },
                    { new Guid("11111111-1111-1111-1111-111111111007"), true, "Others", 99 }
                });

            migrationBuilder.InsertData(
                table: "VehicleBrands",
                columns: new[] { "BrandId", "IsActive", "Name", "SortOrder", "VehicleTypeId" },
                values: new object[,]
                {
                    { new Guid("44444444-4444-4444-4444-444444444001"), true, "Freightliner", 1, null },
                    { new Guid("44444444-4444-4444-4444-444444444002"), true, "Peterbilt", 2, null },
                    { new Guid("44444444-4444-4444-4444-444444444003"), true, "Kenworth", 3, null },
                    { new Guid("44444444-4444-4444-4444-444444444004"), true, "International", 4, null },
                    { new Guid("44444444-4444-4444-4444-444444444005"), true, "Mack", 5, null },
                    { new Guid("44444444-4444-4444-4444-444444444006"), true, "Western Star", 6, null },
                    { new Guid("44444444-4444-4444-4444-444444444007"), true, "Dina", 7, null },
                    { new Guid("44444444-4444-4444-4444-444444444008"), true, "Giant Motors", 8, null },
                    { new Guid("44444444-4444-4444-4444-444444444009"), true, "Volkswagen", 9, null },
                    { new Guid("44444444-4444-4444-4444-444444444010"), true, "Mercedes-Benz", 10, null },
                    { new Guid("44444444-4444-4444-4444-444444444011"), true, "Renault", 11, null },
                    { new Guid("44444444-4444-4444-4444-444444444012"), true, "Toyota", 12, null },
                    { new Guid("44444444-4444-4444-4444-444444444013"), true, "Iveco", 13, null },
                    { new Guid("44444444-4444-4444-4444-444444444014"), true, "Scania", 14, null },
                    { new Guid("44444444-4444-4444-4444-444444444015"), true, "MAN", 15, null },
                    { new Guid("44444444-4444-4444-4444-444444444016"), true, "DAF", 16, null },
                    { new Guid("44444444-4444-4444-4444-444444444017"), true, "Unimog", 17, null },
                    { new Guid("44444444-4444-4444-4444-444444444018"), true, "Astra", 18, null },
                    { new Guid("44444444-4444-4444-4444-444444444019"), true, "Ginaf", 19, null },
                    { new Guid("44444444-4444-4444-4444-444444444020"), true, "Dennis Eagle", 20, null },
                    { new Guid("44444444-4444-4444-4444-444444444021"), true, "Alexander Dennis", 21, null },
                    { new Guid("44444444-4444-4444-4444-444444444022"), true, "KamAZ", 22, null },
                    { new Guid("44444444-4444-4444-4444-444444444023"), true, "Ural", 23, null },
                    { new Guid("44444444-4444-4444-4444-444444444024"), true, "MAZ", 24, null },
                    { new Guid("44444444-4444-4444-4444-444444444025"), true, "GAZ", 25, null },
                    { new Guid("44444444-4444-4444-4444-444444444026"), true, "Toyota Hino", 26, null },
                    { new Guid("44444444-4444-4444-4444-444444444027"), true, "Isuzu", 27, null },
                    { new Guid("44444444-4444-4444-4444-444444444028"), true, "FAW Jiefang", 28, null },
                    { new Guid("44444444-4444-4444-4444-444444444029"), true, "TATA Motors", 29, null },
                    { new Guid("44444444-4444-4444-4444-444444444030"), true, "Mitsubishi", 30, null },
                    { new Guid("44444444-4444-4444-4444-444444444031"), true, "Suzuki", 31, null },
                    { new Guid("44444444-4444-4444-4444-444444444032"), true, "Fuso", 32, null },
                    { new Guid("44444444-4444-4444-4444-444444444033"), true, "Ashok Leyland", 33, null },
                    { new Guid("44444444-4444-4444-4444-444444444034"), true, "Dongfeng", 34, null },
                    { new Guid("44444444-4444-4444-4444-444444444099"), true, "Others", 99, null }
                });

            migrationBuilder.InsertData(
                table: "VehicleTypes",
                columns: new[] { "VehicleTypeId", "IsActive", "Name", "SortOrder", "SubCategoryId" },
                values: new object[,]
                {
                    { new Guid("33333333-3333-3333-3333-333333333001"), true, "Bobtail", 1, null },
                    { new Guid("33333333-3333-3333-3333-333333333002"), true, "Dry Van Semi-trailer Truck", 2, null },
                    { new Guid("33333333-3333-3333-3333-333333333003"), true, "Reefer Semi-trailer Truck", 3, null },
                    { new Guid("33333333-3333-3333-3333-333333333004"), true, "Standard Flatbed Truck", 4, null },
                    { new Guid("33333333-3333-3333-3333-333333333005"), true, "Container Chassis Truck", 5, null },
                    { new Guid("33333333-3333-3333-3333-333333333006"), true, "Drop-Deck/Step-Deck Trailer Truck", 6, null },
                    { new Guid("33333333-3333-3333-3333-333333333007"), true, "Double Drop/Lowboy Trailer Truck", 7, null },
                    { new Guid("33333333-3333-3333-3333-333333333008"), true, "Conestoga Trailer Truck", 8, null },
                    { new Guid("33333333-3333-3333-3333-333333333009"), true, "Curtainside Trailer Truck", 9, null },
                    { new Guid("33333333-3333-3333-3333-333333333010"), true, "Tanker Truck", 10, null },
                    { new Guid("33333333-3333-3333-3333-333333333011"), true, "Car Hauler", 11, null },
                    { new Guid("33333333-3333-3333-3333-333333333012"), true, "Livestock Truck", 12, null },
                    { new Guid("33333333-3333-3333-3333-333333333013"), true, "Logging Truck", 13, null },
                    { new Guid("33333333-3333-3333-3333-333333333014"), true, "Hot Shot Trailer Truck", 14, null },
                    { new Guid("33333333-3333-3333-3333-333333333015"), true, "Pickup Truck", 15, null },
                    { new Guid("33333333-3333-3333-3333-333333333016"), true, "Box Truck", 16, null },
                    { new Guid("33333333-3333-3333-3333-333333333017"), true, "Dump Truck", 17, null },
                    { new Guid("33333333-3333-3333-3333-333333333018"), true, "Garbage Truck", 18, null },
                    { new Guid("33333333-3333-3333-3333-333333333019"), true, "Tow Truck", 19, null },
                    { new Guid("33333333-3333-3333-3333-333333333020"), true, "Oversize Truck", 20, null },
                    { new Guid("33333333-3333-3333-3333-333333333099"), true, "Others", 99, null }
                });

            migrationBuilder.InsertData(
                table: "ProfileSubCategories",
                columns: new[] { "SubCategoryId", "CategoryId", "IsActive", "Name", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("22222222-2222-2222-2222-222222222001"), new Guid("11111111-1111-1111-1111-111111111001"), true, "OTR Driver (Over The Road)", 1 },
                    { new Guid("22222222-2222-2222-2222-222222222002"), new Guid("11111111-1111-1111-1111-111111111001"), true, "Regional Driver", 2 },
                    { new Guid("22222222-2222-2222-2222-222222222003"), new Guid("11111111-1111-1111-1111-111111111001"), true, "Local Delivery Driver", 3 },
                    { new Guid("22222222-2222-2222-2222-222222222004"), new Guid("11111111-1111-1111-1111-111111111001"), true, "Local Driver", 4 },
                    { new Guid("22222222-2222-2222-2222-222222222005"), new Guid("11111111-1111-1111-1111-111111111001"), true, "Owner-Operator", 5 },
                    { new Guid("22222222-2222-2222-2222-222222222006"), new Guid("11111111-1111-1111-1111-111111111001"), true, "Others", 99 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ProfileCategories",
                keyColumn: "CategoryId",
                keyValue: new Guid("11111111-1111-1111-1111-111111111007"));

            migrationBuilder.DeleteData(
                table: "ProfileSubCategories",
                keyColumn: "SubCategoryId",
                keyValue: new Guid("22222222-2222-2222-2222-222222222001"));

            migrationBuilder.DeleteData(
                table: "ProfileSubCategories",
                keyColumn: "SubCategoryId",
                keyValue: new Guid("22222222-2222-2222-2222-222222222002"));

            migrationBuilder.DeleteData(
                table: "ProfileSubCategories",
                keyColumn: "SubCategoryId",
                keyValue: new Guid("22222222-2222-2222-2222-222222222003"));

            migrationBuilder.DeleteData(
                table: "ProfileSubCategories",
                keyColumn: "SubCategoryId",
                keyValue: new Guid("22222222-2222-2222-2222-222222222004"));

            migrationBuilder.DeleteData(
                table: "ProfileSubCategories",
                keyColumn: "SubCategoryId",
                keyValue: new Guid("22222222-2222-2222-2222-222222222005"));

            migrationBuilder.DeleteData(
                table: "ProfileSubCategories",
                keyColumn: "SubCategoryId",
                keyValue: new Guid("22222222-2222-2222-2222-222222222006"));

            migrationBuilder.DeleteData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444001"));

            migrationBuilder.DeleteData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444002"));

            migrationBuilder.DeleteData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444003"));

            migrationBuilder.DeleteData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444004"));

            migrationBuilder.DeleteData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444005"));

            migrationBuilder.DeleteData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444006"));

            migrationBuilder.DeleteData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444007"));

            migrationBuilder.DeleteData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444008"));

            migrationBuilder.DeleteData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444009"));

            migrationBuilder.DeleteData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444010"));

            migrationBuilder.DeleteData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444011"));

            migrationBuilder.DeleteData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444012"));

            migrationBuilder.DeleteData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444013"));

            migrationBuilder.DeleteData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444014"));

            migrationBuilder.DeleteData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444015"));

            migrationBuilder.DeleteData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444016"));

            migrationBuilder.DeleteData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444017"));

            migrationBuilder.DeleteData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444018"));

            migrationBuilder.DeleteData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444019"));

            migrationBuilder.DeleteData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444020"));

            migrationBuilder.DeleteData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444021"));

            migrationBuilder.DeleteData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444022"));

            migrationBuilder.DeleteData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444023"));

            migrationBuilder.DeleteData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444024"));

            migrationBuilder.DeleteData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444025"));

            migrationBuilder.DeleteData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444026"));

            migrationBuilder.DeleteData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444027"));

            migrationBuilder.DeleteData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444028"));

            migrationBuilder.DeleteData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444029"));

            migrationBuilder.DeleteData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444030"));

            migrationBuilder.DeleteData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444031"));

            migrationBuilder.DeleteData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444032"));

            migrationBuilder.DeleteData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444033"));

            migrationBuilder.DeleteData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444034"));

            migrationBuilder.DeleteData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444099"));

            migrationBuilder.DeleteData(
                table: "VehicleTypes",
                keyColumn: "VehicleTypeId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333001"));

            migrationBuilder.DeleteData(
                table: "VehicleTypes",
                keyColumn: "VehicleTypeId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333002"));

            migrationBuilder.DeleteData(
                table: "VehicleTypes",
                keyColumn: "VehicleTypeId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333003"));

            migrationBuilder.DeleteData(
                table: "VehicleTypes",
                keyColumn: "VehicleTypeId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333004"));

            migrationBuilder.DeleteData(
                table: "VehicleTypes",
                keyColumn: "VehicleTypeId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333005"));

            migrationBuilder.DeleteData(
                table: "VehicleTypes",
                keyColumn: "VehicleTypeId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333006"));

            migrationBuilder.DeleteData(
                table: "VehicleTypes",
                keyColumn: "VehicleTypeId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333007"));

            migrationBuilder.DeleteData(
                table: "VehicleTypes",
                keyColumn: "VehicleTypeId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333008"));

            migrationBuilder.DeleteData(
                table: "VehicleTypes",
                keyColumn: "VehicleTypeId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333009"));

            migrationBuilder.DeleteData(
                table: "VehicleTypes",
                keyColumn: "VehicleTypeId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333010"));

            migrationBuilder.DeleteData(
                table: "VehicleTypes",
                keyColumn: "VehicleTypeId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333011"));

            migrationBuilder.DeleteData(
                table: "VehicleTypes",
                keyColumn: "VehicleTypeId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333012"));

            migrationBuilder.DeleteData(
                table: "VehicleTypes",
                keyColumn: "VehicleTypeId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333013"));

            migrationBuilder.DeleteData(
                table: "VehicleTypes",
                keyColumn: "VehicleTypeId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333014"));

            migrationBuilder.DeleteData(
                table: "VehicleTypes",
                keyColumn: "VehicleTypeId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333015"));

            migrationBuilder.DeleteData(
                table: "VehicleTypes",
                keyColumn: "VehicleTypeId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333016"));

            migrationBuilder.DeleteData(
                table: "VehicleTypes",
                keyColumn: "VehicleTypeId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333017"));

            migrationBuilder.DeleteData(
                table: "VehicleTypes",
                keyColumn: "VehicleTypeId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333018"));

            migrationBuilder.DeleteData(
                table: "VehicleTypes",
                keyColumn: "VehicleTypeId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333019"));

            migrationBuilder.DeleteData(
                table: "VehicleTypes",
                keyColumn: "VehicleTypeId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333020"));

            migrationBuilder.DeleteData(
                table: "VehicleTypes",
                keyColumn: "VehicleTypeId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333099"));

            migrationBuilder.DeleteData(
                table: "ProfileCategories",
                keyColumn: "CategoryId",
                keyValue: new Guid("11111111-1111-1111-1111-111111111001"));
        }
    }
}
