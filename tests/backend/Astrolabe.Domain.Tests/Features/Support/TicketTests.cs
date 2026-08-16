using Astrolabe.Domain.Features.Support.Entities;
using Astrolabe.Domain.Features.Support.Enums;
using Astrolabe.Domain.Features.Support.Errors;
using Astrolabe.Domain.Features.Support.Events;
using FluentAssertions;

namespace Astrolabe.Domain.Tests.Features.Support;

/// <summary>
/// Covers the ticket lifecycle: BR-SUP-002, BR-SUP-003, BR-SUP-005 to BR-SUP-008, BR-SUP-011 and
/// BR-SUP-012.
/// </summary>
[TestFixture]
public sealed class TicketTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 16, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid MemberId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid AgentId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Library = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static Ticket ATicket() =>
        Ticket.Open(
            "TCK-2038", MemberId, TicketCategory.PaymentsAndFines, Library,
            "The fine was charged twice", "I paid on the 28th and it shows again.",
            "Francisco Torreglosa", Now).Value;

    private static Ticket AnAssignedTicket()
    {
        var ticket = ATicket();
        ticket.Assign(AgentId, "Marcus Reed", Now);
        ticket.ClearDomainEvents();
        return ticket;
    }

    private static Ticket AResolvedTicket()
    {
        var ticket = AnAssignedTicket();
        ticket.Resolve(Now.AddHours(2));
        return ticket;
    }

    // ---------- Opening ----------

    [Test]
    public void OpeningStartsInCreatedWithTheFirstMessage()
    {
        var ticket = ATicket();

        ticket.Status.Should().Be(TicketStatus.Created);
        ticket.Messages.Should().ContainSingle()
            .Which.Author.Should().Be(TicketAuthor.Member);
    }

    [TestCase("")]
    [TestCase("   ")]
    public void ATicketWithoutASubjectIsRefused(string subject)
    {
        Ticket.Open("TCK-1", MemberId, TicketCategory.AccountAndPlan, Library,
                subject, "Body", "Ada", Now)
            .Error.Should().Be(SupportErrors.SubjectRequired);
    }

    [Test]
    public void ATicketWithoutABodyIsRefused()
    {
        // A title and no question is something somebody has to chase before they can answer it.
        Ticket.Open("TCK-1", MemberId, TicketCategory.AccountAndPlan, Library,
                "Subject", "   ", "Ada", Now)
            .Error.Should().Be(SupportErrors.MessageRequired);
    }

    // ---------- BR-SUP-003: assigning moves it into review ----------

    [Test]
    public void AssigningMovesTheTicketIntoReview()
    {
        // One act, not two. A ticket in review with nobody on it is one everybody assumes somebody
        // else has picked up.
        var ticket = ATicket();

        ticket.Assign(AgentId, "Marcus Reed", Now).IsSuccess.Should().BeTrue();

        ticket.Status.Should().Be(TicketStatus.InReview);
        ticket.AgentName.Should().Be("Marcus Reed");
    }

    [Test]
    public void AnUnassignedTicketCannotBeResolved()
    {
        // Read the other way round: a ticket nobody handled cannot have been resolved by anybody,
        // and closing it would lose who to ask when it comes back.
        ATicket().Resolve(Now).Error.Should().Be(SupportErrors.AgentRequired);
    }

    // ---------- BR-SUP-011: a resolved ticket admits nothing ----------

    [Test]
    public void AResolvedTicketRefusesAReply()
    {
        AResolvedTicket()
            .Reply(MemberId, TicketAuthor.Member, "Francisco", "One more thing", Now.AddHours(3))
            .Error.Should().Be(SupportErrors.TicketIsResolved);
    }

    [Test]
    public void AResolvedTicketRefusesAnAgentReplyToo()
    {
        // Both sides. Letting staff carry on quietly would put a ticket back in a queue somebody had
        // already finished with, without anyone deciding to.
        AResolvedTicket()
            .Reply(AgentId, TicketAuthor.Agent, "Marcus", "Anything else?", Now.AddHours(3))
            .Error.Should().Be(SupportErrors.TicketIsResolved);
    }

    [Test]
    public void ReopeningAdmitsMessagesAgain()
    {
        var ticket = AResolvedTicket();
        ticket.Reopen(Now.AddHours(4));

        ticket.Reply(MemberId, TicketAuthor.Member, "Francisco", "Still wrong", Now.AddHours(5))
            .IsSuccess.Should().BeTrue();
        ticket.Status.Should().Be(TicketStatus.InReview);
    }

    [Test]
    public void OnlyAResolvedTicketCanBeReopened()
    {
        AnAssignedTicket().Reopen(Now).Error.Should().Be(SupportErrors.TicketNotReopenable);
    }

    // ---------- BR-SUP-005 to BR-SUP-007: the rating ----------

    [Test]
    public void ATicketCannotBeRatedBeforeItIsResolved()
    {
        AnAssignedTicket().Rate(5, "Great", Now).Error.Should().Be(SupportErrors.TicketNotResolved);
    }

    [TestCase(0)]
    [TestCase(6)]
    [TestCase(-1)]
    public void ARatingOutsideOneToFiveIsRefused(int stars)
    {
        AResolvedTicket().Rate(stars, null, Now)
            .Error.Should().Be(SupportErrors.RatingOutOfRange);
    }

    [Test]
    public void AReviewIsOptional()
    {
        var ticket = AResolvedTicket();

        ticket.Rate(4, null, Now).IsSuccess.Should().BeTrue();
        ticket.Rating.Should().Be(4);
        ticket.Review.Should().BeNull();
    }

    [Test]
    public void ReopeningClearsTheRating()
    {
        // BR-SUP-007. The rating answers "did we help", and reopening says the answer was no.
        // Keeping five stars on a reopened ticket would report satisfaction that was withdrawn.
        var ticket = AResolvedTicket();
        ticket.Rate(5, "Marcus fixed it in minutes.", Now);

        ticket.Reopen(Now.AddHours(1));

        ticket.Rating.Should().BeNull();
        ticket.Review.Should().BeNull();
    }

    [Test]
    public void ATicketCanBeRatedAgainAfterASecondResolution()
    {
        // Not a second rating of the first resolution — the first was cleared, so this is the first
        // rating of the second one.
        var ticket = AResolvedTicket();
        ticket.Rate(2, "Not fixed", Now);
        ticket.Reopen(Now.AddHours(1));
        ticket.Resolve(Now.AddHours(2));

        ticket.Rate(5, "Fixed properly", Now.AddHours(3)).IsSuccess.Should().BeTrue();
        ticket.Rating.Should().Be(5);
    }

    // ---------- BR-SUP-008 and BR-SUP-012 ----------

    [Test]
    public void AMessageRecordsWhoWroteItAndWhen()
    {
        var ticket = AnAssignedTicket();
        ticket.Reply(AgentId, TicketAuthor.Agent, "Marcus Reed", "Looking into it", Now.AddHours(1));

        var last = ticket.Messages[^1];
        last.AuthorName.Should().Be("Marcus Reed");
        last.Author.Should().Be(TicketAuthor.Agent);
        last.WrittenAt.Should().Be(Now.AddHours(1));
    }

    [Test]
    public void AnAgentReplyRaisesTheEventThatNotifiesTheMember()
    {
        var ticket = AnAssignedTicket();

        ticket.Reply(AgentId, TicketAuthor.Agent, "Marcus", "On it", Now.AddHours(1));

        ticket.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TicketAnswered>();
    }

    [Test]
    public void AMemberReplyNotifiesNobody()
    {
        // A member is not told about their own words. The event exists to reach somebody who is not
        // looking at the screen.
        var ticket = AnAssignedTicket();

        ticket.Reply(MemberId, TicketAuthor.Member, "Francisco", "Any news?", Now.AddHours(1));

        ticket.DomainEvents.Should().BeEmpty();
    }

    [Test]
    public void AnEmptyMessageIsRefused()
    {
        AnAssignedTicket()
            .Reply(MemberId, TicketAuthor.Member, "Francisco", "   ", Now)
            .Error.Should().Be(SupportErrors.MessageRequired);
    }
}
