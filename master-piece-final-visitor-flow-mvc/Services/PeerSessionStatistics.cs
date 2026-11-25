using SkillsSwap.Models;

namespace SkillsSwap.Services;

internal static class PeerSessionStatistics
{
    internal static IQueryable<LearningSession> VerifiedPeerCompletions(this IQueryable<LearningSession> sessions) =>
        sessions.Where(s => !s.LearningRequest.IsExpertBooking && s.Status == SessionStatus.Completed &&
            s.CompletedAtUtc != null && s.LearnerCompletedAtUtc != null && s.TeacherCompletedAtUtc != null);

    internal static IQueryable<Review> ValidPeerReviews(this IQueryable<Review> reviews) =>
        reviews.Where(r => !r.LearningSession.LearningRequest.IsExpertBooking &&
            r.LearningSession.Status == SessionStatus.Completed && r.LearningSession.CompletedAtUtc != null &&
            r.LearningSession.LearnerCompletedAtUtc != null && r.LearningSession.TeacherCompletedAtUtc != null &&
            r.ReviewerId == r.LearningSession.LearningRequest.LearnerId &&
            r.ReviewedUserId == r.LearningSession.LearningRequest.TeacherId && r.Rating >= 1 && r.Rating <= 5);
}
