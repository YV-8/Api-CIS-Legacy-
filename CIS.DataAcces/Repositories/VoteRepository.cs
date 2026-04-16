using CIS.BusinessLogic.Exceptions;
using CIS.BusinessLogic.Persistence;
using CIS.DataAcces.Data;
using CIS.DataAcces.Models;
using Microsoft.EntityFrameworkCore;

namespace CIS.DataAcces.Repositories;

public class VoteRepository : IVoteRepository
{
    private readonly CisDbContext _context;

    public VoteRepository(CisDbContext context)
    {
        _context = context;
    }

    public async Task<VoteCreationResult> AddVoteAsync(string ideaId, string? userId, CancellationToken cancellationToken = default)
    {
        var idea = await _context.Ideas
            .Include(i => i.Topic)
            .FirstOrDefaultAsync(i => i.Id == ideaId && i.DeletedAt == null, cancellationToken);

        if (idea == null)
            throw new NotFoundException($"Idea with id '{ideaId}' not found.");

        if (string.IsNullOrWhiteSpace(userId) && !idea.Topic.AnonymousVote)
            throw new AuthenticationRequiredException("Authentication is required to vote on this idea.");

        if (!string.IsNullOrWhiteSpace(userId))
        {
            var alreadyVoted = await _context.Votes
                .AnyAsync(v => v.IdeaId == ideaId && v.UserId == userId, cancellationToken);

            if (alreadyVoted)
                throw new ConflictException("User has already voted on this idea.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var vote = new Vote
            {
                IdeaId = ideaId,
                UserId = idea.Topic.AnonymousVote ? null : userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Votes.Add(vote);
            idea.VoteCount += 1;

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new VoteCreationResult
            {
                VoteId = vote.Id,
                IdeaId = vote.IdeaId,
                UserId = vote.UserId,
                CreatedAt = vote.CreatedAt,
                TopicId = idea.TopicId
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public Task<bool> HasUserVotedAsync(string ideaId, string userId, CancellationToken cancellationToken = default)
    {
        return _context.Votes
            .AnyAsync(v => v.IdeaId == ideaId && v.UserId == userId, cancellationToken);
    }

    public async Task RemoveVoteAsync(string ideaId, string userId, CancellationToken cancellationToken = default)
    {
        var idea = await _context.Ideas
            .FirstOrDefaultAsync(i => i.Id == ideaId && i.DeletedAt == null, cancellationToken);

        if (idea == null)
            throw new NotFoundException($"Idea with id '{ideaId}' not found.");

        var vote = await _context.Votes
            .FirstOrDefaultAsync(v => v.IdeaId == ideaId && v.UserId == userId, cancellationToken);

        if (vote == null)
            throw new NotFoundException("Vote not found for this user on this idea.");

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            _context.Votes.Remove(vote);
            idea.VoteCount = Math.Max(0, idea.VoteCount - 1);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
