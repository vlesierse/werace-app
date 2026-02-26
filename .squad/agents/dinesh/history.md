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

### 2026-02-26: All 4 Critical Blockers Resolved — Development Unblocked (cross-agent)

**Status:** ✅ All critical blockers resolved. Phase 1 sprint planning complete. Sprint 1 starts now.

**Resolved Blockers:**
- Q25 ✅ Authentication: .NET Identity, email/password + passkeys, anonymous browsing
- Q12 ✅ Data Source: Jolpica replaces Ergast, database dump import
- Q18 ✅ AI Safety: Defense-in-depth (4-layer stack), 50/day, historical-only — implemented in Phase 2
- Q1 ✅ MVP Scope: Phase 1 (5 weeks) data + auth, Phase 2 (2-3 weeks) AI

### 2026-02-26: Phase 1 Plan — Your Assignments

**Plan:** `docs/PHASE1-PLAN.md` (5 sprints × 1 week, 6 epics)
**Technical Foundation:** `docs/TECHNICAL-FOUNDATION.md` (solution structure, DDL, API contracts)

**Your Sprint-by-Sprint Work:**

| Sprint | Tasks | Epic | Days |
|--------|-------|------|------|
| S1 | React Native app + Paper + nav shell, theme system (dark/light) | E1 | 3 |
| S2 | Home screen (connected to API), season list + season detail screens | E5 | 5 |
| S3 | Race detail screen (results/qualifying/laps tabs), standings screen | E5 | 4 |
| S4 | Login + registration screens, driver/constructor profiles, search screen | E4/E5 | 6 |
| S5 | Empty/error/loading states, off-season home, accessibility pass, circuit detail | E5 | 6 |

**Key Decisions That Affect You:**
1. **Mock data decoupling:** Build UI against API contracts, not live endpoints. API contracts published S1 by Richard. Build S2 screens with JSON fixtures, connect to real API as endpoints land.
2. **Client-side search:** Phase 1 search is client-side filtering (datasets < 1000 items). Backend search evaluated in Phase 2.
3. **Passkeys:** Best-effort. Ship email/password in S4. Passkey UI depends on React Native FIDO2 feasibility — spike in S3 (Q25-F4 is your open item).
4. **React Native Paper:** Material Design 3 theme. Bottom tab navigation: Home, Seasons, Drivers, Constructors, Settings.
5. **Pagination:** Offset-based, pageSize=20, max 100. Implement load-more on scroll for all list views.
6. **JSON envelope:** All API responses: `{ "data": [...], "pagination": {...} }` or `{ "error": {...} }`.

**Your Open Items:**
- Q25-F4: Passkey feasibility in React Native (decide by S3)

**Dependencies:**
- Blocked on Gilfoyle for live API data (mitigated by mock data)
- API contracts from Richard in S1 define your screen data shapes
- Monica provides acceptance criteria for empty/error states (Q7, Q9) by S3

### 2026-02-26: E1 Project Scaffolding — Frontend Complete

**What was done:**
- Initialized Expo SDK 55 (React 19, React Native 0.83) app under `src/app/`
- Installed and configured React Native Paper v5 (Material Design 3) with F1-inspired color palette
- Set up React Navigation v7 bottom tabs with 5 screens: Home, Seasons, Drivers, Constructors, Settings
- Built theme system with light/dark/system modes, AsyncStorage persistence, and manual toggle
- Bottom navigation uses Paper's `BottomNavigation.Bar` as custom tab bar — full MD3 styling including active indicators
- TypeScript strict mode, zero compilation errors

**Key architectural decisions:**
1. **Separate Paper + Navigation themes:** PaperProvider gets the Paper theme, NavigationContainer gets its own adapted navigation theme. Merging them into a single object caused font type conflicts. `adaptNavigationTheme()` bridges Paper colors into Navigation's color slots.
2. **ThemeContext pattern:** A single `useThemeProvider()` hook manages state, persistence, and system detection. Screens consume via `useAppTheme()`. This keeps the root `App.tsx` thin.
3. **Tab config as data:** Navigation tabs are defined as a typed array and mapped to `Tab.Screen` components — easy to add/remove tabs without touching navigation logic.
4. **Icons via `@expo/vector-icons`:** Using `MaterialCommunityIcons` from `@expo/vector-icons` rather than raw `react-native-vector-icons` for Expo compatibility.

**Key file paths:**
- `src/app/App.tsx` — Root component (providers: SafeArea → ThemeContext → PaperProvider → NavigationContainer)
- `src/app/src/theme/index.ts` — Theme definitions, context, persistence hook
- `src/app/src/navigation/AppNavigator.tsx` — Bottom tab navigator with Paper integration
- `src/app/src/screens/` — Five placeholder screens (Settings has theme toggle)
- `src/app/app.json` — Expo config (renamed to WeRace, `userInterfaceStyle: automatic`)

**Stack versions:** Expo 55, React 19.2, React Native 0.83.2, Paper 5.15, Navigation 7.1, AsyncStorage 3.0

### 2026-02-26: E1 Scaffolding Complete — Cross-Agent Updates

**Gilfoyle (Backend):** .NET 10 backend created at `src/api/`. Three projects: AppHost (Aspire orchestrator with PostgreSQL + Redis), ServiceDefaults, Api. Solution uses `.slnx` format. Start everything with `dotnet run --project src/api/WeRace.AppHost`.

**Jared (Testing):** Jest placeholder at `src/app/__tests__/App.test.tsx`. Real render test commented out, waiting for your App component. Test naming convention: `it('should [behavior] when [condition]')`. See `docs/TESTING.md`.

**PR #8** opened against `main` (closes #1). Branch: `squad/1-project-scaffolding`.

