export const ReminderType = {
  Minutes15: 0,
  Minutes30: 1,
  OneDayBefore: 2,
} as const;

export type ReminderType = typeof ReminderType[keyof typeof ReminderType];

export interface ReminderDto {
  id: string;
  eventId: string;
  eventTitle: string;
  eventStartDate: string;
  eventCategoryColor: string;
  reminderType: ReminderType;
  customTimeHours?: number;
  customTimeMinutes?: number;
  triggerDateTime: string;
  isSent: boolean;
  createdAt: string;
}

export interface CreateReminderDto {
  eventId: string;
  reminderType: ReminderType;
  customTimeHours?: number;
  customTimeMinutes?: number;
}

export type NotificationPermissionState = 'default' | 'granted' | 'denied';
