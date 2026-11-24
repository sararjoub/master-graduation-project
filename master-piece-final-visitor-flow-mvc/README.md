# Skills Swap على ASP.NET Core MVC

هذا المشروع هو نسخة MVC من واجهات `master-piece-final-visitor-flow/master-piece`.

## التشغيل من Visual Studio

1. فك ضغط الملف وافتح `SkillsSwap.sln` في Visual Studio 2026 مع حزمة **ASP.NET and web development** و.NET 10 SDK.
2. اختَر مشروع **SkillsSwap** وملف التشغيل **http**، ثم اضغط **Ctrl+F5**.
3. الصفحة الرئيسية تفتح تلقائياً. لعرض تسجيل دخول الإدارة، افتح المسار `/Admin/admin-html/admin-login.html` على نفس عنوان المشروع المحلي.

يمكن تشغيل المشروع من الطرفية أيضاً باستخدام `dotnet run --project SkillsSwap.csproj --launch-profile http`.

## ملفات المشروع

- `Controllers` يحتوي إجراءات عرض الصفحات، مقسّمة حسب الوظيفة.
- `Views` يحتوي 59 صفحة Razor. القالب المشترك والأجزاء المعاد استخدامها موجودة في `Views/Shared`.
- `wwwroot` يحتوي CSS وJavaScript والصور والفيديو المحلي. عناوين الصفحات التي تنتهي بـ `.html` تخدمها إجراءات MVC، وتستخدمها سكربتات التنقل الموجودة.
- `Reference/Frontend` يحتوي ملاحظات النسخة الأصلية من الواجهات.

بيانات الدخول وشاشات الاشتراك والإدارة تجريبية للعرض المحلي. ربط قاعدة البيانات والمصادقة والصلاحيات الفعلية جزء من مرحلة الباك إند. الخط الخارجي وفيديو الصفحة الرئيسية الخارجي يحتاجان اتصالاً بالإنترنت.

[متطلبات توافق .NET وVisual Studio](https://learn.microsoft.com/en-us/dotnet/core/install/windows)
