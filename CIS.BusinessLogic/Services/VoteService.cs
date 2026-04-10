using CIS.BusinessLogic.dtos;
using CIS.BusinessLogic.Exceptions;
using CIS.DataAcces.Data;
using CIS.DataAcces.Models;
using Microsoft.EntityFrameworkCore;

namespace CIS.BusinessLogic.Services;

public class VoteService : IVoteService
{
    private readonly CisDbContext _context;

    public VoteService(CisDbContext context)
    {
        _context = context;
    }

    public async Task<VoteResponse> AddVoteAsync(string ideaId, string userId)
    {
        var idea = await _context.Ideas
            .FirstOrDefaultAsync(i => i.Id == ideaId);

        if (idea == null)
            throw new NotFoundException($"Idea with id '{ideaId}' not found.");

        var alreadyVoted = await _context.Votes
            .AnyAsync(v => v.IdeaId == ideaId && v.UserId == userId);

        if (alreadyVoted)
            throw new ConflictException("User has already voted on this idea.");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var vote = new Vote
            {
                IdeaId = ideaId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Votes.Add(vote);
            idea.VoteCount += 1;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new VoteResponse
            {
                Id = vote.Id,
                IdeaId = vote.IdeaId,
                UserId = vote.UserId!,
                CreatedAt = vote.CreatedAt,
                Links = new object[]
                {
                    new { rel = "self",   href = $"/api/v1/ideas/{ideaId}/votes" },
                    new { rel = "idea",   href = $"/api/v1/topics/{idea.TopicId}/ideas/{ideaId}" },
                    new { rel = "unvote", href = $"/api/v1/ideas/{ideaId}/votes" }
                }
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task RemoveVoteAsync(string ideaId, string userId)
    {
        var idea = await _context.Ideas
            .FirstOrDefaultAsync(i => i.Id == ideaId);

        if (idea == null)
            throw new NotFoundException($"Idea with id '{ideaId}' not found.");

        var vote = await _context.Votes
            .FirstOrDefaultAsync(v => v.IdeaId == ideaId && v.UserId == userId);

        if (vote == null)
            throw new NotFoundException("Vote not found for this user on this idea.");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.Votes.Remove(vote);
            idea.VoteCount = Math.Max(0, idea.VoteCount - 1);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
