using Microsoft.EntityFrameworkCore;
using SkillsSwap.Models;

namespace SkillsSwap.Data;

internal static class ModelSeed
{
    public static void Configure(ModelBuilder b)
    {
        b.Entity<Category>().HasData(
            Cat(1, "technology", "التكنولوجيا", "Technology"),
            Cat(2, "design-creative", "التصميم والإبداع", "Design & Creative"),
            Cat(3, "business", "الأعمال", "Business"),
            Cat(4, "languages-education", "اللغات والتعليم", "Languages & Education"),
            Cat(5, "health-wellness", "الصحة والعافية", "Health & Wellness"),
            Cat(6, "media-lifestyle", "الإعلام والمهارات الحياتية", "Media & Lifestyle"));

        b.Entity<SubCategory>().HasData(
            Sub(1, 1, "web-development", "تطوير الويب", "Web Development"),
            Sub(2, 1, "programming-data", "البرمجة والبيانات", "Programming & Data"),
            Sub(3, 1, "mobile-cloud-security", "الموبايل والسحابة والأمن", "Mobile, Cloud & Security"),
            Sub(4, 2, "ui-ux", "تصميم تجربة المستخدم", "UI/UX Design"),
            Sub(5, 2, "visual-arts", "الفنون البصرية", "Visual Arts"),
            Sub(6, 3, "marketing-sales", "التسويق والمبيعات", "Marketing & Sales"),
            Sub(7, 3, "management-finance", "الإدارة والمالية", "Management & Finance"),
            Sub(8, 4, "languages", "اللغات", "Languages"),
            Sub(9, 4, "academic-skills", "المهارات الأكاديمية", "Academic Skills"),
            Sub(10, 5, "fitness", "اللياقة البدنية", "Fitness"),
            Sub(11, 5, "wellness", "العافية", "Wellness"),
            Sub(12, 6, "photo-video", "التصوير والفيديو", "Photo & Video"),
            Sub(13, 6, "music", "الموسيقى", "Music"),
            Sub(14, 6, "handmade", "الأعمال اليدوية", "Handmade"),
            Sub(15, 6, "communication", "التواصل", "Communication"));

        b.Entity<Skill>().HasData(
            SkillRow(1, 1, "html-css", "HTML وCSS", "HTML & CSS", SkillLevel.Beginner, AccessTier.Free),
            SkillRow(2, 1, "javascript", "جافاسكربت", "JavaScript", SkillLevel.Beginner, AccessTier.Free),
            SkillRow(3, 1, "responsive-web-design", "تصميم الويب المتجاوب", "Responsive Web Design", SkillLevel.Intermediate, AccessTier.Free),
            SkillRow(4, 1, "react", "رياكت", "React", SkillLevel.Advanced, AccessTier.Plus),
            SkillRow(5, 1, "aspnet-core", "ASP.NET Core", "ASP.NET Core", SkillLevel.Advanced, AccessTier.Plus),
            SkillRow(6, 2, "python", "بايثون", "Python", SkillLevel.Beginner, AccessTier.Free),
            SkillRow(7, 2, "csharp", "سي شارب", "C#", SkillLevel.Intermediate, AccessTier.Free),
            SkillRow(8, 2, "sql-databases", "SQL وقواعد البيانات", "SQL & Databases", SkillLevel.Intermediate, AccessTier.Free),
            SkillRow(9, 2, "data-analysis", "تحليل البيانات", "Data Analysis", SkillLevel.Intermediate, AccessTier.Plus),
            SkillRow(10, 2, "machine-learning", "تعلم الآلة", "Machine Learning", SkillLevel.Advanced, AccessTier.Plus),
            SkillRow(11, 3, "flutter", "فلاتر", "Flutter", SkillLevel.Advanced, AccessTier.Plus),
            SkillRow(12, 3, "git-github", "Git وGitHub", "Git & GitHub", SkillLevel.Beginner, AccessTier.Free),
            SkillRow(13, 3, "cybersecurity-basics", "أساسيات الأمن السيبراني", "Cybersecurity Basics", SkillLevel.Intermediate, AccessTier.Free),
            SkillRow(14, 3, "cloud-devops", "الحوسبة السحابية وDevOps", "Cloud & DevOps", SkillLevel.Advanced, AccessTier.Plus),
            SkillRow(15, 4, "figma", "فيجما", "Figma", SkillLevel.Beginner, AccessTier.Free),
            SkillRow(16, 4, "ui-ux-design", "تصميم UI/UX", "UI/UX Design", SkillLevel.Intermediate, AccessTier.Free),
            SkillRow(17, 4, "user-research", "بحث المستخدم", "User Research", SkillLevel.Advanced, AccessTier.Plus),
            SkillRow(18, 5, "graphic-design", "التصميم الجرافيكي", "Graphic Design", SkillLevel.Beginner, AccessTier.Free),
            SkillRow(19, 5, "digital-illustration", "الرسم الرقمي", "Digital Illustration", SkillLevel.Intermediate, AccessTier.Plus),
            SkillRow(20, 6, "digital-marketing", "التسويق الرقمي", "Digital Marketing", SkillLevel.Beginner, AccessTier.Free),
            SkillRow(21, 6, "social-media-marketing", "التسويق عبر التواصل الاجتماعي", "Social Media Marketing", SkillLevel.Intermediate, AccessTier.Free),
            SkillRow(22, 6, "seo", "تحسين محركات البحث", "SEO", SkillLevel.Advanced, AccessTier.Plus),
            SkillRow(23, 7, "project-management", "إدارة المشاريع", "Project Management", SkillLevel.Intermediate, AccessTier.Free),
            SkillRow(24, 7, "entrepreneurship", "ريادة الأعمال", "Entrepreneurship", SkillLevel.Beginner, AccessTier.Free),
            SkillRow(25, 7, "excel-finance", "Excel للمالية", "Excel for Finance", SkillLevel.Intermediate, AccessTier.Plus),
            SkillRow(26, 8, "english-conversation", "المحادثة الإنجليزية", "English Conversation", SkillLevel.Beginner, AccessTier.Free),
            SkillRow(27, 8, "arabic-for-non-native", "العربية لغير الناطقين بها", "Arabic for Non-Native Speakers", SkillLevel.Beginner, AccessTier.Free),
            SkillRow(28, 8, "ielts-preparation", "التحضير لآيلتس", "IELTS Preparation", SkillLevel.Advanced, AccessTier.Plus),
            SkillRow(29, 9, "mathematics", "الرياضيات", "Mathematics", SkillLevel.Beginner, AccessTier.Free),
            SkillRow(30, 9, "study-skills", "مهارات الدراسة", "Study Skills", SkillLevel.Beginner, AccessTier.Free),
            SkillRow(31, 10, "yoga", "اليوغا", "Yoga", SkillLevel.Beginner, AccessTier.Free),
            SkillRow(32, 10, "personal-training", "التدريب الشخصي", "Personal Training", SkillLevel.Advanced, AccessTier.Plus),
            SkillRow(33, 11, "nutrition-basics", "أساسيات التغذية", "Nutrition Basics", SkillLevel.Beginner, AccessTier.Free),
            SkillRow(34, 11, "meditation", "التأمل", "Meditation", SkillLevel.Beginner, AccessTier.Free),
            SkillRow(35, 12, "photography", "التصوير الفوتوغرافي", "Photography", SkillLevel.Beginner, AccessTier.Free),
            SkillRow(36, 12, "video-editing", "تحرير الفيديو", "Video Editing", SkillLevel.Intermediate, AccessTier.Plus),
            SkillRow(37, 13, "guitar", "الغيتار", "Guitar", SkillLevel.Beginner, AccessTier.Free),
            SkillRow(38, 13, "music-production", "الإنتاج الموسيقي", "Music Production", SkillLevel.Advanced, AccessTier.Plus),
            SkillRow(39, 14, "crochet", "الكروشيه", "Crochet", SkillLevel.Beginner, AccessTier.Free),
            SkillRow(40, 14, "diy-crafts", "الأعمال اليدوية المنزلية", "DIY Crafts", SkillLevel.Beginner, AccessTier.Free),
            SkillRow(41, 15, "public-speaking", "التحدث أمام الجمهور", "Public Speaking", SkillLevel.Intermediate, AccessTier.Free),
            SkillRow(42, 15, "presentation-skills", "مهارات العرض", "Presentation Skills", SkillLevel.Intermediate, AccessTier.Plus));
    }

    private static Category Cat(int id, string slug, string ar, string en) => new() { Id = id, Slug = slug, NameAr = ar, NameEn = en, IsActive = true };
    private static SubCategory Sub(int id, int categoryId, string slug, string ar, string en) => new() { Id = id, CategoryId = categoryId, Slug = slug, NameAr = ar, NameEn = en, IsActive = true };
    private static Skill SkillRow(int id, int subId, string slug, string ar, string en, SkillLevel level, AccessTier tier) => new()
    {
        Id = id, SubCategoryId = subId, Slug = slug, NameAr = ar, NameEn = en,
        Description = $"Learn {en} through community guidance and structured practice.",
        Level = level, AccessTier = tier, Status = PublicationStatus.Published
    };
}
