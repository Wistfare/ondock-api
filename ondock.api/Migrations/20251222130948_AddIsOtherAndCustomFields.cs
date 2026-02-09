using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ondock.api.Migrations
{
    /// <inheritdoc />
    public partial class AddIsOtherAndCustomFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOther",
                table: "VehicleTypes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsOther",
                table: "VehicleBrands",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CustomCategory",
                table: "UserProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomSubCategory",
                table: "UserProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomVehicleBrand",
                table: "UserProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomVehicleType",
                table: "UserProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleModel",
                table: "UserProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOther",
                table: "ProfileSubCategories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsOther",
                table: "ProfileCategories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "ProfileCategories",
                keyColumn: "CategoryId",
                keyValue: new Guid("11111111-1111-1111-1111-111111111001"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "ProfileCategories",
                keyColumn: "CategoryId",
                keyValue: new Guid("11111111-1111-1111-1111-111111111007"),
                column: "IsOther",
                value: true);

            migrationBuilder.UpdateData(
                table: "ProfileSubCategories",
                keyColumn: "SubCategoryId",
                keyValue: new Guid("22222222-2222-2222-2222-222222222001"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "ProfileSubCategories",
                keyColumn: "SubCategoryId",
                keyValue: new Guid("22222222-2222-2222-2222-222222222002"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "ProfileSubCategories",
                keyColumn: "SubCategoryId",
                keyValue: new Guid("22222222-2222-2222-2222-222222222003"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "ProfileSubCategories",
                keyColumn: "SubCategoryId",
                keyValue: new Guid("22222222-2222-2222-2222-222222222004"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "ProfileSubCategories",
                keyColumn: "SubCategoryId",
                keyValue: new Guid("22222222-2222-2222-2222-222222222005"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "ProfileSubCategories",
                keyColumn: "SubCategoryId",
                keyValue: new Guid("22222222-2222-2222-2222-222222222006"),
                column: "IsOther",
                value: true);

            migrationBuilder.UpdateData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444001"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444002"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444003"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444004"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444005"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444006"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444007"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444008"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444009"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444010"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444011"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444012"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444013"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444014"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444015"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444016"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444017"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444018"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444019"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444020"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444021"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444022"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444023"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444024"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444025"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444026"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444027"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444028"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444029"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444030"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444031"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444032"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444033"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444034"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleBrands",
                keyColumn: "BrandId",
                keyValue: new Guid("44444444-4444-4444-4444-444444444099"),
                column: "IsOther",
                value: true);

            migrationBuilder.UpdateData(
                table: "VehicleTypes",
                keyColumn: "VehicleTypeId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333001"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleTypes",
                keyColumn: "VehicleTypeId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333002"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleTypes",
                keyColumn: "VehicleTypeId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333003"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleTypes",
                keyColumn: "VehicleTypeId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333004"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleTypes",
                keyColumn: "VehicleTypeId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333005"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleTypes",
                keyColumn: "VehicleTypeId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333006"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleTypes",
                keyColumn: "VehicleTypeId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333007"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleTypes",
                keyColumn: "VehicleTypeId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333008"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleTypes",
                keyColumn: "VehicleTypeId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333009"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleTypes",
                keyColumn: "VehicleTypeId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333010"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleTypes",
                keyColumn: "VehicleTypeId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333011"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleTypes",
                keyColumn: "VehicleTypeId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333012"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleTypes",
                keyColumn: "VehicleTypeId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333013"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleTypes",
                keyColumn: "VehicleTypeId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333014"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleTypes",
                keyColumn: "VehicleTypeId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333015"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleTypes",
                keyColumn: "VehicleTypeId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333016"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleTypes",
                keyColumn: "VehicleTypeId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333017"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleTypes",
                keyColumn: "VehicleTypeId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333018"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleTypes",
                keyColumn: "VehicleTypeId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333019"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleTypes",
                keyColumn: "VehicleTypeId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333020"),
                column: "IsOther",
                value: false);

            migrationBuilder.UpdateData(
                table: "VehicleTypes",
                keyColumn: "VehicleTypeId",
                keyValue: new Guid("33333333-3333-3333-3333-333333333099"),
                column: "IsOther",
                value: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOther",
                table: "VehicleTypes");

            migrationBuilder.DropColumn(
                name: "IsOther",
                table: "VehicleBrands");

            migrationBuilder.DropColumn(
                name: "CustomCategory",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "CustomSubCategory",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "CustomVehicleBrand",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "CustomVehicleType",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "VehicleModel",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "IsOther",
                table: "ProfileSubCategories");

            migrationBuilder.DropColumn(
                name: "IsOther",
                table: "ProfileCategories");
        }
    }
}
