import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from 'react';
import { useColorScheme } from 'react-native';
import {
  MD3DarkTheme,
  MD3LightTheme,
  adaptNavigationTheme,
} from 'react-native-paper';
import type { MD3Theme } from 'react-native-paper';
import {
  DarkTheme as NavigationDarkTheme,
  DefaultTheme as NavigationDefaultTheme,
} from '@react-navigation/native';
import type { Theme as NavigationTheme } from '@react-navigation/native';
import AsyncStorage from '@react-native-async-storage/async-storage';

// ---------------------------------------------------------------------------
// F1-inspired color palette
// ---------------------------------------------------------------------------

const f1Colors = {
  primary: '#E10600', // F1 red
  onPrimary: '#FFFFFF',
  primaryContainer: '#FFDAD4',
  onPrimaryContainer: '#410000',

  secondary: '#1E1E1E', // Carbon black
  onSecondary: '#FFFFFF',
  secondaryContainer: '#3D3D3D',
  onSecondaryContainer: '#E0E0E0',

  tertiary: '#15151E', // F1 dark blue
  onTertiary: '#FFFFFF',
  tertiaryContainer: '#2C2C3E',
  onTertiaryContainer: '#D0D0E8',

  error: '#BA1A1A',
  onError: '#FFFFFF',
  errorContainer: '#FFDAD6',
  onErrorContainer: '#410002',
} as const;

// ---------------------------------------------------------------------------
// Light theme
// ---------------------------------------------------------------------------

export const lightTheme: MD3Theme = {
  ...MD3LightTheme,
  colors: {
    ...MD3LightTheme.colors,
    primary: f1Colors.primary,
    onPrimary: f1Colors.onPrimary,
    primaryContainer: f1Colors.primaryContainer,
    onPrimaryContainer: f1Colors.onPrimaryContainer,
    secondary: '#4A4458',
    onSecondary: '#FFFFFF',
    secondaryContainer: '#CBC2DB',
    onSecondaryContainer: '#332D41',
    tertiary: f1Colors.tertiary,
    onTertiary: f1Colors.onTertiary,
    tertiaryContainer: f1Colors.tertiaryContainer,
    onTertiaryContainer: f1Colors.onTertiaryContainer,
    error: f1Colors.error,
    onError: f1Colors.onError,
    errorContainer: f1Colors.errorContainer,
    onErrorContainer: f1Colors.onErrorContainer,
    background: '#FFFBFF',
    onBackground: '#1C1B1F',
    surface: '#FFFBFF',
    onSurface: '#1C1B1F',
    surfaceVariant: '#F4F0F4',
    onSurfaceVariant: '#49454F',
    outline: '#7A757F',
    outlineVariant: '#CAC4D0',
  },
};

// ---------------------------------------------------------------------------
// Dark theme
// ---------------------------------------------------------------------------

export const darkTheme: MD3Theme = {
  ...MD3DarkTheme,
  colors: {
    ...MD3DarkTheme.colors,
    primary: '#FF5449',
    onPrimary: '#690100',
    primaryContainer: '#930100',
    onPrimaryContainer: '#FFDAD4',
    secondary: '#CBC2DB',
    onSecondary: '#332D41',
    secondaryContainer: '#4A4458',
    onSecondaryContainer: '#E7DFF8',
    tertiary: '#BFC2FF',
    onTertiary: '#1E2178',
    tertiaryContainer: '#363990',
    onTertiaryContainer: '#E0E0FF',
    error: '#FFB4AB',
    onError: '#690005',
    errorContainer: '#93000A',
    onErrorContainer: '#FFDAD6',
    background: '#15151E',
    onBackground: '#E6E1E5',
    surface: '#15151E',
    onSurface: '#E6E1E5',
    surfaceVariant: '#2C2C35',
    onSurfaceVariant: '#CAC4D0',
    outline: '#948F99',
    outlineVariant: '#49454F',
  },
};

// ---------------------------------------------------------------------------
// Navigation-compatible themes (adapted from Paper colors)
// ---------------------------------------------------------------------------

const { LightTheme: navLightTheme, DarkTheme: navDarkTheme } = adaptNavigationTheme({
  reactNavigationLight: NavigationDefaultTheme,
  reactNavigationDark: NavigationDarkTheme,
  materialLight: lightTheme,
  materialDark: darkTheme,
});

export { navLightTheme, navDarkTheme };

export type AppTheme = MD3Theme;

// ---------------------------------------------------------------------------
// Theme preference persistence
// ---------------------------------------------------------------------------

const THEME_STORAGE_KEY = '@werace/theme-mode';

export type ThemeMode = 'system' | 'light' | 'dark';

async function loadThemeMode(): Promise<ThemeMode> {
  try {
    const stored = await AsyncStorage.getItem(THEME_STORAGE_KEY);
    if (stored === 'light' || stored === 'dark' || stored === 'system') {
      return stored;
    }
  } catch {
    // Silently fall back to system default
  }
  return 'system';
}

async function saveThemeMode(mode: ThemeMode): Promise<void> {
  try {
    await AsyncStorage.setItem(THEME_STORAGE_KEY, mode);
  } catch {
    // Best-effort persistence
  }
}

// ---------------------------------------------------------------------------
// Theme context
// ---------------------------------------------------------------------------

export interface ThemeContextValue {
  paperTheme: MD3Theme;
  navigationTheme: NavigationTheme;
  themeMode: ThemeMode;
  isDark: boolean;
  setThemeMode: (mode: ThemeMode) => void;
  toggleTheme: () => void;
  /** Shortcut — same as paperTheme, for convenience in screens */
  theme: MD3Theme;
}

export const ThemeContext = createContext<ThemeContextValue>({
  paperTheme: lightTheme,
  navigationTheme: NavigationDefaultTheme,
  theme: lightTheme,
  themeMode: 'system',
  isDark: false,
  setThemeMode: () => {},
  toggleTheme: () => {},
});

export const useAppTheme = () => useContext(ThemeContext);

// ---------------------------------------------------------------------------
// Hook: useThemeProvider — call once in the root <App />
// ---------------------------------------------------------------------------

export function useThemeProvider(): ThemeContextValue {
  const systemColorScheme = useColorScheme();
  const [themeMode, setThemeModeState] = useState<ThemeMode>('system');
  const [loaded, setLoaded] = useState(false);

  // Load persisted preference on mount
  useEffect(() => {
    loadThemeMode().then((mode) => {
      setThemeModeState(mode);
      setLoaded(true);
    });
  }, []);

  const setThemeMode = useCallback((mode: ThemeMode) => {
    setThemeModeState(mode);
    saveThemeMode(mode);
  }, []);

  const isDark = useMemo(() => {
    if (themeMode === 'system') {
      return systemColorScheme === 'dark';
    }
    return themeMode === 'dark';
  }, [themeMode, systemColorScheme]);

  const toggleTheme = useCallback(() => {
    setThemeMode(isDark ? 'light' : 'dark');
  }, [isDark, setThemeMode]);

  const theme = isDark ? darkTheme : lightTheme;
  const navigationTheme = isDark ? navDarkTheme : navLightTheme;

  return useMemo(
    () => ({
      paperTheme: theme,
      navigationTheme,
      theme,
      themeMode,
      isDark,
      setThemeMode,
      toggleTheme,
    }),
    [theme, navigationTheme, themeMode, isDark, setThemeMode, toggleTheme],
  );
}
