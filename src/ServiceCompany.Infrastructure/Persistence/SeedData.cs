using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceCompany.Infrastructure.Identity;
using ServiceCompany.Infrastructure.Persistence;

namespace ServiceCompany.Infrastructure.Persistence;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<AppDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        var conn = context.Database.GetDbConnection();
        await conn.OpenAsync();
        bool schemaExists;
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT to_regclass('public.\"AspNetUsers\"')";
            var result = await cmd.ExecuteScalarAsync();
            schemaExists = result is not null && result is not DBNull;
        }
        await conn.CloseAsync();

        if (!schemaExists)
        {
            await context.Database.EnsureCreatedAsync();
        }

        await context.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""WorkActAttachments"" (
                ""Id""          uuid        NOT NULL DEFAULT gen_random_uuid(),
                ""FileName""    text        NOT NULL DEFAULT '',
                ""StoragePath"" text        NOT NULL DEFAULT '',
                ""FileType""    text        NOT NULL DEFAULT '',
                ""FileSize""    bigint      NOT NULL DEFAULT 0,
                ""WorkActId""   uuid        NOT NULL,
                ""CreatedAt""   timestamptz NOT NULL DEFAULT NOW(),
                ""CreatedBy""   text        NOT NULL DEFAULT '',
                ""UpdatedAt""   timestamptz NULL,
                ""UpdatedBy""   text        NULL,
                ""IsDeleted""   boolean     NOT NULL DEFAULT false,
                CONSTRAINT ""PK_WorkActAttachments"" PRIMARY KEY (""Id""),
                CONSTRAINT ""FK_WorkActAttachments_WorkActs_WorkActId""
                    FOREIGN KEY (""WorkActId"") REFERENCES ""WorkActs"" (""Id"") ON DELETE CASCADE
            );

            ALTER TABLE ""WorkActAttachments""
                ADD COLUMN IF NOT EXISTS ""UpdatedAt""   timestamptz NULL,
                ADD COLUMN IF NOT EXISTS ""UpdatedBy""   text        NULL,
                ADD COLUMN IF NOT EXISTS ""CreatedBy""   text        NOT NULL DEFAULT '',
                ADD COLUMN IF NOT EXISTS ""IsDeleted""   boolean     NOT NULL DEFAULT false;

            CREATE INDEX IF NOT EXISTS ""IX_WorkActAttachments_WorkActId""
                ON ""WorkActAttachments"" (""WorkActId"");
        ");

        string[] roles = { "Admin", "Manager", "Engineer", "Accountant" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var adminEmail = "admin@servicecompany.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Administrator",
                EmailConfirmed = true,
                IsActive = true
            };

            await userManager.CreateAsync(adminUser, "Admin123!");
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}
