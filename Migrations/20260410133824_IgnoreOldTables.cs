using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChoThueQuanAo.Migrations
{
    public partial class IgnoreOldTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Tui đã xóa hết các lệnh CreateTable ở đây 
            // để máy không cố gắng tạo lại các bảng đã có sẵn trong SQL Server nữa.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Tui cũng để trống phần này luôn để đảm bảo an toàn.
        }
    }
}