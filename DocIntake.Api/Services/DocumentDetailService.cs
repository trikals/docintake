using System.Text.Json;
using DocIntake.Api.Data;
using DocIntake.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DocIntake.Api.Services;

/// <summary>Assembles a document with its latest extraction and, once confirmed, its confirmed data.</summary>
public class DocumentDetailService(AppDbContext db, ICurrentUser user)
{
    public async Task<DocumentDetailResponse?> GetAsync(int documentId, CancellationToken ct)
    {
        var document = await db.Documents.AsNoTracking().OwnedBy(user.Id).FirstOrDefaultAsync(d => d.Id == documentId && !d.IsArchived, ct);
        if (document is null)
            return null;

        var latest = await db.Extractions.AsNoTracking()
            .Where(e => e.DocumentId == documentId)
            .OrderByDescending(e => e.CreatedAt).ThenByDescending(e => e.Id)
            .FirstOrDefaultAsync(ct);

        JsonElement? confirmed = null;
        if (document.Status == DocumentStatus.Confirmed && document.DocumentType is { } type)
        {
            if (type == DocumentType.Invoice)
            {
                var invoice = await db.Invoices.AsNoTracking().Include(i => i.Lines).FirstOrDefaultAsync(i => i.DocumentId == documentId, ct);
                if (invoice is not null) confirmed = ConfirmedDataMapper.ToJson(invoice);
            }
            else if (type == DocumentType.BankStatement)
            {
                var statement = await db.BankStatements.AsNoTracking().Include(b => b.Transactions).FirstOrDefaultAsync(b => b.DocumentId == documentId, ct);
                if (statement is not null) confirmed = ConfirmedDataMapper.ToJson(statement);
            }
            else if (type == DocumentType.AchReturnNotice)
            {
                var notice = await db.AchReturnNotices.AsNoTracking().FirstOrDefaultAsync(a => a.DocumentId == documentId, ct);
                if (notice is not null) confirmed = ConfirmedDataMapper.ToJson(notice);
            }
            else if (type == DocumentType.UtilityBill)
            {
                var bill = await db.UtilityBills.AsNoTracking().Include(u => u.Charges).FirstOrDefaultAsync(u => u.DocumentId == documentId, ct);
                if (bill is not null) confirmed = ConfirmedDataMapper.ToJson(bill);
            }
        }

        return new DocumentDetailResponse(
            DocumentResponse.From(document),
            latest is null ? null : ExtractionResponse.From(latest, document.Status),
            confirmed,
            document.ConfirmedAt);
    }
}
