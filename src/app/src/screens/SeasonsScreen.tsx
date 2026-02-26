import React from 'react';
import { StyleSheet, View } from 'react-native';
import { Surface, Text } from 'react-native-paper';
import { useAppTheme } from '../theme';

export default function SeasonsScreen() {
  const { theme } = useAppTheme();

  return (
    <Surface style={[styles.container, { backgroundColor: theme.colors.background }]}>
      <View style={styles.content}>
        <Text variant="headlineMedium">Seasons</Text>
        <Text variant="bodyLarge" style={styles.subtitle}>
          Browse Formula 1 seasons and race results
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
