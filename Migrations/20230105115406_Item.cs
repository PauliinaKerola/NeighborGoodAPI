using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NeighborGoodAPI.Migrations
{
    public partial class Item : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AddInfo",
                table: "Items",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BorrowTime",
                table: "Items",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddInfo",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "BorrowTime",
                table: "Items");
        }
    }
}
