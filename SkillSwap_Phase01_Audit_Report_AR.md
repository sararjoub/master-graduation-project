# مشروع Skill Swap — تقرير التدقيق والتحليل الشامل

**المرحلة:** Phase 01 — Existing Project Audit and Gap Analysis  
**تاريخ التقرير:** 24 نوفمبر 2026  
**نوع المراجعة:** قراءة وتحليل فقط دون تنفيذ تغييرات برمجية أو تحديث قاعدة البيانات

---

## 1. الملخص التنفيذي

Skill Swap مشروع تخرج قائم مبني باستخدام ASP.NET Core MVC وRazor Views وASP.NET Core Identity وEntity Framework Core Code First مع SQL Server. المشروع ليس Greenfield، ولذلك يجب الحفاظ على التصميم الحالي، قاعدة البيانات، migrations، والوظائف المنفذة، ثم استكماله تدريجيًا.

المنصة تدعم مسارين رئيسيين:

1. استكشاف المهارات والتعلم من أعضاء آخرين أو خبراء معتمدين.
2. إنشاء مسار مهني شخصي بمساعدة Ollama، مع مراحل متسلسلة ومتطلبات إنجاز حقيقية.

المشروع يحتوي حاليًا على أساس جيد للحسابات، إعداد البروفايل، الكتالوج، مهارات المستخدم، وعدد من وظائف الأدمن. لكن أجزاء الجلسات، الشراكات، المحادثة، Zoom، الاشتراك الفعلي، تقديم طلب خبير، والمسار المهني ما زالت إما واجهات فقط أو نماذج قاعدة بيانات غير مرتبطة بوظائف مكتملة.

لا يجوز اعتبار وجود صفحة Razor أو Entity دليلًا على اكتمال الخاصية. الخاصية تُعد مكتملة فقط إذا كان المسار كاملًا من الواجهة إلى Controller وService وقاعدة البيانات مع التحقق والصلاحيات اللازمة.

---

## 2. المتطلبات الأساسية المعتمدة

### 2.1 الحسابات والأدوار

- التسجيل ذاتي ولا يتم إنشاء المستخدمين بواسطة الأدمن.
- يجب تأكيد البريد الإلكتروني.
- يجب إكمال البروفايل قبل استخدام الوظائف المحمية.
- الأدوار الأساسية هي `Member` و`Admin` فقط.
- لا يوجد Volunteer Role.
- الاشتراك ليس Role، بل Business Entitlement منفصل.
- اعتماد الخبير ليس Role عامًا، بل اعتماد مرتبط بمهارة محددة.
- يمكن أن يكون المستخدم خبيرًا في JavaScript ومبتدئًا في Python.

### 2.2 مهارات المستخدم

- يختار المستخدم المهارات التي يريد تعلمها.
- مهارات التعليم اختيارية ويمكن تخطيها.
- لا يُشترط تبادل المهارات بين نفس الشخصين.
- يستطيع المستخدم تعليم شخص والتعلم من شخص آخر.

### 2.3 مسار استكشاف المهارات

- يتصفح المستخدم Categories ثم Subcategories ثم Skills.
- عند اختيار Start Learning يجب عرض:
  - أعضاء حقيقيين قادرين على تعليم المهارة.
  - خبراء معتمدين في نفس المهارة فقط.
- التعلم Peer-to-peer مجاني ضمن النطاق الأساسي.
- حجز جلسة مع خبير يتطلب:
  - اشتراك Plus فعال.
  - رصيد Expert Session كافي.
  - اعتماد الخبير في المهارة.
  - توفر موعد صالح.

### 2.4 Empty States

يجب عدم استخدام مستخدمين أو خبراء أو تقييمات أو موارد وهمية لإخفاء الفراغ.

عند عدم وجود شركاء تعلم يجب تقديم بدائل حقيقية مثل:

- استكشاف الخبراء المعتمدين إن وجدوا.
- عرض موارد تعلم حقيقية إن وجدت.
- استكشاف مهارات مرتبطة.
- العودة إلى كتالوج المهارات.

### 2.5 Chat وZoom

- Chat وZoom متاحان لأعضاء Basic وPlus في الجلسات Peer-to-peer المقبولة.
- Zoom ليس خاصية Premium بحد ذاته.
- القيد المدفوع يخص حجز الخبير، وليس بنية التواصل أو الاجتماع.
- Peer sessions وExpert sessions يعيدان استخدام بنية اتصال واجتماعات مشتركة.
- لا يصل إلى المحادثة أو الاجتماع إلا أطراف الجلسة المصرح لهم.
- كود Zoom الحالي سيُستلم لاحقًا، ولا يُستبدل أو ينفذ قبل استلامه.

### 2.6 Expert Workflow

- المستخدم يبقى Member حتى بعد اعتماده كخبير.
- اشتراك Plus فعال مطلوب لتقديم طلب اعتماد خبير حسب المتطلبات الحالية.
- الطلب مرتبط بمهارة تعليم يملكها المستخدم.
- يحتوي الطلب على الخبرة، أسلوب التعليم، Portfolio، وأدلة أو عينة شرح.
- قرارات الأدمن:
  - Approved.
  - Needs Information.
  - Rejected.
- يجب حفظ سبب القرار وهوية الأدمن والتاريخ.
- الشراء لا يمنح اعتماد خبير تلقائيًا.
- شروط مثل 10 جلسات أو متوسط 4.5 اقتراحات مؤقتة، ولا يجوز hardcode لها الآن.

### 2.7 AI Career Path

- يملأ المستخدم استبيانًا يتضمن:
  - الهدف.
  - المستوى الحالي.
  - الخبرة السابقة.
  - الوقت المتاح.
  - تفضيلات التعلم.
- يولد Ollama مسارًا منظمًا.
- يجب التحقق من الناتج قبل حفظه في SQL Server.
- المراحل متسلسلة ولا تُفتح المرحلة التالية إلا بعد استيفاء شروط السابقة.
- المراحل الأساسية Free والمتقدمة قد تتطلب Plus.
- قيد التقدم وقيد الاشتراك يجب تطبيقهما بشكل مستقل على الخادم.
- لا تُستكمل المرحلة بمجرد فتح الصفحة.
- الإكمال المستقبلي يعتمد على Objectives وChecklists وPractical Tasks وQuizzes أو تقييم مناسب للمهارة.
- يستطيع المستخدم إكمال المسار ذاتيًا دون إلزامه بحجز شريك أو خبير.
- لا تُثبت passing thresholds أو قواعد تقييم مؤقتة قبل اعتمادها.

---

## 3. البنية والتقنيات الحالية

### نتائج مؤكدة

- Target Framework: `.NET 10.0`.
- ASP.NET Core MVC.
- Razor Views.
- ASP.NET Core Identity Entity Framework Core `10.0.14`.
- EF Core SQL Server `10.0.14`.
- EF Core Design `10.0.14`.
- Code First migrations.
- SQL Server.
- Nullable Reference Types مفعلة.
- User Secrets معرف للمشروع.

### التنظيم الحالي

- Controllers المستخدم موجودة داخل `Controllers`.
- Controllers الأدمن الأساسية موجودة داخل `Areas/Admin/Controllers`.
- Services الحالية:
  - `CatalogueService`.
  - `AdminPeopleService`.
  - `AdminOperationsService`.
- Member controllers ما زالت تستخدم `ApplicationDbContext` مباشرة في عدة أماكن.
- ViewModels موجودة حاليًا داخل مجلد `Models`.
- Views الأدمن موجودة داخل `Views/Admin` ويتم فتحها من الـArea بمسارات صريحة.
- لا توجد حاليًا حزم SignalR أو Zoom أو Ollama في ملف المشروع.
- لا يوجد Test Project.

---

## 4. كيانات قاعدة البيانات والعلاقات

### الكيانات الحالية

- `ApplicationUser`
- `Category`
- `SubCategory`
- `Skill`
- `UserSkill`
- `ExpertApplication`
- `ExpertAvailability`
- `Plan`
- `Subscription`
- `Payment`
- `LearningRequest`
- `LearningSession`
- `Message`
- `Review`
- `CareerPath`
- `CareerPathStep`

### القيود الجيدة الموجودة

- Unique slug للـCategory والـSkill.
- Unique slug داخل كل Category للـSubCategory.
- Composite key في `UserSkill` على User + Skill + Intent.
- طلب خبير فريد لكل User + Skill.
- منع Learning Request بين المستخدم ونفسه.
- جلسة واحدة لكل Learning Request.
- تقييم واحد لكل Reviewer لكل Session.
- Rating بين 1 و5.
- ترتيب Career Path Step فريد داخل المسار.
- Restrict deletion للعلاقات المرتبطة بالمستخدم للحفاظ على التاريخ.
- خطتا FREE وPLUS موجودتان كبذور أساسية.

### ملاحظات سلامة البيانات

- لا يوجد Credit Ledger لجلسات الخبراء.
- لا يوجد Booking entity واضح يربط الموعد والخبير والرصيد.
- لا يوجد Audit Log عام لقرارات الأدمن.
- سبب تعليق الحساب يُطلب في الواجهة لكنه لا يُحفظ.
- لا يوجد Complaint/Report entity.
- لا يوجد Learning Resource entity.
- لا توجد كيانات Objectives أو Checklists أو Practical Tasks أو Quizzes.
- Unique index لطلب الخبير قد يمنع إعادة التقديم بعد الرفض ما لم يُعاد فتح نفس السجل.
- لا يوجد قيد يمنع وجود أكثر من اشتراك Active متداخل للمستخدم.
- Message لا يضمن على مستوى قاعدة البيانات أن Sender طرف في Learning Request.
- Review يحتاج تحققًا صريحًا أن Reviewer وReviewedUser هما طرفا الجلسة.

---

## 5. مصفوفة تنفيذ الخصائص

| الخاصية | الحالة | التحليل |
|---|---|---|
| التسجيل الذاتي | Fully Implemented | Identity + Member Role |
| تسجيل الدخول الموحد | Fully Implemented | الوجهة تحدد حسب Role |
| تأكيد البريد | Partially Implemented | Token flow موجود، لا إرسال بريد حقيقي |
| Forgot/Reset Password | Partially Implemented | Token موجود، لا Email provider |
| إعداد البروفايل | Fully Implemented | يحفظ البيانات في SQL |
| اختيار مهارات التعلم | Fully Implemented | تخزين فعلي |
| مهارات التعليم الاختيارية | Fully Implemented | يمكن تركها فارغة |
| تصفح Categories/Subcategories/Skills | Fully Implemented | قراءة فعلية من SQL |
| Related Skills | Fully Implemented | Query فعلية |
| Empty States | Partially Implemented | لا توجد جميع البدائل المطلوبة |
| Partner discovery حسب المهارة | UI Only | لا Query مكتملة للصفحة |
| عرض الخبراء حسب المهارة | Missing | لا تجميع فعلي بعد |
| Learning Request creation | UI Only | Entity موجودة دون POST فعلي |
| قبول/رفض الطلب | UI Only | لا Server workflow |
| Peer Chat | UI Only | لا SignalR أو ownership checks |
| Peer Zoom | UI Only | Placeholder فقط |
| Chat وZoom لـBasic وPlus | Missing | لا بنية مشتركة منفذة |
| Expert Booking | UI Only | لا entitlement أو credits |
| Expert Availability | Database Only | Entity دون workflow |
| تقديم طلب Expert | UI Only | لا حفظ فعلي |
| مراجعة Expert Application | Partially Implemented | الأدمن يراجع سجلات موجودة، لكن member submission غير مربوط |
| Plus required for expert application | Missing | لا Server enforcement |
| إدارة المستخدمين | Fully Implemented | عرض وبحث وتفاصيل وحالة |
| إدارة الكتالوج | Fully Implemented | Categories/Subcategories/Skills |
| إدارة الخطط | Partially Implemented | تعديل فعلي دون audit history |
| شراء Plus | UI Only | لا دفع فعلي |
| إلغاء الاشتراك | Partially Implemented | Admin cancellation موجود |
| Transactions | Database Only + Admin Read | لا Payment creation |
| Reviews | Database + Admin Read | إنشاء Review غير مكتمل |
| Complaints | Missing | لا Entity أو workflow |
| Career questionnaire | UI Only | لا حفظ |
| Ollama integration | Missing | غير مثبت أو مربوط |
| Career Path persistence | Database Only | Entities موجودة |
| Admin AI path review | Database Only | Status موجود دون workflow |
| Sequential unlocking | Database Only | ProgressStatus دون enforcement |
| Meaningful completion | Missing | لا tasks أو assessments |
| Free/Plus stages | Database Only | RequiredTier دون server enforcement |
| Admin analytics | Fully Implemented للبيانات المتوفرة | Queries فعلية |
| SignalR | Missing | لا package أو Hub |
| Zoom integration | Missing/Placeholder | مؤجل حتى استلام الكود |
| Automated tests | Missing | لا Test Project |

---

## 6. Mock Data والسلوك غير المرتبط

### نتائج مؤكدة

- صفحة Apply Expert تحتوي نموذج Preview ومهارات ثابتة ولا تحفظ طلبًا.
- `workflow-ui.js` يعرض رسائل Preview ولا ينفذ العمليات.
- `SessionsController` يعيد Views فقط.
- `CareerController` يعيد Views فقط.
- `zoom-meeting.js` يحتوي Zoom preview.
- بعض ملفات Admin JavaScript القديمة تحتوي Demo arrays ورسائل demo.
- وجود Entity لا يعني أن الصفحة تستخدمها.
- لم يتم العثور على استخدام `localStorage` أو `sessionStorage` في JavaScript الحالي.

### قاعدة معتمدة

أي بيانات وهمية متبقية يجب ألا تظهر كبيانات إنتاجية. عند عدم وجود بيانات حقيقية يجب عرض Empty State صادق بدل اختراع مستخدمين أو خبراء أو Reviews أو Resources.

---

## 7. تقييم Authentication وAuthorization

### نقاط جيدة

- Require Confirmed Email مفعّل.
- البريد فريد.
- Password minimum length ثمانية أحرف.
- Lockout بعد خمس محاولات فاشلة.
- POST actions الفعلية تستخدم Anti-forgery.
- ReturnUrl يُفحص باستخدام `Url.IsLocalUrl`.
- Admin routes محمية بدور Admin.
- Member routes محمية بدور Member.
- الأدمن لا يستطيع تغيير حالة حساب Admin من واجهة إدارة الأعضاء.
- تغيير حالة المستخدم يحدّث Security Stamp.

### الفجوات

- لا توجد Policy مركزية تفرض `AccountStatus.Active` على كل Request.
- Security Stamp قد لا يبطل الجلسة في اللحظة نفسها قبل دورة التحقق التالية.
- Profile completion يُفحص في Actions محددة وليس بسياسة شاملة.
- ConfirmEmail يسجل المستخدم مباشرة دون إعادة فحص AccountStatus.
- لا توجد Policies مثل:
  - ActiveMember.
  - CompleteProfile.
  - ActivePlus.
  - ApprovedExpertForSkill.
  - SessionParticipant.
- لا توجد Resource-based authorization للجلسات والمحادثات والحجوزات.

---

## 8. النتائج الأمنية

### High Severity

1. روابط Email Confirmation وPassword Reset الكاملة تُكتب في logs. إذا تسربت logs يمكن استخدام token للاستيلاء على الحساب. يجب حصرها بوضوح في Development أو استبدالها بخدمة بريد آمنة.
2. ConfirmEmail يسجل المستخدم بعد التأكيد دون التحقق من أن الحساب Active.
3. Plus entitlement وExpert credits وExpert approval وAvailability لا تُفرض حاليًا على الخادم لأنها غير منفذة.

### Medium Severity

1. لا توجد ownership checks للجلسات والمحادثات.
2. لا يوجد AccountStatus validation مركزي لكل request.
3. لا يوجد Rate Limiting واضح لـForgot Password وResend Confirmation.
4. Portfolio وSample URLs لا تملك validation واضحًا للبروتوكولات الآمنة.
5. أسباب قرارات حالة الحساب لا تُحفظ.
6. لا يوجد Audit Trail موحد للقرارات الإدارية.
7. Review eligibility لا تتحقق بعد من المشاركة الفعلية وإكمال الجلسة.

### Low Severity

1. بعض Services تستخدم نتائج نصية مثل Success وNotFound بدل Result type منظم.
2. بعض Controllers مضغوطة في أسطر طويلة مما يقلل قابلية المراجعة.
3. Views الأدمن ليست داخل بنية Area القياسية بالكامل.

---

## 9. حالة Migrations والبناء

### Migrations

- `InitialCreate`.
- `SeedCatalogueAndAuth`.
- Model Snapshot موجود.
- نتيجة الفحص: لا توجد تغييرات معلقة بين Model والـmigrations.

### Build

- Build succeeded.
- 0 Errors.
- ظهر تحذيران MSB3061 لأن ملفات DLL وEXE مقفلة بواسطة نسخة التطبيق المشغلة.
- لم يتم تشغيل التطبيق أثناء التدقيق.
- لم يتم تنفيذ database update.
- لم يتم إنشاء migration.

---

## 10. الترتيب المقترح للتنفيذ

1. Security Foundation.
2. Partner/Expert Discovery وEmpty States.
3. Learning Request workflow.
4. Shared Session Infrastructure.
5. SignalR Chat.
6. Plus Entitlement وExpert Credit Ledger.
7. Expert Application workflow.
8. Reviews وComplaints.
9. Career Questionnaire وOllama.
10. Career stage completion infrastructure.
11. Zoom integration بعد استلام الكود الموجود.
12. Automated tests وfinal hardening.

---

## 11. المكونات التي يجب إعادة استخدامها

- ASP.NET Core Identity الحالي.
- Member وAdmin roles.
- ApplicationUser.
- UserSkill وSkillIntent.
- LearningRequest وLearningSession.
- Message كأساس للمحادثة.
- ExpertApplication وExpertAvailability.
- Plan وSubscription وPayment.
- CareerPath وCareerPathStep.
- CatalogueService.
- AdminPeopleService.
- AdminOperationsService.
- Razor Views وCSS والتصميم الأصلي.
- NameFormatting.
- Slug generation.
- Status pages وAccess Denied.

---

## 12. تغييرات قاعدة البيانات المقترحة دون تطبيق

- AdminAuditLog.
- AccountStatusHistory.
- ExpertApplicationDecisionHistory أو ExpertApplicationAttempt.
- ExpertCreditLedger.
- ExpertBooking أو توسيع واضح للجلسة.
- Complaint/Report.
- LearningResource.
- CareerPathQuestionnaire.
- StageObjective.
- StageChecklistItem.
- PracticalTask.
- Quiz وQuizQuestion وQuizAttempt.
- CompletionRule أو AssessmentStrategy قابل للتهيئة بدل hardcoded rules.
- قيود تمنع Active subscriptions المتداخلة.
- علاقات واضحة للتحقق من أطراف Message وReview.

هذه مقترحات فقط ولم تُطبق.

---

## 13. القرارات المفتوحة

1. هل Plus مطلوب فقط وقت تقديم Expert Application أم يجب أن يبقى فعالًا بعد الاعتماد؟
2. كيف يحصل الخبير على التعويض: دفعات حقيقية، رصيد داخلي، أم نموذج عرض للمشروع؟
3. متى يُخصم Expert Credit: عند طلب الحجز، قبول الخبير، أم تثبيت الموعد؟
4. متى يُعاد الرصيد عند الإلغاء أو No-show؟
5. ما مصادر Learning Resources المعتمدة؟
6. هل يراجع الأدمن كل AI Career Path أم فقط الحالات المعلّمة للمراجعة؟
7. هل إعادة تقديم Expert Application تنشئ Attempt جديدًا؟
8. ما سياسة إخفاء Review المخالف مع الحفاظ على التاريخ؟
9. ما تعريف الجلسة Completed والمؤهلة للتقييم؟
10. كيف تُراجع Practical Tasks مع السماح بالتعلم الذاتي؟

---

## 14. التوصية للمرحلة التالية

### Phase 02 — Security Foundation and Partner/Expert Discovery

النطاق المقترح:

- منع logging غير الآمن للـtokens.
- فحص AccountStatus عند ConfirmEmail.
- إنشاء سياسات ActiveMember وCompleteProfile.
- تصميم Resource Authorization للجلسات.
- تنفيذ Partner query حسب المهارة.
- تنفيذ Expert query حسب المهارة والاعتماد.
- تنفيذ Empty States والبدائل الحقيقية.
- عدم استخدام بيانات وهمية.
- عدم تنفيذ Zoom أو Ollama بعد.
- عدم hardcode لأي شروط تقييم مؤقتة.

بعد نجاح هذه المرحلة يبدأ تنفيذ Learning Request workflow، ثم البنية المشتركة للجلسات والمحادثة.

---

## 15. الخلاصة

المشروع يمتلك أساسًا جيدًا وقابلًا للتطوير، خصوصًا في Identity والكتالوج ونموذج المهارات وإدارة الأدمن. أكبر فجوة ليست في عدد الصفحات، بل في ربط صفحات المستخدم بمنطق Server حقيقي مع ownership checks والاشتراكات والتقدم. النهج الصحيح هو إكمال السايكل الوظيفي تدريجيًا، مع الحفاظ على التصميم الحالي وعدم الاعتماد على JavaScript لتنفيذ قواعد العمل الأساسية.

انتهى تقرير Phase 01. لا يبدأ أي تغيير تنفيذي قبل الموافقة على Phase 02.
