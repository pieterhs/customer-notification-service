using Microsoft.EntityFrameworkCore.Migrations;

namespace CustomerNotificationService.Infrastructure.Migrations
{
    public partial class RemoveLegacyTemplates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop legacy Templates table if it exists to align DB schema with current model
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"Templates\";");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recreate Templates table (best-effort) to support down migration
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS ""Templates"" (
                ""Id"" uuid NOT NULL,
                ""Key"" text NOT NULL,
                ""Subject"" text NOT NULL,
                ""Body"" text NOT NULL,
                ""CreatedAt"" timestamp with time zone NOT NULL,
                ""UpdatedAt"" timestamp with time zone NULL,
                CONSTRAINT ""PK_Templates"" PRIMARY KEY (""Id"")
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Templates_Key"" ON ""Templates"" (""Key"");
            ");
        }
    }
}
