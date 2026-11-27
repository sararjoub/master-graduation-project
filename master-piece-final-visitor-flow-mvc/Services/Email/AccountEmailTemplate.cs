using System.Net;

namespace SkillsSwap.Services.Email;

public sealed record AccountEmailTemplate(string Subject, string Html, string Text)
{
    public const string LogoContentId = "skillswap-brand-mark";

    public static AccountEmailTemplate Create(AccountEmailKind kind, string actionUrl, string language, TimeSpan lifespan)
    {
        var rtl = language == "ar";
        var copy = GetCopy(kind, rtl);
        var minutes = (int)Math.Floor(lifespan.TotalMinutes);
        var duration = minutes % 60 == 0
            ? (rtl ? (minutes == 60 ? "ساعة واحدة" : $"{minutes / 60} ساعة") : $"{minutes / 60} {(minutes == 60 ? "hour" : "hours")}")
            : (rtl ? $"{minutes} دقيقة" : $"{minutes} minutes");
        var expiration = rtl ? $"تنتهي صلاحية الرابط بعد {duration}." : $"This link expires in {duration}.";
        var tagline = rtl ? "تعلّم مهارة وشارك ما تعرفه." : "Learn a skill. Share what you know.";
        var direction = rtl ? "rtl" : "ltr";
        var alignment = rtl ? "right" : "left";
        var fonts = rtl ? "Tahoma,Arial,sans-serif" : "Arial,Segoe UI,sans-serif";
        var url = Encode(actionUrl);
        var year = DateTime.UtcNow.Year;

        var html = $"""
            <!doctype html>
            <html lang="{(rtl ? "ar" : "en")}" dir="{direction}">
            <head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"></head>
            <body style="margin:0;padding:0;background:#f5f3ff;color:#211637;font-family:{fonts}">
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#f5f3ff">
            <tr><td align="center" style="padding:24px 12px">
            <!--[if mso]><table role="presentation" width="600" align="center"><tr><td><![endif]-->
            <table role="presentation" dir="{direction}" width="100%" cellspacing="0" cellpadding="0" style="max-width:600px;background:#ffffff;border:1px solid #e7ddfb;border-radius:18px">
            <tr><td align="{alignment}" style="padding:22px 28px;background:#f9f5ff;border-bottom:1px solid #e7ddfb;border-radius:18px 18px 0 0">
            <table role="presentation" dir="ltr" cellspacing="0" cellpadding="0"><tr>
            <td align="center" valign="middle" width="42" height="42" style="background:#ede4ff;border-radius:50%">
            <img src="cid:{LogoContentId}" width="32" height="32" alt="Skill Swap" style="display:block;border:0;margin:0 auto">
            </td><td style="padding-left:12px;font:700 25px Georgia,serif;color:#6d3bda">Skill Swap</td>
            </tr></table>
            </td></tr>
            <tr><td align="{alignment}" style="padding:28px;font-family:{fonts}">
            <table role="presentation" cellspacing="0" cellpadding="0"><tr><td align="center" width="40" height="40"
            style="border-radius:10px;background:#ede4ff;color:#512b9f;font-size:22px;font-weight:bold"><span aria-hidden="true">{Encode(copy.Indicator)}</span></td></tr></table>
            <h1 style="margin:14px 0 10px;font-size:28px;line-height:1.3;color:#211637">{EncodeBrandText(copy.Heading, rtl)}</h1>
            <p style="margin:0 0 20px;font-size:16px;line-height:1.65;color:#3c3052">{EncodeBrandText(copy.Message, rtl)}</p>
            <table role="presentation" cellspacing="0" cellpadding="0"><tr><td bgcolor="#8b7cf6" style="border-radius:9px;mso-padding-alt:14px 22px">
            <a href="{url}" style="display:inline-block;padding:14px 22px;mso-padding-alt:0;color:#1d1335;font-size:16px;font-weight:bold;text-decoration:none;line-height:1.25">{Encode(copy.Action)}</a>
            </td></tr></table>
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="margin-top:22px"><tr>
            <td align="{alignment}" bgcolor="#f5f0ff" style="padding:11px 14px;border-radius:8px;font-size:14px;line-height:1.5;color:#352453">{Encode(expiration)}</td>
            </tr></table>
            <p style="margin:17px 0 0;font-size:14px;line-height:1.6;color:#3c3052">{Encode(copy.FallbackPrompt)}
            <a href="{url}" style="color:#5a2db5;text-decoration:underline">{Encode(copy.FallbackAction)}</a></p>
            </td></tr>
            <tr><td align="{alignment}" style="padding:20px 28px;background:#f9f5ff;border-top:1px solid #e7ddfb;border-radius:0 0 18px 18px;font-family:{fonts};color:#46375d;font-size:13px;line-height:1.65">
            <p style="margin:0 0 8px">{EncodeBrandText(copy.Security, rtl)}</p>
            <p style="margin:0"><strong>Skill Swap</strong> · {Encode(tagline)}</p>
            <p style="margin:4px 0 0"><span dir="ltr">© {year} Skill Swap</span></p>
            </td></tr></table>
            <!--[if mso]></td></tr></table><![endif]-->
            </td></tr></table>
            </body></html>
            """;
        var text = $"{copy.Heading}\n\n{copy.Message}\n\n{copy.Action}: {actionUrl}\n\n{expiration}\n{copy.Security}\n\nSkill Swap · {tagline}\n© {year} Skill Swap";
        return new(copy.Subject, html, text);
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);

    private static string EncodeBrandText(string value, bool rtl) =>
        rtl ? Encode(value).Replace("Skill Swap", "<span dir=\"ltr\" style=\"white-space:nowrap\">Skill Swap</span>") : Encode(value);

    private sealed record Copy(string Subject, string Heading, string Message, string Action,
        string Indicator, string FallbackPrompt, string FallbackAction, string Security);

    private static Copy GetCopy(AccountEmailKind kind, bool rtl) => (kind, rtl) switch
    {
        (AccountEmailKind.Verify, false) => new("Verify your Skill Swap email", "Welcome to Skill Swap!",
            "Verify your email address to activate your account and start learning with others.",
            "Verify My Email Address", "✓", "Having trouble with the button?", "Open the verification link.",
            "If you did not create a Skill Swap account, ignore this email and keep the link private."),
        (AccountEmailKind.Resend, false) => new("New Skill Swap verification link", "Verify Your Email Address",
            "We received a request for another verification link. Use it to finish setting up your Skill Swap account.",
            "Verify My Email Address", "↻", "Having trouble with the button?", "Open the verification link.",
            "If you did not request another link, ignore this email and keep the link private."),
        (AccountEmailKind.Reset, false) => new("Reset your Skill Swap password", "Reset Your Password",
            "We received a request to reset your Skill Swap password. Use this link to choose a new password.",
            "Reset My Password", "↺", "Having trouble with the button?", "Open the password reset link.",
            "If you did not request a password reset, ignore this email and keep the link private."),
        (AccountEmailKind.Verify, true) => new("أكّد بريدك في Skill Swap", "أهلًا بك في Skill Swap",
            "أكّد بريدك الإلكتروني لتفعيل حسابك والبدء بالتعلّم ومشاركة المعرفة مع الآخرين.",
            "تأكيد بريدي الإلكتروني", "✓", "هل تواجه مشكلة في استخدام الزر؟", "افتح رابط تأكيد البريد.",
            "إذا لم تنشئ حسابًا في Skill Swap، تجاهل هذه الرسالة ولا تشارك الرابط."),
        (AccountEmailKind.Resend, true) => new("رابط تأكيد جديد من Skill Swap", "تأكيد بريدك الإلكتروني",
            "تلقّينا طلبًا لإرسال رابط تأكيد جديد. استخدم الرابط لإكمال إعداد حسابك في Skill Swap.",
            "تأكيد بريدي الإلكتروني", "↻", "هل تواجه مشكلة في استخدام الزر؟", "افتح رابط تأكيد البريد.",
            "إذا لم تطلب رابطًا جديدًا، تجاهل هذه الرسالة ولا تشارك الرابط."),
        _ => new("إعادة تعيين كلمة المرور في Skill Swap", "إعادة تعيين كلمة المرور",
            "تلقّينا طلبًا لإعادة تعيين كلمة مرور حسابك في Skill Swap. استخدم الرابط لاختيار كلمة مرور جديدة.",
            "إعادة تعيين كلمة المرور", "↺", "هل تواجه مشكلة في استخدام الزر؟", "افتح رابط إعادة تعيين كلمة المرور.",
            "إذا لم تطلب إعادة تعيين كلمة المرور، تجاهل هذه الرسالة ولا تشارك الرابط.")
    };
}
