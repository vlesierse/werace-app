# Decision: Frontend Scaffolding — E1

**Date:** 2026-02-26
**By:** Dinesh (Frontend Dev)
**Epic:** E1 — Project Scaffolding
**Issue:** #1

## Decisions Made

### 1. Expo SDK 55 (managed workflow)

**What:** Used Expo managed workflow with SDK 55, React 19.2, React Native 0.83.2.
**Why:** Latest stable Expo SDK. Managed workflow keeps the build pipeline simple — no native code ejection needed for current features. SDK 55 ships with React 19 which gives us concurrent features and the new JSX transform.

### 2. Separate Paper and Navigation themes

**What:** PaperProvider and NavigationContainer receive their own theme objects. Paper's `adaptNavigationTheme()` bridges colors.
**Why:** Merging Paper (MD3Typescale fonts) and Navigation (FontStyle fonts) into one object causes TypeScript type conflicts. Keeping them separate is cleaner and type-safe.
**Impact:** Screens use `useAppTheme()` → `theme` (Paper theme). Navigation's theme is only passed to `NavigationContainer` in `App.tsx`.

### 3. Paper BottomNavigation.Bar as custom tab bar

**What:** React Navigation's `createBottomTabNavigator` uses Paper's `BottomNavigation.Bar` via the `tabBar` prop.
**Why:** Gives us full MD3 styling (active indicators, ripple effects, theming) while keeping React Navigation's routing. This is the approach recommended by Paper's docs.
**Impact:** Tab config is defined as a typed data array — adding new tabs requires only a new entry in the array.

### 4. ThemeContext with AsyncStorage persistence

**What:** Theme preference (system/light/dark) stored in AsyncStorage, exposed via React Context.
**Why:** Users expect their theme choice to survive app restarts. System-default is the fallback. The Settings screen provides both a quick toggle and explicit mode selector (system/light/dark radio buttons).
**Impact:** Any screen can access and modify the theme via `useAppTheme()`.

### 5. MaterialCommunityIcons via @expo/vector-icons

**What:** Using `@expo/vector-icons/MaterialCommunityIcons` for all icons.
**Why:** Bundled with Expo (no native linking), extensive icon set that matches Paper's design language. The `react-native-vector-icons` package is installed as a Paper peer dependency but we don't import it directly.

## Team Impact

- **Gilfoyle:** No backend impact. Frontend is self-contained for now.
- **Jared:** Can add component/screen tests using the existing `__tests__/` directory. Jest types installed.
- **Richard:** Navigation structure (5 tabs) matches the PRD information architecture. New screen additions follow the pattern in `src/app/src/screens/`.
