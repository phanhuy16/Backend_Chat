
using Core.DTOs;
using Core.DTOs.Users;
using Core.DTOs.Messages;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services
{
    public class PollService : IPollService
    {
        private readonly ChatAppDbContext _context;

        public PollService(ChatAppDbContext context)
        {
            _context = context;
        }

        public async Task<MessageDto> CreatePollAsync(int userId, CreatePollDto createPollDto)
        {
            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == createPollDto.ConversationId);

            if (conversation == null)
            {
                throw new Exception("Conversation not found");
            }

            // Create Poll entity
            var poll = new Poll
            {
                Question = createPollDto.Question,
                AllowMultipleVotes = createPollDto.AllowMultipleVotes,
                EndsAt = createPollDto.EndsAt,
                ConversationId = createPollDto.ConversationId,
                CreatorId = userId,
                CreatedAt = DateTime.UtcNow
            };

            // Add Options
            foreach (var optionText in createPollDto.Options)
            {
                poll.Options.Add(new PollOption
                {
                    Text = optionText
                });
            }

            _context.Polls.Add(poll);
            await _context.SaveChangesAsync();
            
            // Create a system message or a special message type to indicate poll creation
            // We set MessageType to Poll (4)
            var message = new Message
            {
                ConversationId = createPollDto.ConversationId,
                SenderId = userId,
                MessageType = MessageType.Poll,
                Content = "Created a poll: " + poll.Question,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                PollId = poll.Id
            };
            
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Fetch the message with all necessary includes (Poll, Options, Votes, Sender)
            // We can use the already existing MessageRepository via a service or just fetch manually.
            // But look, PollService doesn't have MessageService. 
            // We'll fetch it here to avoid circular dependency if we were to add IMessageService.
            
            var savedMessage = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Poll)
                    .ThenInclude(p => p!.Options)
                        .ThenInclude(o => o.Votes)
                            .ThenInclude(v => v.User)
                .FirstOrDefaultAsync(m => m.Id == message.Id);

            // Map manually or use a mapper. For now, let's keep it simple.
            // Since we need MessageDto, and MessageDto mapping is complex, 
            // maybe we SHOULD add IMessageService or a shared mapper.
            // Given the complexity of MapToMessageDto in MessageService, I'll fetch and return a simple mapping or 
            // better: Add IMessageService (as long as it doesn't depend on IPollService, which it doesn't).
            
            // Wait, MessageService DOES NOT depend on IPollService. So we can add it here.
            
            // Actually, let's just use the mapper logic if we can.
            // For now, I'll return the PollDto inside a MessageDto shell if it's easier, 
            // but the controller needs the whole thing.
            
            // Let's just fetch it the same way MessageRepository does.
            return new MessageDto
            {
                Id = savedMessage!.Id,
                ConversationId = savedMessage.ConversationId,
                SenderId = savedMessage.SenderId,
                Sender = new UserDto
                {
                    Id = savedMessage.Sender.Id,
                    UserName = savedMessage.Sender.UserName!,
                    DisplayName = savedMessage.Sender.DisplayName,
                    Avatar = savedMessage.Sender.Avatar
                },
                Content = savedMessage.Content,
                MessageType = savedMessage.MessageType,
                CreatedAt = savedMessage.CreatedAt,
                PollId = savedMessage.PollId,
                Poll = await GetPollAsync(savedMessage.PollId!.Value, userId)
            };
        }

        public async Task<PollDto> VoteAsync(int userId, int pollId, int optionId)
        {
            var poll = await _context.Polls
                .Include(p => p.Options)
                .ThenInclude(o => o.Votes)
                .FirstOrDefaultAsync(p => p.Id == pollId);

            if (poll == null) throw new Exception("Poll not found");

            if (poll.EndsAt.HasValue && poll.EndsAt < DateTime.UtcNow)
            {
                 throw new Exception("Poll has ended");
            }

            var option = poll.Options.FirstOrDefault(o => o.Id == optionId);
            if (option == null) throw new Exception("Option not found");

            var existingVotes = poll.Options.SelectMany(o => o.Votes).Where(v => v.UserId == userId).ToList();

            if (!poll.AllowMultipleVotes && existingVotes.Any())
            {
                // Remove previous vote if multiple votes not allowed (switch vote)
                // Or throw error? Usually UI handles this, but backend should enforce. 
                // Let's toggle behavior: if voting for same option -> remove. If different -> switch.
                
                // For simplicity, if not allowed multiple, remove all other votes
                _context.PollVotes.RemoveRange(existingVotes);
            }
            
            // Toggle vote on the specific option
            var existingVoteForOption = existingVotes.FirstOrDefault(v => v.PollOptionId == optionId);
            if (existingVoteForOption != null)
            {
                _context.PollVotes.Remove(existingVoteForOption);
            }
            else
            {
                 _context.PollVotes.Add(new PollVote
                 {
                     PollOptionId = optionId,
                     UserId = userId,
                     VotedAt = DateTime.UtcNow
                 });
            }

            await _context.SaveChangesAsync();
            return await GetPollAsync(pollId, userId);
        }
        
        public async Task<PollDto> RemoveVoteAsync(int userId, int pollId, int optionId)
        {
             // This logic can be handled inside VoteAsync as toggle, but generic Remove function is good.
             var vote = await _context.PollVotes
                 .FirstOrDefaultAsync(v => v.PollOptionId == optionId && v.UserId == userId);
                 
             if (vote != null)
             {
                 _context.PollVotes.Remove(vote);
                 await _context.SaveChangesAsync();
             }
             
             return await GetPollAsync(pollId, userId);
        }

        public async Task<PollDto> GetPollAsync(int pollId, int currentUserId)
        {
            var poll = await _context.Polls
                .Include(p => p.Creator)
                .Include(p => p.Options)
                .ThenInclude(o => o.Votes)
                .ThenInclude(v => v.User)
                .FirstOrDefaultAsync(p => p.Id == pollId);

            if (poll == null) throw new Exception("Poll not found");

            var dto = new PollDto
            {
                Id = poll.Id,
                Question = poll.Question,
                AllowMultipleVotes = poll.AllowMultipleVotes,
                ConversationId = poll.ConversationId,
                CreatorId = poll.CreatorId,
                CreatorName = poll.Creator != null ? poll.Creator.DisplayName : "Unknown",
                CreatedAt = poll.CreatedAt,
                EndsAt = poll.EndsAt,
                TotalVotes = poll.Options.Sum(o => o.Votes.Count),
                HasVoted = poll.Options.Any(o => o.Votes.Any(v => v.UserId == currentUserId)),
                Options = poll.Options.Select(o => new PollOptionDto
                {
                    Id = o.Id,
                    Text = o.Text,
                    VoteCount = o.Votes.Count,
                    IsVotedByCurrentUser = o.Votes.Any(v => v.UserId == currentUserId),
                    Votes = o.Votes.Select(v => new PollVoteDto
                    {
                        UserId = v.UserId,
                        Username = v.User.DisplayName,
                        AvatarUrl = v.User.Avatar
                    }).ToList()
                }).ToList()
            };

            return dto;
        }
    }
}
