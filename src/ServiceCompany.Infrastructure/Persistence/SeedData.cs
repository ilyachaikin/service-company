using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceCompany.Infrastructure.Identity;

namespace ServiceCompany.Infrastructure.Persistence;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var context     = serviceProvider.GetRequiredService<AppDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await context.Database.MigrateAsync();
                break;
            }
            catch when (attempt < 5)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
            }
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
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        await EnsureUser(userManager, "admin@servicecompany.com",    "Admin123!",    "System Administrator",      "Admin");
        await EnsureUser(userManager, "styazhkin@gmail.com",         "Engineer123!", "Стяжкин Кирилл Сергеевич",  "Engineer");

        if (await context.SlaPolicies.AnyAsync()) return;

        await context.Database.ExecuteSqlRawAsync(@"
            INSERT INTO ""SlaPolicies"" (""Id"",""Name"",""Description"",""ResponseTimeHours"",""ResolutionTimeHours"",""CreatedAt"",""CreatedBy"",""UpdatedAt"",""UpdatedBy"",""IsDeleted"")
            VALUES
              ('a1cfee27-9749-4aee-9a75-c380d7e88a09','Ежемесячное ТО','Ежемесячно проезжать и проверять работоспособность оборудования',12,24,'2026-05-31 18:17:00.951873+00','admin@servicecompany.com','2026-05-31 18:17:00.951875+00','admin@servicecompany.com',false),
              ('337e267d-fd22-4c59-bdad-4d25b2822cf9','Критическая заявка','Решить проблему на объекте в течение дня, когда была дана заявка',2,6,'2026-05-31 18:17:47.780229+00','admin@servicecompany.com','2026-05-31 18:17:47.780230+00','admin@servicecompany.com',false)
            ON CONFLICT (""Id"") DO NOTHING;
        ");

        await context.Database.ExecuteSqlRawAsync(@"
            INSERT INTO ""Clients"" (""Id"",""Name"",""Inn"",""Address"",""Email"",""PhoneNumber"",""IsActive"",""CreatedAt"",""CreatedBy"",""UpdatedAt"",""UpdatedBy"",""IsDeleted"")
            VALUES
              ('136b2abe-3e9c-466d-b5be-2c5868576038','Информационно-аналитический центр Карагандинской области','151240015176','Улица Мустафина, 6/4',NULL,'+7 (7212) 21-53-13',true,'2026-05-31 18:03:19.065530+00','admin@servicecompany.com','2026-05-31 18:03:19.065532+00','admin@servicecompany.com',false),
              ('0c3160d5-f546-4b3b-b5e5-929de12ac5e5','Общеобразовательная школа №88','950640001165','Улица Мустафина, 28','sch88@kargoo.kz','+7 (7212) 56-36-35',true,'2026-05-31 18:05:24.692357+00','admin@servicecompany.com','2026-05-31 18:05:24.692359+00','admin@servicecompany.com',false),
              ('25fead2c-fb6c-4678-891f-0b789d721b60','Русский драматический театр им. К.С. Станиславского','990140002668','Проспект Нурсултана Назарбаева, 19/1','stan_dram@mail.ru','+7 (7212) 56-56-74',true,'2026-05-31 18:10:54.087598+00','admin@servicecompany.com','2026-05-31 18:10:54.087615+00','admin@servicecompany.com',false),
              ('6c63d2ae-ded0-44e0-a47b-513a84f80c78','Ясли-сад ""Айналайын""','990140003273','ул. Ержанова 63а','ds.ainalaion@mail.ru','8 (7212) 43-59-78',true,'2026-05-31 18:12:38.386688+00','admin@servicecompany.com','2026-05-31 18:12:38.386690+00','admin@servicecompany.com',false),
              ('eb878d29-c8de-4a22-9cd4-ca96d55cf182','Специальная школа-интернат №2','950740000091','г. Караганда, Сатыбалдина, 21','gulnur_b10l@mail.ru','8 (7212) 25-77-05',true,'2026-05-31 18:15:06.755572+00','admin@servicecompany.com','2026-05-31 18:15:06.755573+00','admin@servicecompany.com',false),
              ('375e796e-797e-4ed4-9684-bd6d837db7a1','ИП ""UNI PRO""','1234567890','г. Сарань','unipro@gmail.com','+77771112233',true,'2026-05-31 08:55:05.402367+00','admin@servicecompany.com','2026-05-31 18:12:47.354989+00','admin@servicecompany.com',true)
            ON CONFLICT (""Id"") DO NOTHING;
        ");

        await context.Database.ExecuteSqlRawAsync(@"
            INSERT INTO ""ContactPersons"" (""Id"",""FirstName"",""LastName"",""Position"",""Email"",""PhoneNumber"",""ClientId"",""CreatedAt"",""CreatedBy"",""UpdatedAt"",""UpdatedBy"",""IsDeleted"")
            VALUES
              ('84a0c626-9c5e-4c8d-8414-80e9c2295211','Кирилл','Стяжкин','Главный инженер','styazhkin@gmail.com','+77771234567','375e796e-797e-4ed4-9684-bd6d837db7a1','2026-05-31 08:55:29.258841+00','admin@servicecompany.com','2026-05-31 08:55:29.258843+00','admin@servicecompany.com',false)
            ON CONFLICT (""Id"") DO NOTHING;
        ");

        await context.Database.ExecuteSqlRawAsync(@"
            INSERT INTO ""ServiceObjects"" (""Id"",""Name"",""Address"",""Description"",""Location"",""IsActive"",""ClientId"",""CreatedAt"",""CreatedBy"",""UpdatedAt"",""UpdatedBy"",""IsDeleted"")
            VALUES
              ('1cbf8724-4760-4450-9fac-6bce04445bec','Главный офис','Ерубаева 45',NULL,'0101000020E61000003CCDB85FF44552400A40ED0104E74840',true,'375e796e-797e-4ed4-9684-bd6d837db7a1','2026-05-31 08:56:23.593566+00','admin@servicecompany.com','2026-05-31 08:56:23.593570+00','admin@servicecompany.com',false),
              ('419c6d1e-1eb2-4db4-ab94-fe205b9a1c5c','Информационно-аналитический центр','Мустафина 6/4',NULL,'0101000020E6100000FD1AA43B634752405DE4F96761E84840',true,'136b2abe-3e9c-466d-b5be-2c5868576038','2026-05-31 18:20:06.379367+00','admin@servicecompany.com','2026-05-31 18:20:06.379369+00','admin@servicecompany.com',false),
              ('d068b4ca-c2dd-4b03-98f1-1eb88308a734','Русский драматический театр им. К.С. Станиславского','Проспект Нурсултана Назарбаева, 19/1',NULL,'0101000020E6100000FA14B655FF455240088E70FF36E84840',true,'25fead2c-fb6c-4678-891f-0b789d721b60','2026-05-31 18:20:48.021665+00','admin@servicecompany.com','2026-05-31 18:20:48.021667+00','admin@servicecompany.com',false),
              ('06ee0ae7-cf7a-43c0-8225-c73ba8a175ba','Общеобразовательная школа №88','Улица Мустафина, 28',NULL,'0101000020E610000079ECC26A07475240DB3B592030E94840',true,'0c3160d5-f546-4b3b-b5e5-929de12ac5e5','2026-05-31 18:21:17.033462+00','admin@servicecompany.com','2026-05-31 18:21:17.033463+00','admin@servicecompany.com',false),
              ('3a449f80-523b-470e-8ab2-398ec8944292','Специальная школа-интернат №2','Сатыбалдина 21',NULL,'0101000020E61000002B775556094952400AC7E2EDF7E34840',true,'eb878d29-c8de-4a22-9cd4-ca96d55cf182','2026-05-31 18:21:40.082740+00','admin@servicecompany.com','2026-05-31 18:21:40.082741+00','admin@servicecompany.com',false),
              ('1abe797f-a13b-4ae9-b511-224287aea266','Ясли-сад ""Айналайын""','Ержанова, 63',NULL,'0101000020E6100000612294ADE44452403B3B74D597E54840',true,'6c63d2ae-ded0-44e0-a47b-513a84f80c78','2026-05-31 18:21:55.827877+00','admin@servicecompany.com','2026-05-31 18:21:55.827884+00','admin@servicecompany.com',false)
            ON CONFLICT (""Id"") DO NOTHING;
        ");

        await context.Database.ExecuteSqlRawAsync(@"
            INSERT INTO ""Equipments"" (""Id"",""Name"",""SerialNumber"",""Model"",""Manufacturer"",""PurchaseDate"",""WarrantyExpiryDate"",""Status"",""IsActive"",""ServiceObjectId"",""CreatedAt"",""CreatedBy"",""UpdatedAt"",""UpdatedBy"",""IsDeleted"")
            VALUES
              ('61f7d7bf-0c89-467e-9fe8-686c4bd00128','Мультиметр цифровой DT-830B','SN-2024-0047','DT-830B','UNI-T','2025-11-30 19:00:00+00','2026-11-30 19:00:00+00',0,true,'419c6d1e-1eb2-4db4-ab94-fe205b9a1c5c','2026-05-31 18:24:33.545608+00','admin@servicecompany.com','2026-05-31 18:24:33.545609+00','admin@servicecompany.com',false)
            ON CONFLICT (""Id"") DO NOTHING;
        ");

        await context.Database.ExecuteSqlRawAsync(@"
            INSERT INTO ""Contracts"" (""Id"",""Number"",""StartDate"",""EndDate"",""TotalAmount"",""Status"",""ClientId"",""SlaPolicyId"",""CreatedAt"",""CreatedBy"",""UpdatedAt"",""UpdatedBy"",""IsDeleted"")
            VALUES
              ('1dfc0e2a-837b-4ed0-aea4-2ff578f86ed2','ДГ 2026-01-01','2026-01-03 19:00:00+00','2026-12-30 19:00:00+00',150000,1,'136b2abe-3e9c-466d-b5be-2c5868576038','a1cfee27-9749-4aee-9a75-c380d7e88a09','2026-05-31 18:18:17.094229+00','admin@servicecompany.com','2026-05-31 18:18:17.094231+00','admin@servicecompany.com',false),
              ('5b50843f-cf72-42eb-b546-47c1193490e9','ДГ 2026-01-02','2026-01-04 19:00:00+00','2026-12-30 19:00:00+00',120000,1,'0c3160d5-f546-4b3b-b5e5-929de12ac5e5','a1cfee27-9749-4aee-9a75-c380d7e88a09','2026-05-31 18:18:54.404980+00','admin@servicecompany.com','2026-05-31 18:18:54.404981+00','admin@servicecompany.com',false)
            ON CONFLICT (""Id"") DO NOTHING;
        ");

        var engineerId = (await userManager.FindByEmailAsync("styazhkin@gmail.com"))?.Id ?? "";

        await context.Database.ExecuteSqlRawAsync($@"
            INSERT INTO ""MaintenancePlans"" (""Id"",""ServiceObjectId"",""EquipmentId"",""Title"",""Description"",""CronExpression"",""StartDate"",""EndDate"",""IsActive"",""LastGeneratedDate"",""LastGeneratedTicketId"",""ChecklistTemplateJson"",""DefaultEngineerId"",""DefaultPriority"",""CreatedAt"",""CreatedBy"",""UpdatedAt"",""UpdatedBy"",""IsDeleted"")
            VALUES
              ('1f67db4b-44b0-4088-bc3a-e8eee21db12c','419c6d1e-1eb2-4db4-ab94-fe205b9a1c5c','61f7d7bf-0c89-467e-9fe8-686c4bd00128','Ежемесячное ТО Пожарной сигнализации','Раз в месяц если нету заявок приезжать, осматривать объект, проводить консультацию персоналу, проверять ПС, производить контрольное срабатывание ПС','0 9 1 * *','2026-01-03 19:00:00+00','2026-12-30 19:00:00+00',true,NULL,NULL,NULL,'{engineerId}',2,'2026-05-31 18:26:13.297869+00','admin@servicecompany.com','2026-05-31 19:00:10.805645+00','System',false)
            ON CONFLICT (""Id"") DO NOTHING;
        ");

        await context.Database.ExecuteSqlRawAsync(@"
            INSERT INTO ""Tickets"" (""Id"",""Title"",""Description"",""Status"",""Priority"",""Type"",""ClientId"",""ServiceObjectId"",""EquipmentId"",""AssignedUserId"",""DueDate"",""CompletedAt"",""SlaResponseDeadline"",""SlaResolutionDeadline"",""IsSlaBreached"",""MaintenancePlanId"",""ChecklistResultJson"",""CreatedAt"",""CreatedBy"",""UpdatedAt"",""UpdatedBy"",""IsDeleted"")
            VALUES
              ('f22b7a8e-bcab-432b-b6c8-3583af24e2b6','Неисправность ПС','Сработала пожарная сигнализация, нужно проверить датчики, произвести чистку',2,0,0,'136b2abe-3e9c-466d-b5be-2c5868576038','419c6d1e-1eb2-4db4-ab94-fe205b9a1c5c',NULL,NULL,NULL,NULL,NULL,NULL,false,NULL,NULL,'2026-05-31 18:23:54.864389+00','admin@servicecompany.com','2026-05-31 18:26:46.092779+00','admin@servicecompany.com',false)
            ON CONFLICT (""Id"") DO NOTHING;
        ");

        await context.Database.ExecuteSqlRawAsync(@"
            INSERT INTO ""TicketStatusHistory"" (""Id"",""OldStatus"",""NewStatus"",""ChangedByUserId"",""Comment"",""TicketId"",""CreatedAt"",""CreatedBy"",""UpdatedAt"",""UpdatedBy"",""IsDeleted"")
            VALUES
              ('b8b18a04-441f-47c8-8bda-3aedcfe34a88',0,2,(SELECT ""Id"" FROM ""AspNetUsers"" WHERE ""Email""='admin@servicecompany.com'),'Статус изменён вручную','f22b7a8e-bcab-432b-b6c8-3583af24e2b6','2026-05-31 18:26:46.092777+00','admin@servicecompany.com','2026-05-31 18:26:46.092778+00','admin@servicecompany.com',false)
            ON CONFLICT (""Id"") DO NOTHING;
        ");
    }

    private static async Task<ApplicationUser> EnsureUser(
        UserManager<ApplicationUser> userManager,
        string email, string password, string fullName, string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user != null) return user;

        user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            EmailConfirmed = true,
            IsActive = true,
        };
        await userManager.CreateAsync(user, password);
        await userManager.AddToRoleAsync(user, role);
        return user;
    }
}
