namespace SkillsSwap.Models;

public sealed record ChatConversation(long Id, string Skill, string Partner, string? ProfileImage,
    string? LastBody, DateTime? LastAt, int Unread);

public sealed record LearningChatPage(IReadOnlyList<ChatConversation> Conversations, long? SelectedId, bool CanSend);

public sealed record ChatMessage(long Id, string Body, DateTime SentAt, bool Mine, bool Unread)
{
    public string Utc => DateTime.SpecifyKind(SentAt, DateTimeKind.Utc).ToString("O");
}
