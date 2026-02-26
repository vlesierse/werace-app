import React from 'react';
import { StatusBar } from 'expo-status-bar';
import { PaperProvider } from 'react-native-paper';
import { NavigationContainer } from '@react-navigation/native';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import { ThemeContext, useThemeProvider } from './src/theme';
import AppNavigator from './src/navigation/AppNavigator';

export default function App() {
  const themeCtx = useThemeProvider();

  return (
    <SafeAreaProvider>
      <ThemeContext.Provider value={themeCtx}>
        <PaperProvider theme={themeCtx.paperTheme}>
          <NavigationContainer theme={themeCtx.navigationTheme}>
            <AppNavigator />
            <StatusBar style={themeCtx.isDark ? 'light' : 'dark'} />
          </NavigationContainer>
        </PaperProvider>
      </ThemeContext.Provider>
    </SafeAreaProvider>
  );
}
