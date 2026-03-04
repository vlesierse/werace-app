import React from 'react';
import { StyleSheet, View } from 'react-native';
import {
  Divider,
  List,
  RadioButton,
  Surface,
  Switch,
  Text,
} from 'react-native-paper';
import { useAppTheme, type ThemeMode } from '../theme';

export default function SettingsScreen() {
  const { theme, themeMode, isDark, setThemeMode, toggleTheme } = useAppTheme();

  return (
    <Surface style={[styles.container, { backgroundColor: theme.colors.background }]}>
      <View style={styles.content}>
        <Text variant="headlineMedium" style={styles.heading}>
          Settings
        </Text>

        <Divider style={styles.divider} />

        {/* Quick toggle */}
        <List.Item
          title="Dark Mode"
          description="Toggle between light and dark theme"
          left={(props) => <List.Icon {...props} icon="brightness-6" />}
          right={() => (
            <Switch value={isDark} onValueChange={toggleTheme} color={theme.colors.primary} />
          )}
        />

        <Divider style={styles.divider} />

        {/* Explicit mode selector */}
        <Text variant="titleMedium" style={styles.sectionTitle}>
          Theme preference
        </Text>

        <RadioButton.Group
          onValueChange={(value) => setThemeMode(value as ThemeMode)}
          value={themeMode}
        >
          <RadioButton.Item label="System default" value="system" />
          <RadioButton.Item label="Light" value="light" />
          <RadioButton.Item label="Dark" value="dark" />
        </RadioButton.Group>
      </View>
    </Surface>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
  content: {
    flex: 1,
    paddingTop: 16,
  },
  heading: {
    paddingHorizontal: 16,
    paddingBottom: 8,
  },
  divider: {
    marginVertical: 8,
  },
  sectionTitle: {
    paddingHorizontal: 16,
    paddingVertical: 8,
  },
});
