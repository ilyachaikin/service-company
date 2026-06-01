using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceCompany.Infrastructure.Migrations
{

    public partial class AddWorkActAttachments : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.Sql(@"
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

                CREATE INDEX IF NOT EXISTS ""IX_WorkActAttachments_WorkActId""
                    ON ""WorkActAttachments"" (""WorkActId"");
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "WorkActAttachments");
        }
    }
}
