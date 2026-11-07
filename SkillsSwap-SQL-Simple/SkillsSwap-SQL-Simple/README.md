# SkillsSwap simplified database

Run `SkillsSwap_Simple.sql` in SSMS (F5). It creates the `SkillsSwap` database. If the tables already exist, it stops and changes nothing.

Demo accounts (password `SkillsSwap_Demo!2026`): sara.free, omar.plus, lana.expert, ahmad.partner, admin (all `@example.test`).

## Tables (19 + 7 standard Identity tables)

| Area | Tables |
|---|---|
| Accounts | AspNetUsers (with profile columns), AspNetRoles (Member, Admin), AspNetUserRoles, plus 4 empty Identity tables |
| Catalogue | Categories, SubCategories, Skills |
| Member skills | UserSkills (Teach / Learn) |
| Experts | ExpertApplications (an Approved row is the expert badge for that skill), ExpertAvailability |
| Plans and money | Plans, Subscriptions, Payments, CreditTransactions (credit balance = SUM of Credits) |
| Learning | Requests (Exchange / Volunteer / Expert), Sessions, SessionChangeRequests |
| Chat and reviews | Messages (linked to the request), Reviews (one per person per request) |
| Career path | CareerGoals, CareerGoalSteps, CareerPaths (one per member), CareerPathProgress |

Dashboard and report numbers (ratings, swap counts, users per skill, revenue) are calculated with queries, not stored.

## Removed from the previous script

UserProfiles (merged into AspNetUsers), UserAvailability, ExpertSkills, PlanPrices, ExpertCreditGrants, ExpertCreditReservations, SessionConfirmations, Conversations, ConversationParticipants, PaymentProviderEvents, PaymentRefunds, CareerPathKnownSkills, rowversion columns, and Arabic/English duplicate name columns.

## Connecting from ASP.NET Core

Use `IdentityUser<Guid>` and `IdentityRole<Guid>`, and add `FullName`, `Location`, `Bio`, `AvatarPath`, `AccountStatus`, `StatusReason` and `CreatedAt` to the user class. Scaffold or map the existing tables instead of running an initial migration that would create them again.
