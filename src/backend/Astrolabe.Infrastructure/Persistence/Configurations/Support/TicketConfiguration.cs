using Astrolabe.Domain.Features.Support.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Astrolabe.Infrastructure.Persistence.Configurations.Support;

public sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("tickets");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Reference).HasMaxLength(16).IsRequired();
        builder.Property(t => t.MemberId).IsRequired();
        builder.Property(t => t.Category).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(t => t.LibraryId).IsRequired();
        builder.Property(t => t.Subject).HasMaxLength(Ticket.MaxSubjectLength).IsRequired();
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(t => t.AgentUserId);
        builder.Property(t => t.AgentName).HasMaxLength(160);
        builder.Property(t => t.Rating);
        builder.Property(t => t.Review).HasMaxLength(Ticket.MaxReviewLength);
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UpdatedAt).IsRequired();

        builder.Ignore(t => t.DomainEvents);

        // Owned, not a table anybody joins to on its own: a message has no life without its ticket
        // and the transition rules read the list to decide.
        builder.OwnsMany(t => t.Messages, message =>
        {
            message.ToTable("ticket_messages");
            message.WithOwner().HasForeignKey("ticket_id");
            message.HasKey(m => m.Id);
            message.Property(m => m.AuthorUserId).IsRequired();
            message.Property(m => m.Author).HasConversion<string>().HasMaxLength(16).IsRequired();
            message.Property(m => m.AuthorName).HasMaxLength(160).IsRequired();
            message.Property(m => m.Text).HasMaxLength(TicketMessage.MaxTextLength).IsRequired();
            message.Property(m => m.WrittenAt).IsRequired();
        });

        // The reference is what a member quotes, so it is unique and indexed for lookup by it.
        builder.HasIndex(t => t.Reference).IsUnique();

        // The two queues: a member's own, and a library's.
        builder.HasIndex(t => new { t.MemberId, t.UpdatedAt });
        builder.HasIndex(t => new { t.LibraryId, t.Status });

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();
    }
}
