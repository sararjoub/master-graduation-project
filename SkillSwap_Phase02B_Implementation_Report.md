# Skill Swap — تقرير تنفيذ Phase 02B

## 1. الملخص التنفيذي

تم تحويل رحلة اكتشاف شركاء التعلم والخبراء من بيانات JavaScript وهمية إلى استعلامات حقيقية من SQL Server عبر EF Core، وربط زر Skill Details بالصفحة الجديدة. تعرض الصفحة Peer Partners وVerified Experts لنفس المهارة مع بحث، مستوى، ترتيب، pagination، Related Skills، وحالات فراغ صادقة. حُفظت هوية Skill Swap وأضيفت تحسينات responsive وaccessibility ضمن الصفحات الواقعة في نطاق المرحلة فقط.

## 2. الوظائف المنفذة والمكونات المعاد استخدامها

- أضيفت `SkillDiscoveryService.DiscoverAsync` كخدمة reusable.
- أعيد استخدام `ApplicationDbContext`, `UserSkill`, `ExpertApplication`, `Review`, `LearningSession`, `SecurityPolicies.CompleteProfile`, `NameFormatting`, وبيانات Catalogue الحالية.
- المسار المحمي: `GET /skill-partners.html?skillSlug=c-sharp`.
- المسارات العامة الموجودة بقيت كما هي: Categories, SubCategory, Skill Details.
- زر Skill Details أصبح `Explore learning options` ويوضح Peer مقابل Expert دون تنفيذ Learning Request أو Booking.

## 3. قواعد Partner Discovery

يظهر العضو عندما يملك `UserSkill` للمهارة نفسها مع `Intent == Teach`، وحسابه `Active`، وبريده confirmed، وبروفايله complete، ويحمل Role `Member` وليس `Admin`، وليس المستخدم الحالي. لا يشترط Plus أو Expert approval أو reciprocal match أو Reviews سابقة. العضو الجديد بلا Reviews يظهر بحالة `New Teaching Partner · No Reviews Yet`.

## 4. قواعد Expert Discovery

الخبير يخضع لكل شروط Partner السابقة، إضافة إلى `ExpertApplication.Status == Approved` للمهارة نفسها. حالات `UnderReview`, `NeedsInformation`, `Rejected`, و`Withdrawn` مستبعدة. لا يوجد Expert Role، ولا تُستخدم Plus كدليل اعتماد. شرط استمرار Plus بعد الاعتماد بقي integration point غير محسوم، ولم يُhardcode.

## 5. Validation وSecurity

- يتحقق Server من slug ومن أن Skill `Published` وأن Category وSubCategory فعالتان.
- المهارة المفقودة أو غير المتاحة ترجع Not Found ولا تظهر عبر direct URL.
- المستخدم الحالي يؤخذ من `UserManager` ولا يقبل learner id من المتصفح.
- Controller ما زال تحت `CompleteProfile`، مع الحفاظ على Phase 02A وتعليق الحساب الفوري.
- ViewModels العامة لا تحتوي Email أو PasswordHash أو SecurityStamp أو PhoneNumber أو وثائق Expert الخاصة.
- النصوص التي أدخلها المستخدم تُعرض بترميز Razor الافتراضي.

## 6. Search، Sorting، Pagination، والأداء

- Search بالاسم، filter حسب `SkillLevel`، وترتيب `Newest`, `Name`, `Rating`.
- ترتيب deterministic؛ الجديد حسب `UserSkill.AddedAtUtc` ثم الاسم.
- `PageSize = 8` للشركاء، وbounded expert results.
- الاستعلامات async و`AsNoTracking()` وserver-side projection؛ لا تحميل لكل المستخدمين في الذاكرة ولا N+1.
- Rating وReview count وCompleted sessions محسوبة لنفس Skill فقط. لا تعرض نجوم صفرية عندما لا توجد Reviews.

## 7. Empty States والبدائل

- No Partners: شرح صادق، Clear Filters عند الحاجة، Browse Catalogue، مع استمرار قسم Experts وRelated Skills.
- No Experts: يوضح أن Peer Learning ما زال مجانيًا.
- No Matching Filters: يحافظ على Skill context ويوفر Clear.
- لا Learning Resources وهمية، ولا Waitlist أو Notify Me.

## 8. UI/UX والهوية الأصلية

### الجرد قبل التعديل

الهوية الحالية تستخدم `#8b7cf6` ودرجاتها، خلفيات بنفسجية فاتحة، Poppins للنص وInstrument Serif للعناوين، الشعار الحالي، navbar/footer، cards بحواف وshadows خفيفة، وحركات دخول/hover.

### المشاكل التي عولجت

- الصفحة القديمة احتوت أسماء وتقييمات وتوفرًا وهميًا وعدّادًا ثابتًا.
- فلتر Availability لم يكن له مصدر بيانات حقيقي.
- Volunteer وreciprocal wording تعارضا مع Business Requirements.
- JS كرر filter logic واستخدم delays ورسائل نجاح غير حقيقية.
- لم يكن هناك فصل واضح بين Peer المجاني وExpert premium.

### التحسينات

- Learning-option cards واضحة، نتيجة فعلية، cards للشركاء والخبراء، badge خبير حقيقي فقط، breadcrumbs، filters، pagination، Related Skills، وحالات فراغ.
- responsive breakpoints للـmobile/tablet/desktop ومنع ازدحام controls.
- semantic headings، labels، focus styles، `aria-current`, status regions، و`prefers-reduced-motion`.
- لا يوجد JavaScript خاص بالاكتشاف؛ الوظيفة الأساسية Server-rendered MVC.
- أبقينا shared navigation/footer والألوان والخطوط ولم نغير Admin أو Onboarding أو Registration.

## 9. Mock Data التي أزيلت

أزيلت بيانات Ahmad/Lana/Sara/Omar والratings/availability الوهمية من صفحة discovery. أصبح `workflow-catalogue.js` compatibility object فارغًا للصفحات المؤجلة بدل صناعة بيانات إنتاجية. لم تتم إضافة Users أو Experts أو Reviews أو Seed data.

## 10. الملفات

### ملفات جديدة

- `master-piece-final-visitor-flow-mvc/Services/SkillDiscoveryService.cs`
- `master-piece-final-visitor-flow-mvc/Views/Skills/_DiscoveryPersonCard.cshtml`
- `SkillsSwap.Tests/SkillDiscoveryServiceTests.cs`
- `PROJECT_DEVELOPMENT_GUIDELINES.md`
- `SkillSwap_Phase02B_Implementation_Report.md`

### ملفات معدلة

- `master-piece-final-visitor-flow-mvc/Models/MemberViewModels.cs`
- `master-piece-final-visitor-flow-mvc/Controllers/SkillsController.cs`
- `master-piece-final-visitor-flow-mvc/Program.cs`
- `master-piece-final-visitor-flow-mvc/Views/Skills/SkillPartners.cshtml`
- `master-piece-final-visitor-flow-mvc/Views/Skills/SkillDetails.cshtml`
- `master-piece-final-visitor-flow-mvc/wwwroot/css/partners.css`
- `master-piece-final-visitor-flow-mvc/wwwroot/js/workflow-catalogue.js`

## 11. Database وMigrations

لا Entity أو relationship أو migration أو connection string تغيرت. لم يُنفذ `database update`، ولم تُضف بيانات اختبار إلى Development SQL Server. لذلك لا يوجد EF model schema impact في هذه المرحلة.

## 12. Automated Verification

- Isolated build: **Succeeded — 0 errors, 0 warnings**.
- Tests: **41 passed, 0 failed** (18 Phase 02A + 23 Phase 02B test cases).
- استخدمت الاختبارات EF Core InMemory بقواعد معزولة، ولم تتصل بقاعدة التطوير.
- تشمل الاختبارات: exact Teach skill، Learn-only/other skill/current user، Suspended/Closed/unverified/incomplete، بدون reciprocal أو reviews، كل Expert statuses، skill-specific approval، catalogue hierarchy، empty results، search/level، Admin exclusion، عدم كشف private fields، وحجب unsafe profile image URLs.

الـbuild العادي حاول النسخ إلى `bin/Debug` وفشل بسبب أن التطبيق المفتوح يقفل `SkillsSwap.dll`. لم تُوقف العملية احترامًا للتعليمات. تم البناء بنجاح إلى `tmp/phase02b-output` للتحقق من compilation الفعلي.

## 13. التحقق البصري والقيود

لم يُدّعَ Browser verification للنسخة الجديدة لأن السيرفر المفتوح يستخدم DLL القديمة ويحتاج Stop/Restart يدويًا. لم يتم التقاط before/after screenshots. تحقق الـRazor أثناء build بنجاح. يلزم الفحص اليدوي التالي بعد إعادة التشغيل.

## 14. خطوات الاختبار في Visual Studio

1. أوقفي التشغيل الحالي ثم `Build > Rebuild Solution` وشغلي المشروع.
2. أنشئي حسابين حقيقيين، أكدي البريد، وأكملي البروفايل لكليهما.
3. في حساب Teacher اختاري Skill تحت `Can Teach`، وفي Learner افتحي نفس Skill ثم `Explore learning options`.
4. تحققي من ظهور Teacher وعدم ظهور Learner نفسه أو مستخدم Learn-only.
5. جربي Search وTeaching Level وSort وpagination وClear.
6. اختبري Skill بلا Teachers وتحققي من Empty State وRelated Skills وBrowse Catalogue.
7. لاختبار Expert استخدمي ExpertApplication حقيقية وافقي عليها من Admin للمهارة نفسها؛ تحققي من badge. غيري الحالة إلى pending/rejected أو اختاري skill أخرى وتحققي من الاختفاء.
8. افحصي العرض عند 360، 390، 768، 1024، 1440، و1920px، والـkeyboard focus.
9. راجعي Console للتأكد من عدم وجود broken assets أو JavaScript errors.

## 15. المحتوى والوظائف المحفوظة

- Logo، navbar، footer، brand palette، fonts، theme/language scripts، catalogue routes، Skill Details، Plus navigation، Auth وAdmin protection بقيت.
- لم تُنفذ عمليات نجاح وهمية، Learning Requests، Booking، Chat، Zoom، Payments، Expert documents، AI، Career assessments أو Notifications.

## 16. القيود والقرارات المؤجلة

- لا يمكن الاعتماد على availability لأن schema الحالي لا يوفر availability للـpeer؛ لذلك لم يُعرض فلتر وهمي.
- Profile action لم يُربط بصفحة عامة جديدة لأن public profile authorization والمحتوى يحتاجان نطاقًا مستقلًا أو Phase 03؛ البطاقة توضح أن Learning Request/Booking قادم بدل زر لا يعمل.
- Expert results bounded إلى 8؛ يمكن إضافة pagination مستقلة إذا زاد الحجم.
- شرط Plus المستمر للخبير بعد الاعتماد غير محسوم.
- Legacy future-phase screens التي تعتمد Catalogue JavaScript أصبحت بلا بيانات تجريبية وقد تحتاج تنفيذًا حقيقيًا في مراحلها.

## 17. Resource Authorization المقترح لـPhase 03

عند إنشاء `LearningRequest` يجب استخدام resource-based authorization handler يتحقق أن actor هو Learner أو Teacher المرتبط بالطلب، وأن الحساب Active، وأن Skill مطابقة، وأن الانتقال بين statuses مسموح. Expert request يضيف server-side checks للـApproved application، Active Plus، credits، وavailability. لا يُوثق بأي IDs أو entitlement قادمة من client.

## 18. النطاق المقترح لـPhase 03

Learning Request Workflow + UI/UX: إنشاء request حقيقي، منع self-request والduplicates المناسبة، acceptance/rejection، status history، resource authorization، notifications الحقيقية اللازمة، empty/error/success states، ثم tests وresponsive/accessibility. Session scheduling وZoom/Chat يبقيان مرحلة لاحقة ما لم يتم اعتمادهما صراحة.
