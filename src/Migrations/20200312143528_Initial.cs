using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace mummybot.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "blacklist",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<decimal>(nullable: false),
                    reason = table.Column<string>(nullable: true),
                    createdat = table.Column<DateTime>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blacklist", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "guilds",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildid = table.Column<decimal>(nullable: false),
                    guildname = table.Column<string>(nullable: false),
                    ownerid = table.Column<decimal>(nullable: false),
                    active = table.Column<bool>(nullable: false, defaultValue: true),
                    region = table.Column<string>(nullable: false),
                    greeting = table.Column<string>(nullable: false, defaultValue: "**%user% has joined**"),
                    goodbye = table.Column<string>(nullable: false, defaultValue: "**%user% has left**"),
                    greetchl = table.Column<decimal>(nullable: true),
                    filterinvites = table.Column<bool>(nullable: false),
                    regex = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guilds", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tags",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(maxLength: 12, nullable: false),
                    content = table.Column<string>(nullable: false),
                    author = table.Column<decimal>(nullable: false),
                    guild = table.Column<decimal>(nullable: false),
                    createdat = table.Column<DateTime>(nullable: true),
                    iscommand = table.Column<bool>(nullable: false),
                    uses = table.Column<int>(nullable: false),
                    lastusedby = table.Column<decimal>(nullable: true),
                    lastused = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<decimal>(nullable: false),
                    username = table.Column<string>(nullable: false),
                    nickname = table.Column<string>(nullable: true),
                    guildid = table.Column<decimal>(nullable: false),
                    avatar = table.Column<string>(nullable: true),
                    tagbanned = table.Column<bool>(nullable: false, defaultValueSql: "false"),
                    joined = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users_audit",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<decimal>(nullable: false),
                    username = table.Column<string>(nullable: true),
                    nickname = table.Column<string>(nullable: true),
                    guildid = table.Column<decimal>(nullable: false),
                    changedon = table.Column<DateTime>(nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users_audit", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "users_userid_key",
                schema: "public",
                table: "users",
                column: "userid",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "blacklist",
                schema: "public");

            migrationBuilder.DropTable(
                name: "guilds",
                schema: "public");

            migrationBuilder.DropTable(
                name: "tags",
                schema: "public");

            migrationBuilder.DropTable(
                name: "users",
                schema: "public");

            migrationBuilder.DropTable(
                name: "users_audit",
                schema: "public");
        }
    }
}
