using Microsoft.EntityFrameworkCore;
using SkillsSwap.Data;
using SkillsSwap.Models;

namespace SkillsSwap.Services;

public sealed class SkillDiscoveryService(ApplicationDbContext db, TimeProvider? clock = null)
{
    public const int PageSize = 8;

    public async Task<SkillDiscoveryViewModel?> DiscoverAsync(
        string skillSlug, Guid currentUserId, SkillDiscoveryQuery query, CancellationToken cancellationToken = default)
    {
        var now = (clock ?? TimeProvider.System).GetUtcNow().UtcDateTime;
        var activePlus = PlusEntitlement.Active(db, now);
        var skill = await db.Skills.AsNoTracking()
            .Where(x => x.Slug == skillSlug && x.Status == PublicationStatus.Published &&
                        x.SubCategory.IsActive && x.SubCategory.Category.IsActive &&
                        (x.AccessTier == AccessTier.Free || activePlus.Any(s => s.UserId == currentUserId)))
            .Select(x => new
            {
                x.Id, Name = x.NameEn, x.Slug, x.AccessTier,
                CategoryName = x.SubCategory.Category.NameEn,
                CategorySlug = x.SubCategory.Category.Slug,
                SubCategoryName = x.SubCategory.NameEn,
                SubCategorySlug = x.SubCategory.Slug
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (skill is null) return null;

        var memberRoleId = await db.Roles.AsNoTracking()
            .Where(x => x.NormalizedName == "MEMBER").Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (!memberRoleId.HasValue)
            return CreateEmpty(skill.Id, skill.Name, skill.Slug, skill.CategoryName, skill.CategorySlug,
                skill.SubCategoryName, skill.SubCategorySlug, skill.AccessTier, query);

        var eligibleMemberIds = db.UserRoles.AsNoTracking()
            .Where(x => x.RoleId == memberRoleId.Value).Select(x => x.UserId);
        var adminRoleIds = db.Roles.AsNoTracking().Where(x => x.NormalizedName == "ADMIN").Select(x => x.Id);
        var administrativeUserIds = db.UserRoles.AsNoTracking()
            .Where(x => adminRoleIds.Contains(x.RoleId)).Select(x => x.UserId);

        var basePartners = db.UserSkills.AsNoTracking().Where(x =>
            x.SkillId == skill.Id && x.Intent == SkillIntent.Teach && x.UserId != currentUserId &&
            eligibleMemberIds.Contains(x.UserId) && !administrativeUserIds.Contains(x.UserId) &&
            x.User.Status == AccountStatus.Active &&
            x.User.EmailConfirmed && x.User.IsProfileComplete);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            basePartners = basePartners.Where(x => x.User.FullName.Contains(search));
        }
        if (query.Level.HasValue)
            basePartners = basePartners.Where(x => x.Level == query.Level.Value);

        var validReviews = db.Reviews.AsNoTracking().ValidPeerReviews();
        var completedSessions = db.LearningSessions.AsNoTracking().PeerCompletions();
        var partnerRows = basePartners.Select(x => new DiscoveryPersonViewModel
        {
            UserId = x.UserId,
            SkillId = x.SkillId,
            PendingPeerRequestId = db.LearningRequests.Where(r => r.LearnerId == currentUserId &&
                r.TeacherId == x.UserId && r.SkillId == x.SkillId && !r.IsExpertBooking &&
                r.EndedAtUtc == null && r.Status == RequestStatus.Pending).Select(r => (long?)r.Id).FirstOrDefault(),
            AcceptedRelationshipId = db.LearningRequests.Where(r => r.LearnerId == currentUserId &&
                r.TeacherId == x.UserId && r.SkillId == x.SkillId && !r.IsExpertBooking &&
                r.EndedAtUtc == null && r.Status == RequestStatus.Accepted).Select(r => (long?)r.Id).FirstOrDefault(),
            DisplayName = x.User.FullName,
            ProfileImageUrl = x.User.ProfileImageUrl,
            Biography = x.User.Bio,
            SkillName = x.Skill.NameEn,
            TeachingLevel = x.Level,
            Experience = x.Experience,
            AddedAtUtc = x.AddedAtUtc,
            ReviewCount = validReviews.Count(r => r.ReviewedUserId == x.UserId &&
                r.LearningSession.LearningRequest.SkillId == skill.Id),
            Rating = validReviews.Where(r => r.ReviewedUserId == x.UserId &&
                    r.LearningSession.LearningRequest.SkillId == skill.Id)
                .Select(r => (double?)r.Rating).Average(),
            CompletedTeachingSessions = completedSessions.Count(s => s.LearningRequest.TeacherId == x.UserId &&
                s.LearningRequest.SkillId == skill.Id)
        });

        partnerRows = query.Sort switch
        {
            DiscoverySort.Name => partnerRows.OrderBy(x => x.DisplayName).ThenBy(x => x.UserId),
            DiscoverySort.Rating => partnerRows.OrderByDescending(x => x.Rating.HasValue)
                .ThenByDescending(x => x.Rating).ThenBy(x => x.DisplayName),
            _ => partnerRows.OrderByDescending(x => x.AddedAtUtc).ThenBy(x => x.DisplayName)
        };

        var partnerCount = await partnerRows.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(partnerCount / (double)PageSize));
        var page = Math.Clamp(query.Page, 1, totalPages);
        var partners = await partnerRows.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync(cancellationToken);
        foreach (var partner in partners)
        {
            partner.Initials = NameFormatting.Initials(partner.DisplayName);
            partner.ProfileImageUrl = SafePublicImageUrl(partner.ProfileImageUrl);
        }

        var approvedExpertIds = db.ExpertApplications.AsNoTracking()
            .Where(x => x.SkillId == skill.Id && x.Status == ExpertApplicationStatus.Approved)
            .Select(x => x.UserId);
        var expertRows = basePartners.Where(x => approvedExpertIds.Contains(x.UserId))
            .Select(x => new DiscoveryPersonViewModel
            {
                UserId = x.UserId,
                DisplayName = x.User.FullName,
                ProfileImageUrl = x.User.ProfileImageUrl,
                Biography = x.User.Bio,
                SkillName = x.Skill.NameEn,
                TeachingLevel = x.Level,
                Experience = db.ExpertApplications.Where(a => a.UserId == x.UserId && a.SkillId == skill.Id &&
                    a.Status == ExpertApplicationStatus.Approved).Select(a => a.Experience).FirstOrDefault(),
                ReviewCount = validReviews.Count(r => r.ReviewedUserId == x.UserId &&
                    r.LearningSession.LearningRequest.SkillId == skill.Id),
                Rating = validReviews.Where(r => r.ReviewedUserId == x.UserId &&
                        r.LearningSession.LearningRequest.SkillId == skill.Id)
                    .Select(r => (double?)r.Rating).Average(),
                CompletedTeachingSessions = completedSessions.Count(s => s.LearningRequest.TeacherId == x.UserId &&
                    s.LearningRequest.SkillId == skill.Id),
                IsVerifiedExpert = true
            });
        var expertCount = await expertRows.CountAsync(cancellationToken);
        var experts = await expertRows.OrderByDescending(x => x.Rating.HasValue).ThenByDescending(x => x.Rating)
            .ThenBy(x => x.DisplayName).Take(PageSize).ToListAsync(cancellationToken);
        foreach (var expert in experts)
        {
            expert.Initials = NameFormatting.Initials(expert.DisplayName);
            expert.ProfileImageUrl = SafePublicImageUrl(expert.ProfileImageUrl);
        }

        var related = await db.Skills.AsNoTracking()
            .Where(x => x.Id != skill.Id && x.Status == PublicationStatus.Published &&
                        x.SubCategory.Slug == skill.SubCategorySlug && x.SubCategory.IsActive &&
                        x.SubCategory.Category.IsActive)
            .OrderBy(x => x.NameEn).Take(4)
            .Select(x => new RelatedSkillViewModel(x.NameEn, x.Slug)).ToListAsync(cancellationToken);

        return new SkillDiscoveryViewModel
        {
            SkillId = skill.Id, SkillName = skill.Name, SkillSlug = skill.Slug, AccessTier = skill.AccessTier,
            CategoryName = skill.CategoryName, CategorySlug = skill.CategorySlug,
            SubCategoryName = skill.SubCategoryName, SubCategorySlug = skill.SubCategorySlug,
            Search = query.Search?.Trim(), Level = query.Level, Sort = query.Sort,
            Partners = partners, Experts = experts, PartnerCount = partnerCount, ExpertCount = expertCount,
            RelatedSkills = related, Page = page, TotalPages = totalPages
        };
    }

    private static SkillDiscoveryViewModel CreateEmpty(int id, string name, string slug, string category,
        string categorySlug, string subCategory, string subCategorySlug, AccessTier accessTier, SkillDiscoveryQuery query) => new()
    {
        SkillId = id, SkillName = name, SkillSlug = slug, CategoryName = category, AccessTier = accessTier,
        CategorySlug = categorySlug, SubCategoryName = subCategory, SubCategorySlug = subCategorySlug,
        Search = query.Search, Level = query.Level, Sort = query.Sort, Page = 1, TotalPages = 1
    };

    internal static string? SafePublicImageUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (value.StartsWith('/') && !value.StartsWith("//")) return value;
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
            ? uri.AbsoluteUri
            : null;
    }
}
