using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SkillsSwap.Migrations
{
    /// <inheritdoc />
    public partial class SeedCatalogueAndAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Description", "IsActive", "NameAr", "NameEn", "Slug" },
                values: new object[,]
                {
                    { 1, null, true, "التكنولوجيا", "Technology", "technology" },
                    { 2, null, true, "التصميم والإبداع", "Design & Creative", "design-creative" },
                    { 3, null, true, "الأعمال", "Business", "business" },
                    { 4, null, true, "اللغات والتعليم", "Languages & Education", "languages-education" },
                    { 5, null, true, "الصحة والعافية", "Health & Wellness", "health-wellness" },
                    { 6, null, true, "الإعلام والمهارات الحياتية", "Media & Lifestyle", "media-lifestyle" }
                });

            migrationBuilder.InsertData(
                table: "SubCategories",
                columns: new[] { "Id", "CategoryId", "IsActive", "NameAr", "NameEn", "Slug" },
                values: new object[,]
                {
                    { 1, 1, true, "تطوير الويب", "Web Development", "web-development" },
                    { 2, 1, true, "البرمجة والبيانات", "Programming & Data", "programming-data" },
                    { 3, 1, true, "الموبايل والسحابة والأمن", "Mobile, Cloud & Security", "mobile-cloud-security" },
                    { 4, 2, true, "تصميم تجربة المستخدم", "UI/UX Design", "ui-ux" },
                    { 5, 2, true, "الفنون البصرية", "Visual Arts", "visual-arts" },
                    { 6, 3, true, "التسويق والمبيعات", "Marketing & Sales", "marketing-sales" },
                    { 7, 3, true, "الإدارة والمالية", "Management & Finance", "management-finance" },
                    { 8, 4, true, "اللغات", "Languages", "languages" },
                    { 9, 4, true, "المهارات الأكاديمية", "Academic Skills", "academic-skills" },
                    { 10, 5, true, "اللياقة البدنية", "Fitness", "fitness" },
                    { 11, 5, true, "العافية", "Wellness", "wellness" },
                    { 12, 6, true, "التصوير والفيديو", "Photo & Video", "photo-video" },
                    { 13, 6, true, "الموسيقى", "Music", "music" },
                    { 14, 6, true, "الأعمال اليدوية", "Handmade", "handmade" },
                    { 15, 6, true, "التواصل", "Communication", "communication" }
                });

            migrationBuilder.InsertData(
                table: "Skills",
                columns: new[] { "Id", "AccessTier", "Description", "Level", "NameAr", "NameEn", "Slug", "Status", "SubCategoryId" },
                values: new object[,]
                {
                    { 1, 0, "Learn HTML & CSS through community guidance and structured practice.", 0, "HTML وCSS", "HTML & CSS", "html-css", 1, 1 },
                    { 2, 0, "Learn JavaScript through community guidance and structured practice.", 0, "جافاسكربت", "JavaScript", "javascript", 1, 1 },
                    { 3, 0, "Learn Responsive Web Design through community guidance and structured practice.", 1, "تصميم الويب المتجاوب", "Responsive Web Design", "responsive-web-design", 1, 1 },
                    { 4, 1, "Learn React through community guidance and structured practice.", 2, "رياكت", "React", "react", 1, 1 },
                    { 5, 1, "Learn ASP.NET Core through community guidance and structured practice.", 2, "ASP.NET Core", "ASP.NET Core", "aspnet-core", 1, 1 },
                    { 6, 0, "Learn Python through community guidance and structured practice.", 0, "بايثون", "Python", "python", 1, 2 },
                    { 7, 0, "Learn C# through community guidance and structured practice.", 1, "سي شارب", "C#", "csharp", 1, 2 },
                    { 8, 0, "Learn SQL & Databases through community guidance and structured practice.", 1, "SQL وقواعد البيانات", "SQL & Databases", "sql-databases", 1, 2 },
                    { 9, 1, "Learn Data Analysis through community guidance and structured practice.", 1, "تحليل البيانات", "Data Analysis", "data-analysis", 1, 2 },
                    { 10, 1, "Learn Machine Learning through community guidance and structured practice.", 2, "تعلم الآلة", "Machine Learning", "machine-learning", 1, 2 },
                    { 11, 1, "Learn Flutter through community guidance and structured practice.", 2, "فلاتر", "Flutter", "flutter", 1, 3 },
                    { 12, 0, "Learn Git & GitHub through community guidance and structured practice.", 0, "Git وGitHub", "Git & GitHub", "git-github", 1, 3 },
                    { 13, 0, "Learn Cybersecurity Basics through community guidance and structured practice.", 1, "أساسيات الأمن السيبراني", "Cybersecurity Basics", "cybersecurity-basics", 1, 3 },
                    { 14, 1, "Learn Cloud & DevOps through community guidance and structured practice.", 2, "الحوسبة السحابية وDevOps", "Cloud & DevOps", "cloud-devops", 1, 3 },
                    { 15, 0, "Learn Figma through community guidance and structured practice.", 0, "فيجما", "Figma", "figma", 1, 4 },
                    { 16, 0, "Learn UI/UX Design through community guidance and structured practice.", 1, "تصميم UI/UX", "UI/UX Design", "ui-ux-design", 1, 4 },
                    { 17, 1, "Learn User Research through community guidance and structured practice.", 2, "بحث المستخدم", "User Research", "user-research", 1, 4 },
                    { 18, 0, "Learn Graphic Design through community guidance and structured practice.", 0, "التصميم الجرافيكي", "Graphic Design", "graphic-design", 1, 5 },
                    { 19, 1, "Learn Digital Illustration through community guidance and structured practice.", 1, "الرسم الرقمي", "Digital Illustration", "digital-illustration", 1, 5 },
                    { 20, 0, "Learn Digital Marketing through community guidance and structured practice.", 0, "التسويق الرقمي", "Digital Marketing", "digital-marketing", 1, 6 },
                    { 21, 0, "Learn Social Media Marketing through community guidance and structured practice.", 1, "التسويق عبر التواصل الاجتماعي", "Social Media Marketing", "social-media-marketing", 1, 6 },
                    { 22, 1, "Learn SEO through community guidance and structured practice.", 2, "تحسين محركات البحث", "SEO", "seo", 1, 6 },
                    { 23, 0, "Learn Project Management through community guidance and structured practice.", 1, "إدارة المشاريع", "Project Management", "project-management", 1, 7 },
                    { 24, 0, "Learn Entrepreneurship through community guidance and structured practice.", 0, "ريادة الأعمال", "Entrepreneurship", "entrepreneurship", 1, 7 },
                    { 25, 1, "Learn Excel for Finance through community guidance and structured practice.", 1, "Excel للمالية", "Excel for Finance", "excel-finance", 1, 7 },
                    { 26, 0, "Learn English Conversation through community guidance and structured practice.", 0, "المحادثة الإنجليزية", "English Conversation", "english-conversation", 1, 8 },
                    { 27, 0, "Learn Arabic for Non-Native Speakers through community guidance and structured practice.", 0, "العربية لغير الناطقين بها", "Arabic for Non-Native Speakers", "arabic-for-non-native", 1, 8 },
                    { 28, 1, "Learn IELTS Preparation through community guidance and structured practice.", 2, "التحضير لآيلتس", "IELTS Preparation", "ielts-preparation", 1, 8 },
                    { 29, 0, "Learn Mathematics through community guidance and structured practice.", 0, "الرياضيات", "Mathematics", "mathematics", 1, 9 },
                    { 30, 0, "Learn Study Skills through community guidance and structured practice.", 0, "مهارات الدراسة", "Study Skills", "study-skills", 1, 9 },
                    { 31, 0, "Learn Yoga through community guidance and structured practice.", 0, "اليوغا", "Yoga", "yoga", 1, 10 },
                    { 32, 1, "Learn Personal Training through community guidance and structured practice.", 2, "التدريب الشخصي", "Personal Training", "personal-training", 1, 10 },
                    { 33, 0, "Learn Nutrition Basics through community guidance and structured practice.", 0, "أساسيات التغذية", "Nutrition Basics", "nutrition-basics", 1, 11 },
                    { 34, 0, "Learn Meditation through community guidance and structured practice.", 0, "التأمل", "Meditation", "meditation", 1, 11 },
                    { 35, 0, "Learn Photography through community guidance and structured practice.", 0, "التصوير الفوتوغرافي", "Photography", "photography", 1, 12 },
                    { 36, 1, "Learn Video Editing through community guidance and structured practice.", 1, "تحرير الفيديو", "Video Editing", "video-editing", 1, 12 },
                    { 37, 0, "Learn Guitar through community guidance and structured practice.", 0, "الغيتار", "Guitar", "guitar", 1, 13 },
                    { 38, 1, "Learn Music Production through community guidance and structured practice.", 2, "الإنتاج الموسيقي", "Music Production", "music-production", 1, 13 },
                    { 39, 0, "Learn Crochet through community guidance and structured practice.", 0, "الكروشيه", "Crochet", "crochet", 1, 14 },
                    { 40, 0, "Learn DIY Crafts through community guidance and structured practice.", 0, "الأعمال اليدوية المنزلية", "DIY Crafts", "diy-crafts", 1, 14 },
                    { 41, 0, "Learn Public Speaking through community guidance and structured practice.", 1, "التحدث أمام الجمهور", "Public Speaking", "public-speaking", 1, 15 },
                    { 42, 1, "Learn Presentation Skills through community guidance and structured practice.", 1, "مهارات العرض", "Presentation Skills", "presentation-skills", 1, 15 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 34);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 35);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 36);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 37);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 38);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 39);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 40);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 41);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 42);

            migrationBuilder.DeleteData(
                table: "SubCategories",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "SubCategories",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "SubCategories",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "SubCategories",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "SubCategories",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "SubCategories",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "SubCategories",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "SubCategories",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "SubCategories",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "SubCategories",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "SubCategories",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "SubCategories",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "SubCategories",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "SubCategories",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "SubCategories",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 6);
        }
    }
}
