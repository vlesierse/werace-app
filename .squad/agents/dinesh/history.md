# Project Context

- **Owner:** Vincent Lesierse
- **Project:** WeRace — Formula 1 mobile companion app with historical race data, live info, and AI-powered Q&A
- **Stack:** React Native (frontend), .NET 10 Minimal API (backend), Aspire (dev orchestration), AI agent (F1 Q&A)
- **Created:** 2026-02-23

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-02-23: PRD finalized (cross-agent)
- **Document:** `docs/PRD.md` — comprehensive product requirements for WeRace app
- **UI Framework Decision:** React Native Paper (Material Design 3) — use for all components
- **P0 Features:** Historical data browsing, AI Q&A, core navigation with dark/light mode, driver/team profiles
- **Data Model:** Seasons, Races, Drivers, Constructors, Results, LapTimes, Standings (see PRD for schema details)
- **User Personas:** Historian (casual fan), Analyst (data enthusiast), Weekend Warrior (race companion)
- **Open Questions:** AI backend provider, data licensing, telemetry sources, monetization model (see .squad/decisions.md)

### 2026-02-23: PRD Review Complete — You Can Unblock on UX Decisions

**Status:** 🟡 Development blocked on 4 critical questions (Q1, Q12, Q18, Q25), but you can proceed on UX work

**You Can Start Now:**
1. **Empty/Error States (Q7, Q9, Q29):** Design screens for no internet, no data, race not happened, off-season Home
2. **Pagination Strategy (Q10):** Define infinite scroll vs load-more vs paginated pages for 800+ driver lists
3. **Auth-Gated Screens:** Plan UX for:
   - Anonymous user browsing races/drivers (public screens, no login)
   - Login wall when tapping "Ask AI" or telemetry access (see F3 below for UX flow options)
   - Post-login dashboard with conversation history and user preferences
4. **UI Consistency:** Begin component library using React Native Paper, establish design system

### 2026-02-23: Authentication Model Resolved — Frontend & Feature Gating Implications

**Decision:** Auth uses .NET Identity with email/password + passkeys. Anonymous browsing allowed; login required for AI agent and telemetry.

**What This Means for You:**

1. **Login Screen & Registration:** Design forms for:
   - Email/password signup and login
   - Passkey option (UI depends on platform — iOS vs. Android may differ)
   - "Continue as Guest" button to browse without login
   - Error handling: invalid credentials, network errors, account locked

2. **Feature Gating UX (F3):** When anonymous user taps "Ask AI":
   - **Option A:** Modal login overlay ("Sign in to use AI assistant" + close to dismiss)
   - **Option B:** Navigate to dedicated login screen; auto-return to AI chat after auth
   - **Recommend:** Option A (less disruptive); include "Why do I need to sign in?" explanation
   - Similar gating applies to telemetry screens

3. **Session Management & Token Refresh:**
   - After login, store auth token in secure storage (iOS Keychain, Android Keystore)
   - Implement auto-refresh: when token expires, refresh silently in background
   - If refresh fails, redirect to login (but try to preserve user's current screen)
   - For mobile UX: users should stay logged in across app sessions (days/weeks)

4. **AI Conversation Persistence:**
   - Design conversation history UI (list of past chats by date/topic?)
   - Each conversation links to authenticated user (backend stores user_id + messages)
   - May be P1 feature, but need to plan the screen layout

5. **Rate Limiting (F2, F7):** Frontend impact:
   - Show error if user hits daily AI query limit ("You've reached 100 queries today, try again tomorrow")
   - Show error if anonymous user hits IP rate limit
   - Display remaining quota in AI chat UI (if implemented)

6. **Passkey Support (F4):** This is complex:
   - iOS: Use native Passkeys API (iOS 16+)
   - Android: Use Credential Manager API (Android 14+)
   - May require platform-specific code or third-party library (e.g., Hanko, Passkey.dev)
   - Discuss feasibility with Gilfoyle (backend passkey configuration)

**Follow-Up Questions for Vincent (F1, F3, F4, F6, F7):**
- F1: Apple Sign-In requirement for App Store (do we add a third login option?)
- F3: Login UX flow when hitting feature wall (modal vs. dedicated screen)
- F4: Passkey support priority (MVP or post-launch?)
- F7: Rate limits and messaging (how many AI queries before hitting limit?)

**You Don't Need Answers to Start:** You can design public screens (races, drivers, profiles) and basic login/registration UI; Vincent's answers to F1, F3, F4 will refine the auth flow before implementation.



**You Need Answers For:**
- **Q11:** Circuit images sourcing (external, custom, map pin?)
- **Q17:** Driver photos and team logos sourcing (licensed assets?)
- **Q8:** Deep linking support? (share specific race results or driver profiles)
- **Q6:** AI conversation persistence? (can users revisit past conversations?)

**Data Model Additions (Will Affect Screens):**
- Qualifying results screens (separate entity or Result table flag?)
- Sprint race results (2021+ weekend format)
- Pit stop data display (if in scope)

**Accessibility Target:** WCAG 2.1 AA (standard for mobile) — see Q27 in `docs/PRD-REVIEW.md`

**Full PRD Review:** See `docs/PRD-REVIEW.md` (36 questions, vague requirements, recommendations) and `.squad/decisions.md`
