using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceCompany.Infrastructure.Migrations
{

    public partial class AddMaintenancePlanAndTicketFields : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_EquipmentId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_ServiceObjectId",
                table: "Tickets");

            migrationBuilder.AddColumn<string>(
                name: "ChecklistResultJson",
                table: "Tickets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MaintenancePlanId",
                table: "Tickets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChecklistTemplateJson",
                table: "MaintenancePlans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CronExpression",
                table: "MaintenancePlans",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DefaultEngineerId",
                table: "MaintenancePlans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultPriority",
                table: "MaintenancePlans",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "MaintenancePlans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "MaintenancePlans",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EquipmentId",
                table: "MaintenancePlans",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "MaintenancePlans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastGeneratedDate",
                table: "MaintenancePlans",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastGeneratedTicketId",
                table: "MaintenancePlans",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceObjectId",
                table: "MaintenancePlans",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "MaintenancePlans",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "MaintenancePlans",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_AssignedUserId",
                table: "Tickets",
                column: "AssignedUserId",
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_EquipmentId",
                table: "Tickets",
                column: "EquipmentId",
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_MaintenancePlanId",
                table: "Tickets",
                column: "MaintenancePlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_ServiceObjectId",
                table: "Tickets",
                column: "ServiceObjectId",
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Status_CreatedAt",
                table: "Tickets",
                columns: new[] { "Status", "CreatedAt" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenancePlans_EquipmentId",
                table: "MaintenancePlans",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenancePlans_ServiceObjectId",
                table: "MaintenancePlans",
                column: "ServiceObjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenancePlans_Equipments_EquipmentId",
                table: "MaintenancePlans",
                column: "EquipmentId",
                principalTable: "Equipments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenancePlans_ServiceObjects_ServiceObjectId",
                table: "MaintenancePlans",
                column: "ServiceObjectId",
                principalTable: "ServiceObjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_MaintenancePlans_MaintenancePlanId",
                table: "Tickets",
                column: "MaintenancePlanId",
                principalTable: "MaintenancePlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaintenancePlans_Equipments_EquipmentId",
                table: "MaintenancePlans");

            migrationBuilder.DropForeignKey(
                name: "FK_MaintenancePlans_ServiceObjects_ServiceObjectId",
                table: "MaintenancePlans");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_MaintenancePlans_MaintenancePlanId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_AssignedUserId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_EquipmentId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_MaintenancePlanId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_ServiceObjectId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_Status_CreatedAt",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_MaintenancePlans_EquipmentId",
                table: "MaintenancePlans");

            migrationBuilder.DropIndex(
                name: "IX_MaintenancePlans_ServiceObjectId",
                table: "MaintenancePlans");

            migrationBuilder.DropColumn(
                name: "ChecklistResultJson",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "MaintenancePlanId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ChecklistTemplateJson",
                table: "MaintenancePlans");

            migrationBuilder.DropColumn(
                name: "CronExpression",
                table: "MaintenancePlans");

            migrationBuilder.DropColumn(
                name: "DefaultEngineerId",
                table: "MaintenancePlans");

            migrationBuilder.DropColumn(
                name: "DefaultPriority",
                table: "MaintenancePlans");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "MaintenancePlans");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "MaintenancePlans");

            migrationBuilder.DropColumn(
                name: "EquipmentId",
                table: "MaintenancePlans");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "MaintenancePlans");

            migrationBuilder.DropColumn(
                name: "LastGeneratedDate",
                table: "MaintenancePlans");

            migrationBuilder.DropColumn(
                name: "LastGeneratedTicketId",
                table: "MaintenancePlans");

            migrationBuilder.DropColumn(
                name: "ServiceObjectId",
                table: "MaintenancePlans");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "MaintenancePlans");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "MaintenancePlans");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_EquipmentId",
                table: "Tickets",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_ServiceObjectId",
                table: "Tickets",
                column: "ServiceObjectId");
        }
    }
}
