using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class SponsorsOverhaul : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_sich_sponsor_sponsor_rank_sponsor_rank_id",
                table: "sich_sponsor");

            migrationBuilder.DropPrimaryKey(
                name: "PK_sponsor_rank",
                table: "sponsor_rank");

            migrationBuilder.DropPrimaryKey(
                name: "PK_sich_sponsor",
                table: "sich_sponsor");

            migrationBuilder.DropIndex(
                name: "IX_sich_sponsor_user_id",
                table: "sich_sponsor");

            migrationBuilder.DropColumn(
                name: "sich_sponsor_id",
                table: "sich_sponsor");

            migrationBuilder.RenameTable(
                name: "sponsor_rank",
                newName: "sponsor_ranks");

            migrationBuilder.RenameTable(
                name: "sich_sponsor",
                newName: "sich_sponsors");

            migrationBuilder.RenameColumn(
                name: "sponsor_rank_id",
                table: "sponsor_ranks",
                newName: "sponsor_ranks_id");

            migrationBuilder.RenameColumn(
                name: "color",
                table: "sponsor_ranks",
                newName: "default_color");

            migrationBuilder.RenameColumn(
                name: "sponsor_rank_id",
                table: "sich_sponsors",
                newName: "selected_ooc_rank_id");

            migrationBuilder.RenameIndex(
                name: "IX_sich_sponsor_sponsor_rank_id",
                table: "sich_sponsors",
                newName: "IX_sich_sponsors_selected_ooc_rank_id");

            migrationBuilder.AddColumn<bool>(
                name: "can_set_ghost_color",
                table: "sponsor_ranks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "can_set_ooc_color",
                table: "sponsor_ranks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "default_ghost_color",
                table: "sponsor_ranks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "default_ooc_color",
                table: "sponsor_ranks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "priority",
                table: "sponsor_ranks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "show_in_sponsor_window",
                table: "sponsor_ranks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "selected_ghost_color",
                table: "sich_sponsors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "selected_ghost_rank_id",
                table: "sich_sponsors",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "selected_ooc_color",
                table: "sich_sponsors",
                type: "text",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_sponsor_ranks",
                table: "sponsor_ranks",
                column: "sponsor_ranks_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_sich_sponsors",
                table: "sich_sponsors",
                column: "user_id");

            migrationBuilder.CreateTable(
                name: "rank_tags",
                columns: table => new
                {
                    rank_tags_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sponsor_rank_id = table.Column<int>(type: "integer", nullable: false),
                    tag_value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rank_tags", x => x.rank_tags_id);
                    table.ForeignKey(
                        name: "FK_rank_tags_sponsor_ranks_sponsor_rank_id",
                        column: x => x.sponsor_rank_id,
                        principalTable: "sponsor_ranks",
                        principalColumn: "sponsor_ranks_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sponsor_role_assignments",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rank_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sponsor_role_assignments", x => new { x.user_id, x.rank_id });
                    table.ForeignKey(
                        name: "FK_sponsor_role_assignments_sich_sponsors_sponsor_user_id",
                        column: x => x.user_id,
                        principalTable: "sich_sponsors",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_sponsor_role_assignments_sponsor_ranks_rank_id",
                        column: x => x.rank_id,
                        principalTable: "sponsor_ranks",
                        principalColumn: "sponsor_ranks_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sich_sponsors_selected_ghost_rank_id",
                table: "sich_sponsors",
                column: "selected_ghost_rank_id");

            migrationBuilder.CreateIndex(
                name: "IX_rank_tags_sponsor_rank_id",
                table: "rank_tags",
                column: "sponsor_rank_id");

            migrationBuilder.CreateIndex(
                name: "IX_sponsor_role_assignments_rank_id",
                table: "sponsor_role_assignments",
                column: "rank_id");

            migrationBuilder.AddForeignKey(
                name: "FK_sich_sponsors_sponsor_ranks_selected_ghost_rank_id",
                table: "sich_sponsors",
                column: "selected_ghost_rank_id",
                principalTable: "sponsor_ranks",
                principalColumn: "sponsor_ranks_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_sich_sponsors_sponsor_ranks_selected_ooc_rank_id",
                table: "sich_sponsors",
                column: "selected_ooc_rank_id",
                principalTable: "sponsor_ranks",
                principalColumn: "sponsor_ranks_id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_sich_sponsors_sponsor_ranks_selected_ghost_rank_id",
                table: "sich_sponsors");

            migrationBuilder.DropForeignKey(
                name: "FK_sich_sponsors_sponsor_ranks_selected_ooc_rank_id",
                table: "sich_sponsors");

            migrationBuilder.DropTable(
                name: "rank_tags");

            migrationBuilder.DropTable(
                name: "sponsor_role_assignments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_sponsor_ranks",
                table: "sponsor_ranks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_sich_sponsors",
                table: "sich_sponsors");

            migrationBuilder.DropIndex(
                name: "IX_sich_sponsors_selected_ghost_rank_id",
                table: "sich_sponsors");

            migrationBuilder.DropColumn(
                name: "can_set_ghost_color",
                table: "sponsor_ranks");

            migrationBuilder.DropColumn(
                name: "can_set_ooc_color",
                table: "sponsor_ranks");

            migrationBuilder.DropColumn(
                name: "default_ghost_color",
                table: "sponsor_ranks");

            migrationBuilder.DropColumn(
                name: "default_ooc_color",
                table: "sponsor_ranks");

            migrationBuilder.DropColumn(
                name: "priority",
                table: "sponsor_ranks");

            migrationBuilder.DropColumn(
                name: "show_in_sponsor_window",
                table: "sponsor_ranks");

            migrationBuilder.DropColumn(
                name: "selected_ghost_color",
                table: "sich_sponsors");

            migrationBuilder.DropColumn(
                name: "selected_ghost_rank_id",
                table: "sich_sponsors");

            migrationBuilder.DropColumn(
                name: "selected_ooc_color",
                table: "sich_sponsors");

            migrationBuilder.RenameTable(
                name: "sponsor_ranks",
                newName: "sponsor_rank");

            migrationBuilder.RenameTable(
                name: "sich_sponsors",
                newName: "sich_sponsor");

            migrationBuilder.RenameColumn(
                name: "sponsor_ranks_id",
                table: "sponsor_rank",
                newName: "sponsor_rank_id");

            migrationBuilder.RenameColumn(
                name: "default_color",
                table: "sponsor_rank",
                newName: "color");

            migrationBuilder.RenameColumn(
                name: "selected_ooc_rank_id",
                table: "sich_sponsor",
                newName: "sponsor_rank_id");

            migrationBuilder.RenameIndex(
                name: "IX_sich_sponsors_selected_ooc_rank_id",
                table: "sich_sponsor",
                newName: "IX_sich_sponsor_sponsor_rank_id");

            migrationBuilder.AddColumn<int>(
                name: "sich_sponsor_id",
                table: "sich_sponsor",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_sponsor_rank",
                table: "sponsor_rank",
                column: "sponsor_rank_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_sich_sponsor",
                table: "sich_sponsor",
                column: "sich_sponsor_id");

            migrationBuilder.CreateIndex(
                name: "IX_sich_sponsor_user_id",
                table: "sich_sponsor",
                column: "user_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_sich_sponsor_sponsor_rank_sponsor_rank_id",
                table: "sich_sponsor",
                column: "sponsor_rank_id",
                principalTable: "sponsor_rank",
                principalColumn: "sponsor_rank_id");
        }
    }
}
