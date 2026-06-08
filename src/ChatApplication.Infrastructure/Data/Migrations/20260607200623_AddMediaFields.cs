using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatApplication.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SentAt",
                table: "PrivateMessages",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<long>(
                name: "MediaBytes",
                table: "PrivateMessages",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MediaName",
                table: "PrivateMessages",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MediaPublicId",
                table: "PrivateMessages",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MediaType",
                table: "PrivateMessages",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MediaUrl",
                table: "PrivateMessages",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "SentAt",
                table: "Messages",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "EditedAt",
                table: "Messages",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<long>(
                name: "MediaBytes",
                table: "Messages",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MediaName",
                table: "Messages",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MediaPublicId",
                table: "Messages",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MediaType",
                table: "Messages",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MediaUrl",
                table: "Messages",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "ChatRooms",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "JoinedAt",
                table: "ChatRoomMembers",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MediaBytes",
                table: "PrivateMessages");

            migrationBuilder.DropColumn(
                name: "MediaName",
                table: "PrivateMessages");

            migrationBuilder.DropColumn(
                name: "MediaPublicId",
                table: "PrivateMessages");

            migrationBuilder.DropColumn(
                name: "MediaType",
                table: "PrivateMessages");

            migrationBuilder.DropColumn(
                name: "MediaUrl",
                table: "PrivateMessages");

            migrationBuilder.DropColumn(
                name: "MediaBytes",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "MediaName",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "MediaPublicId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "MediaType",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "MediaUrl",
                table: "Messages");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SentAt",
                table: "PrivateMessages",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SentAt",
                table: "Messages",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "EditedAt",
                table: "Messages",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "ChatRooms",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "JoinedAt",
                table: "ChatRoomMembers",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");
        }
    }
}
