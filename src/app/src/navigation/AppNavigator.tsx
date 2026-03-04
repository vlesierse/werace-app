import React from 'react';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { CommonActions } from '@react-navigation/native';
import { BottomNavigation } from 'react-native-paper';
import { MaterialCommunityIcons } from '@expo/vector-icons';

import HomeScreen from '../screens/HomeScreen';
import SeasonsScreen from '../screens/SeasonsScreen';
import DriversScreen from '../screens/DriversScreen';
import ConstructorsScreen from '../screens/ConstructorsScreen';
import SettingsScreen from '../screens/SettingsScreen';

// ---------------------------------------------------------------------------
// Tab configuration
// ---------------------------------------------------------------------------

type TabConfig = {
  name: string;
  title: string;
  icon: keyof typeof MaterialCommunityIcons.glyphMap;
  component: React.ComponentType;
};

const tabs: TabConfig[] = [
  { name: 'Home', title: 'Home', icon: 'home', component: HomeScreen },
  { name: 'Seasons', title: 'Seasons', icon: 'calendar', component: SeasonsScreen },
  { name: 'Drivers', title: 'Drivers', icon: 'account-group', component: DriversScreen },
  { name: 'Constructors', title: 'Constructors', icon: 'car-sports', component: ConstructorsScreen },
  { name: 'Settings', title: 'Settings', icon: 'cog', component: SettingsScreen },
];

// ---------------------------------------------------------------------------
// Navigator
// ---------------------------------------------------------------------------

const Tab = createBottomTabNavigator();

export default function AppNavigator() {
  return (
    <Tab.Navigator
      screenOptions={{ headerShown: false }}
      tabBar={({ navigation, state, descriptors, insets }) => (
        <BottomNavigation.Bar
          navigationState={state}
          safeAreaInsets={insets}
          onTabPress={({ route, preventDefault }) => {
            const event = navigation.emit({
              type: 'tabPress',
              target: route.key,
              canPreventDefault: true,
            });

            if (event.defaultPrevented) {
              preventDefault();
            } else {
              navigation.dispatch({
                ...CommonActions.navigate(route.name, route.params),
                target: state.key,
              });
            }
          }}
          renderIcon={({ route, focused, color }) => {
            const { options } = descriptors[route.key];
            if (options.tabBarIcon) {
              return options.tabBarIcon({ focused, color, size: 24 });
            }
            return null;
          }}
          getLabelText={({ route }) => {
            const { options } = descriptors[route.key];
            const label =
              typeof options.tabBarLabel === 'string'
                ? options.tabBarLabel
                : options.title ?? route.name;
            return label;
          }}
        />
      )}
    >
      {tabs.map(({ name, title, icon, component }) => (
        <Tab.Screen
          key={name}
          name={name}
          component={component}
          options={{
            title,
            tabBarIcon: ({ color, size }) => (
              <MaterialCommunityIcons name={icon} color={color} size={size} />
            ),
          }}
        />
      ))}
    </Tab.Navigator>
  );
}
