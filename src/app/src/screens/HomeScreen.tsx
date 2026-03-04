import React from 'react';
import { StyleSheet, View } from 'react-native';
import { Surface, Text } from 'react-native-paper';
import { useAppTheme } from '../theme';

export default function HomeScreen() {
  const { theme } = useAppTheme();

  return (
    <Surface style={[styles.container, { backgroundColor: theme.colors.background }]}>
      <View style={styles.content}>
        <Text variant="headlineLarge" style={{ color: theme.colors.primary }}>
          WeRace
        </Text>
        <Text variant="bodyLarge" style={styles.subtitle}>
          Your Formula 1 Companion
        </Text>
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
    alignItems: 'center',
    justifyContent: 'center',
    padding: 24,
  },
  subtitle: {
    marginTop: 8,
    opacity: 0.7,
  },
});
