using Microsoft.EntityFrameworkCore;

namespace ondock.api.Data;

public static class DatabaseSeeder
{
    /// <summary>
    /// Seeds profile data if tables are empty. Safe to call on every startup.
    /// </summary>
    public static void SeedProfileData(OnDockDbContext dbContext)
    {
        var categoryCount = dbContext.Database.ExecuteSqlRaw(@"
            INSERT INTO ""ProfileCategories"" (""CategoryId"", ""Name"", ""SortOrder"", ""IsActive"", ""IsOther"")
            SELECT * FROM (VALUES
                ('11111111-1111-1111-1111-111111111001'::uuid, 'Commercial Truck Driver', 1, true, false),
                ('11111111-1111-1111-1111-111111111007'::uuid, 'Others', 99, true, true)
            ) AS v(""CategoryId"", ""Name"", ""SortOrder"", ""IsActive"", ""IsOther"")
            WHERE NOT EXISTS (SELECT 1 FROM ""ProfileCategories"" LIMIT 1)
        ");

        dbContext.Database.ExecuteSqlRaw(@"
            INSERT INTO ""ProfileSubCategories"" (""SubCategoryId"", ""CategoryId"", ""Name"", ""SortOrder"", ""IsActive"", ""IsOther"")
            SELECT * FROM (VALUES
                ('22222222-2222-2222-2222-222222222001'::uuid, '11111111-1111-1111-1111-111111111001'::uuid, 'OTR Driver (Over The Road)', 1, true, false),
                ('22222222-2222-2222-2222-222222222002'::uuid, '11111111-1111-1111-1111-111111111001'::uuid, 'Regional Driver', 2, true, false),
                ('22222222-2222-2222-2222-222222222003'::uuid, '11111111-1111-1111-1111-111111111001'::uuid, 'Local Delivery Driver', 3, true, false),
                ('22222222-2222-2222-2222-222222222004'::uuid, '11111111-1111-1111-1111-111111111001'::uuid, 'Local Driver', 4, true, false),
                ('22222222-2222-2222-2222-222222222005'::uuid, '11111111-1111-1111-1111-111111111001'::uuid, 'Owner-Operator', 5, true, false),
                ('22222222-2222-2222-2222-222222222006'::uuid, '11111111-1111-1111-1111-111111111001'::uuid, 'Others', 99, true, true)
            ) AS v(""SubCategoryId"", ""CategoryId"", ""Name"", ""SortOrder"", ""IsActive"", ""IsOther"")
            WHERE NOT EXISTS (SELECT 1 FROM ""ProfileSubCategories"" LIMIT 1)
        ");

        dbContext.Database.ExecuteSqlRaw(@"
            INSERT INTO ""VehicleTypes"" (""VehicleTypeId"", ""SubCategoryId"", ""Name"", ""SortOrder"", ""IsActive"", ""IsOther"")
            SELECT * FROM (VALUES
                ('33333333-3333-3333-3333-333333333001'::uuid, NULL::uuid, 'Bobtail', 1, true, false),
                ('33333333-3333-3333-3333-333333333002'::uuid, NULL::uuid, 'Dry Van Semi-trailer Truck', 2, true, false),
                ('33333333-3333-3333-3333-333333333003'::uuid, NULL::uuid, 'Reefer Semi-trailer Truck', 3, true, false),
                ('33333333-3333-3333-3333-333333333004'::uuid, NULL::uuid, 'Standard Flatbed Truck', 4, true, false),
                ('33333333-3333-3333-3333-333333333005'::uuid, NULL::uuid, 'Container Chassis Truck', 5, true, false),
                ('33333333-3333-3333-3333-333333333006'::uuid, NULL::uuid, 'Drop-Deck/Step-Deck Trailer Truck', 6, true, false),
                ('33333333-3333-3333-3333-333333333007'::uuid, NULL::uuid, 'Double Drop/Lowboy Trailer Truck', 7, true, false),
                ('33333333-3333-3333-3333-333333333008'::uuid, NULL::uuid, 'Conestoga Trailer Truck', 8, true, false),
                ('33333333-3333-3333-3333-333333333009'::uuid, NULL::uuid, 'Curtainside Trailer Truck', 9, true, false),
                ('33333333-3333-3333-3333-333333333010'::uuid, NULL::uuid, 'Tanker Truck', 10, true, false),
                ('33333333-3333-3333-3333-333333333011'::uuid, NULL::uuid, 'Car Hauler', 11, true, false),
                ('33333333-3333-3333-3333-333333333012'::uuid, NULL::uuid, 'Livestock Truck', 12, true, false),
                ('33333333-3333-3333-3333-333333333013'::uuid, NULL::uuid, 'Logging Truck', 13, true, false),
                ('33333333-3333-3333-3333-333333333014'::uuid, NULL::uuid, 'Hot Shot Trailer Truck', 14, true, false),
                ('33333333-3333-3333-3333-333333333015'::uuid, NULL::uuid, 'Pickup Truck', 15, true, false),
                ('33333333-3333-3333-3333-333333333016'::uuid, NULL::uuid, 'Box Truck', 16, true, false),
                ('33333333-3333-3333-3333-333333333017'::uuid, NULL::uuid, 'Dump Truck', 17, true, false),
                ('33333333-3333-3333-3333-333333333018'::uuid, NULL::uuid, 'Garbage Truck', 18, true, false),
                ('33333333-3333-3333-3333-333333333019'::uuid, NULL::uuid, 'Tow Truck', 19, true, false),
                ('33333333-3333-3333-3333-333333333020'::uuid, NULL::uuid, 'Oversize Truck', 20, true, false),
                ('33333333-3333-3333-3333-333333333099'::uuid, NULL::uuid, 'Others', 99, true, true)
            ) AS v(""VehicleTypeId"", ""SubCategoryId"", ""Name"", ""SortOrder"", ""IsActive"", ""IsOther"")
            WHERE NOT EXISTS (SELECT 1 FROM ""VehicleTypes"" LIMIT 1)
        ");

        dbContext.Database.ExecuteSqlRaw(@"
            INSERT INTO ""VehicleBrands"" (""BrandId"", ""VehicleTypeId"", ""Name"", ""SortOrder"", ""IsActive"", ""IsOther"")
            SELECT * FROM (VALUES
                ('44444444-4444-4444-4444-444444444001'::uuid, NULL::uuid, 'Freightliner', 1, true, false),
                ('44444444-4444-4444-4444-444444444002'::uuid, NULL::uuid, 'Peterbilt', 2, true, false),
                ('44444444-4444-4444-4444-444444444003'::uuid, NULL::uuid, 'Kenworth', 3, true, false),
                ('44444444-4444-4444-4444-444444444004'::uuid, NULL::uuid, 'International', 4, true, false),
                ('44444444-4444-4444-4444-444444444005'::uuid, NULL::uuid, 'Mack', 5, true, false),
                ('44444444-4444-4444-4444-444444444006'::uuid, NULL::uuid, 'Western Star', 6, true, false),
                ('44444444-4444-4444-4444-444444444007'::uuid, NULL::uuid, 'Dina', 7, true, false),
                ('44444444-4444-4444-4444-444444444008'::uuid, NULL::uuid, 'Giant Motors', 8, true, false),
                ('44444444-4444-4444-4444-444444444009'::uuid, NULL::uuid, 'Volkswagen', 9, true, false),
                ('44444444-4444-4444-4444-444444444010'::uuid, NULL::uuid, 'Mercedes-Benz', 10, true, false),
                ('44444444-4444-4444-4444-444444444011'::uuid, NULL::uuid, 'Renault', 11, true, false),
                ('44444444-4444-4444-4444-444444444012'::uuid, NULL::uuid, 'Toyota', 12, true, false),
                ('44444444-4444-4444-4444-444444444013'::uuid, NULL::uuid, 'Iveco', 13, true, false),
                ('44444444-4444-4444-4444-444444444014'::uuid, NULL::uuid, 'Scania', 14, true, false),
                ('44444444-4444-4444-4444-444444444015'::uuid, NULL::uuid, 'MAN', 15, true, false),
                ('44444444-4444-4444-4444-444444444016'::uuid, NULL::uuid, 'DAF', 16, true, false),
                ('44444444-4444-4444-4444-444444444017'::uuid, NULL::uuid, 'Unimog', 17, true, false),
                ('44444444-4444-4444-4444-444444444018'::uuid, NULL::uuid, 'Astra', 18, true, false),
                ('44444444-4444-4444-4444-444444444019'::uuid, NULL::uuid, 'Ginaf', 19, true, false),
                ('44444444-4444-4444-4444-444444444020'::uuid, NULL::uuid, 'Dennis Eagle', 20, true, false),
                ('44444444-4444-4444-4444-444444444021'::uuid, NULL::uuid, 'Alexander Dennis', 21, true, false),
                ('44444444-4444-4444-4444-444444444022'::uuid, NULL::uuid, 'KamAZ', 22, true, false),
                ('44444444-4444-4444-4444-444444444023'::uuid, NULL::uuid, 'Ural', 23, true, false),
                ('44444444-4444-4444-4444-444444444024'::uuid, NULL::uuid, 'MAZ', 24, true, false),
                ('44444444-4444-4444-4444-444444444025'::uuid, NULL::uuid, 'GAZ', 25, true, false),
                ('44444444-4444-4444-4444-444444444026'::uuid, NULL::uuid, 'Toyota Hino', 26, true, false),
                ('44444444-4444-4444-4444-444444444027'::uuid, NULL::uuid, 'Isuzu', 27, true, false),
                ('44444444-4444-4444-4444-444444444028'::uuid, NULL::uuid, 'FAW Jiefang', 28, true, false),
                ('44444444-4444-4444-4444-444444444029'::uuid, NULL::uuid, 'TATA Motors', 29, true, false),
                ('44444444-4444-4444-4444-444444444030'::uuid, NULL::uuid, 'Mitsubishi', 30, true, false),
                ('44444444-4444-4444-4444-444444444031'::uuid, NULL::uuid, 'Suzuki', 31, true, false),
                ('44444444-4444-4444-4444-444444444032'::uuid, NULL::uuid, 'Fuso', 32, true, false),
                ('44444444-4444-4444-4444-444444444033'::uuid, NULL::uuid, 'Ashok Leyland', 33, true, false),
                ('44444444-4444-4444-4444-444444444034'::uuid, NULL::uuid, 'Dongfeng', 34, true, false),
                ('44444444-4444-4444-4444-444444444099'::uuid, NULL::uuid, 'Others', 99, true, true)
            ) AS v(""BrandId"", ""VehicleTypeId"", ""Name"", ""SortOrder"", ""IsActive"", ""IsOther"")
            WHERE NOT EXISTS (SELECT 1 FROM ""VehicleBrands"" LIMIT 1)
        ");

        if (categoryCount > 0)
        {
            Console.WriteLine("Traditional seeding completed - profile data inserted.");
        }
    }

    /// <summary>
    /// Force re-seeds all profile data (used after CleanupDatabase truncation).
    /// </summary>
    public static void ForceSeedProfileData(OnDockDbContext dbContext)
    {
        Console.WriteLine("Re-seeding profile categories and vehicle data...");

        // Profile Categories
        dbContext.Database.ExecuteSqlRaw(@"
            INSERT INTO ""ProfileCategories"" (""CategoryId"", ""Name"", ""SortOrder"", ""IsActive"", ""IsOther"") VALUES
            ('11111111-1111-1111-1111-111111111001', 'Commercial Truck Driver', 1, true, false),
            ('11111111-1111-1111-1111-111111111007', 'Others', 99, true, true)
        ");

        // Profile SubCategories
        dbContext.Database.ExecuteSqlRaw(@"
            INSERT INTO ""ProfileSubCategories"" (""SubCategoryId"", ""CategoryId"", ""Name"", ""SortOrder"", ""IsActive"", ""IsOther"") VALUES
            ('22222222-2222-2222-2222-222222222001', '11111111-1111-1111-1111-111111111001', 'OTR Driver (Over The Road)', 1, true, false),
            ('22222222-2222-2222-2222-222222222002', '11111111-1111-1111-1111-111111111001', 'Regional Driver', 2, true, false),
            ('22222222-2222-2222-2222-222222222003', '11111111-1111-1111-1111-111111111001', 'Local Delivery Driver', 3, true, false),
            ('22222222-2222-2222-2222-222222222004', '11111111-1111-1111-1111-111111111001', 'Local Driver', 4, true, false),
            ('22222222-2222-2222-2222-222222222005', '11111111-1111-1111-1111-111111111001', 'Owner-Operator', 5, true, false),
            ('22222222-2222-2222-2222-222222222006', '11111111-1111-1111-1111-111111111001', 'Others', 99, true, true)
        ");

        // Vehicle Types
        dbContext.Database.ExecuteSqlRaw(@"
            INSERT INTO ""VehicleTypes"" (""VehicleTypeId"", ""SubCategoryId"", ""Name"", ""SortOrder"", ""IsActive"", ""IsOther"") VALUES
            ('33333333-3333-3333-3333-333333333001', NULL, 'Bobtail', 1, true, false),
            ('33333333-3333-3333-3333-333333333002', NULL, 'Dry Van Semi-trailer Truck', 2, true, false),
            ('33333333-3333-3333-3333-333333333003', NULL, 'Reefer Semi-trailer Truck', 3, true, false),
            ('33333333-3333-3333-3333-333333333004', NULL, 'Standard Flatbed Truck', 4, true, false),
            ('33333333-3333-3333-3333-333333333005', NULL, 'Container Chassis Truck', 5, true, false),
            ('33333333-3333-3333-3333-333333333006', NULL, 'Drop-Deck/Step-Deck Trailer Truck', 6, true, false),
            ('33333333-3333-3333-3333-333333333007', NULL, 'Double Drop/Lowboy Trailer Truck', 7, true, false),
            ('33333333-3333-3333-3333-333333333008', NULL, 'Conestoga Trailer Truck', 8, true, false),
            ('33333333-3333-3333-3333-333333333009', NULL, 'Curtainside Trailer Truck', 9, true, false),
            ('33333333-3333-3333-3333-333333333010', NULL, 'Tanker Truck', 10, true, false),
            ('33333333-3333-3333-3333-333333333011', NULL, 'Car Hauler', 11, true, false),
            ('33333333-3333-3333-3333-333333333012', NULL, 'Livestock Truck', 12, true, false),
            ('33333333-3333-3333-3333-333333333013', NULL, 'Logging Truck', 13, true, false),
            ('33333333-3333-3333-3333-333333333014', NULL, 'Hot Shot Trailer Truck', 14, true, false),
            ('33333333-3333-3333-3333-333333333015', NULL, 'Pickup Truck', 15, true, false),
            ('33333333-3333-3333-3333-333333333016', NULL, 'Box Truck', 16, true, false),
            ('33333333-3333-3333-3333-333333333017', NULL, 'Dump Truck', 17, true, false),
            ('33333333-3333-3333-3333-333333333018', NULL, 'Garbage Truck', 18, true, false),
            ('33333333-3333-3333-3333-333333333019', NULL, 'Tow Truck', 19, true, false),
            ('33333333-3333-3333-3333-333333333020', NULL, 'Oversize Truck', 20, true, false),
            ('33333333-3333-3333-3333-333333333099', NULL, 'Others', 99, true, true)
        ");

        // Vehicle Brands
        dbContext.Database.ExecuteSqlRaw(@"
            INSERT INTO ""VehicleBrands"" (""BrandId"", ""VehicleTypeId"", ""Name"", ""SortOrder"", ""IsActive"", ""IsOther"") VALUES
            ('44444444-4444-4444-4444-444444444001', NULL, 'Freightliner', 1, true, false),
            ('44444444-4444-4444-4444-444444444002', NULL, 'Peterbilt', 2, true, false),
            ('44444444-4444-4444-4444-444444444003', NULL, 'Kenworth', 3, true, false),
            ('44444444-4444-4444-4444-444444444004', NULL, 'International', 4, true, false),
            ('44444444-4444-4444-4444-444444444005', NULL, 'Mack', 5, true, false),
            ('44444444-4444-4444-4444-444444444006', NULL, 'Western Star', 6, true, false),
            ('44444444-4444-4444-4444-444444444007', NULL, 'Dina', 7, true, false),
            ('44444444-4444-4444-4444-444444444008', NULL, 'Giant Motors', 8, true, false),
            ('44444444-4444-4444-4444-444444444009', NULL, 'Volkswagen', 9, true, false),
            ('44444444-4444-4444-4444-444444444010', NULL, 'Mercedes-Benz', 10, true, false),
            ('44444444-4444-4444-4444-444444444011', NULL, 'Renault', 11, true, false),
            ('44444444-4444-4444-4444-444444444012', NULL, 'Toyota', 12, true, false),
            ('44444444-4444-4444-4444-444444444013', NULL, 'Iveco', 13, true, false),
            ('44444444-4444-4444-4444-444444444014', NULL, 'Scania', 14, true, false),
            ('44444444-4444-4444-4444-444444444015', NULL, 'MAN', 15, true, false),
            ('44444444-4444-4444-4444-444444444016', NULL, 'DAF', 16, true, false),
            ('44444444-4444-4444-4444-444444444017', NULL, 'Unimog', 17, true, false),
            ('44444444-4444-4444-4444-444444444018', NULL, 'Astra', 18, true, false),
            ('44444444-4444-4444-4444-444444444019', NULL, 'Ginaf', 19, true, false),
            ('44444444-4444-4444-4444-444444444020', NULL, 'Dennis Eagle', 20, true, false),
            ('44444444-4444-4444-4444-444444444021', NULL, 'Alexander Dennis', 21, true, false),
            ('44444444-4444-4444-4444-444444444022', NULL, 'KamAZ', 22, true, false),
            ('44444444-4444-4444-4444-444444444023', NULL, 'Ural', 23, true, false),
            ('44444444-4444-4444-4444-444444444024', NULL, 'MAZ', 24, true, false),
            ('44444444-4444-4444-4444-444444444025', NULL, 'GAZ', 25, true, false),
            ('44444444-4444-4444-4444-444444444026', NULL, 'Toyota Hino', 26, true, false),
            ('44444444-4444-4444-4444-444444444027', NULL, 'Isuzu', 27, true, false),
            ('44444444-4444-4444-4444-444444444028', NULL, 'FAW Jiefang', 28, true, false),
            ('44444444-4444-4444-4444-444444444029', NULL, 'TATA Motors', 29, true, false),
            ('44444444-4444-4444-4444-444444444030', NULL, 'Mitsubishi', 30, true, false),
            ('44444444-4444-4444-4444-444444444031', NULL, 'Suzuki', 31, true, false),
            ('44444444-4444-4444-4444-444444444032', NULL, 'Fuso', 32, true, false),
            ('44444444-4444-4444-4444-444444444033', NULL, 'Ashok Leyland', 33, true, false),
            ('44444444-4444-4444-4444-444444444034', NULL, 'Dongfeng', 34, true, false),
            ('44444444-4444-4444-4444-444444444099', NULL, 'Others', 99, true, true)
        ");

        Console.WriteLine("Re-seeding completed.");
    }

    /// <summary>
    /// Truncates all tables except migrations history (Development only).
    /// </summary>
    public static void CleanupDatabase(OnDockDbContext dbContext)
    {
        Console.WriteLine("CleanupDatabase is enabled - truncating all tables (including seeded data)...");

        var tableNames = dbContext.Model.GetEntityTypes()
            .Select(t => t.GetTableName())
            .Where(t => t != null && t != "__EFMigrationsHistory")
            .Distinct()
            .ToList();

        foreach (var tableName in tableNames)
        {
            dbContext.Database.ExecuteSqlRaw($"TRUNCATE TABLE \"{tableName}\" CASCADE");
        }

        Console.WriteLine($"Database cleanup completed. Truncated {tableNames.Count} tables.");
    }
}
