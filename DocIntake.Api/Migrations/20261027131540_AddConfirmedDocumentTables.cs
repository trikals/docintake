using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DocIntake.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddConfirmedDocumentTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AchReturnNotices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DocumentId = table.Column<int>(type: "integer", nullable: false),
                    OriginatorName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ReceiverName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RdfiName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SecCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    OriginalEffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ReturnDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TraceNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OriginalTraceNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ReturnReasonCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ReturnReasonDescription = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchReturnNotices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AchReturnNotices_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BankStatements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DocumentId = table.Column<int>(type: "integer", nullable: false),
                    InstitutionName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AccountHolder = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AccountType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AccountNumberMasked = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    OpeningBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ClosingBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankStatements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankStatements_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DocumentId = table.Column<int>(type: "integer", nullable: false),
                    VendorName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    InvoiceNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    InvoiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    BillTo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Tax = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UtilityBills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DocumentId = table.Column<int>(type: "integer", nullable: false),
                    ProviderName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UtilityType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AccountNumberMasked = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ServiceAddress = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    StatementDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    UsageQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UsageUnit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PreviousBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentsReceived = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrentCharges = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AmountDue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UtilityBills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UtilityBills_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BankTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BankStatementId = table.Column<int>(type: "integer", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    TransactionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Direction = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankTransactions_BankStatements_BankStatementId",
                        column: x => x.BankStatementId,
                        principalTable: "BankStatements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InvoiceId = table.Column<int>(type: "integer", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceLines_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UtilityCharges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UtilityBillId = table.Column<int>(type: "integer", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UtilityCharges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UtilityCharges_UtilityBills_UtilityBillId",
                        column: x => x.UtilityBillId,
                        principalTable: "UtilityBills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AchReturnNotices_DocumentId",
                table: "AchReturnNotices",
                column: "DocumentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AchReturnNotices_ReturnDate",
                table: "AchReturnNotices",
                column: "ReturnDate");

            migrationBuilder.CreateIndex(
                name: "IX_BankStatements_DocumentId",
                table: "BankStatements",
                column: "DocumentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankStatements_InstitutionName_AccountHolder_AccountType_Ac~",
                table: "BankStatements",
                columns: new[] { "InstitutionName", "AccountHolder", "AccountType", "AccountNumberMasked" });

            migrationBuilder.CreateIndex(
                name: "IX_BankStatements_PeriodEnd",
                table: "BankStatements",
                column: "PeriodEnd");

            migrationBuilder.CreateIndex(
                name: "IX_BankTransactions_BankStatementId_LineNumber",
                table: "BankTransactions",
                columns: new[] { "BankStatementId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_InvoiceId_LineNumber",
                table: "InvoiceLines",
                columns: new[] { "InvoiceId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_DocumentId",
                table: "Invoices",
                column: "DocumentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceDate",
                table: "Invoices",
                column: "InvoiceDate");

            migrationBuilder.CreateIndex(
                name: "IX_UtilityBills_DocumentId",
                table: "UtilityBills",
                column: "DocumentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UtilityBills_StatementDate",
                table: "UtilityBills",
                column: "StatementDate");

            migrationBuilder.CreateIndex(
                name: "IX_UtilityCharges_UtilityBillId_LineNumber",
                table: "UtilityCharges",
                columns: new[] { "UtilityBillId", "LineNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AchReturnNotices");

            migrationBuilder.DropTable(
                name: "BankTransactions");

            migrationBuilder.DropTable(
                name: "InvoiceLines");

            migrationBuilder.DropTable(
                name: "UtilityCharges");

            migrationBuilder.DropTable(
                name: "BankStatements");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "UtilityBills");
        }
    }
}
