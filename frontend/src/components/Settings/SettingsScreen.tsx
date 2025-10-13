import { Bell, BellOff, Clock, Globe, MessageSquare, Moon, Save, Settings, Sun, Tag, User } from 'lucide-react';
import React, { useEffect, useState } from 'react';
import { useTheme } from '../../context/ThemeContext';
import { apiService, type UpdateUserPreferencesDto, type UpdateUserProfileDto, type UserPreferencesDto, type UserProfileDto } from '../../services/apiService';
import { Button } from '../ui/button';
import { Card } from '../ui/card';
import { Input } from '../ui/input';
import { CategoryManagement } from './CategoryManagement';
import './SettingsScreen.css';

type TabId = 'profile' | 'preferences' | 'categories' | 'ai-nlp' | 'notifications';

interface Tab {
  id: TabId;
  label: string;
  icon: React.ComponentType<{ size?: number; className?: string }>;
}

const tabs: Tab[] = [
  { id: 'profile', label: 'Perfil', icon: User },
  { id: 'preferences', label: 'Preferencias', icon: Settings },
  { id: 'categories', label: 'Categorías', icon: Tag },
  { id: 'ai-nlp', label: 'IA & NLP', icon: MessageSquare },
  { id: 'notifications', label: 'Notificaciones', icon: Bell }
];

const TIMEZONES = [
  { value: 'America/Mexico_City', label: 'México (GMT-6)' },
  { value: 'America/New_York', label: 'Nueva York (GMT-5)' },
  { value: 'America/Chicago', label: 'Chicago (GMT-6)' },
  { value: 'America/Los_Angeles', label: 'Los Ángeles (GMT-8)' },
  { value: 'Europe/Madrid', label: 'Madrid (GMT+1)' },
  { value: 'Europe/London', label: 'Londres (GMT+0)' },
  { value: 'Asia/Tokyo', label: 'Tokio (GMT+9)' }
];

const WEEKDAYS = [
  { id: 1, label: 'Lunes', short: 'L' },
  { id: 2, label: 'Martes', short: 'M' },
  { id: 3, label: 'Miércoles', short: 'X' },
  { id: 4, label: 'Jueves', short: 'J' },
  { id: 5, label: 'Viernes', short: 'V' },
  { id: 6, label: 'Sábado', short: 'S' },
  { id: 0, label: 'Domingo', short: 'D' }
];

const SettingsScreen: React.FC = () => {
  const [activeTab, setActiveTab] = useState<TabId>('profile');
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState('');
  const [successMessage, setSuccessMessage] = useState('');
  const { setTheme } = useTheme();

  // Profile state
  const [profile, setProfile] = useState<UserProfileDto>({
    id: '',
    name: '',
    email: '',
    timezone: 'America/Mexico_City'
  });

  // Preferences state
  const [preferences, setPreferences] = useState<UserPreferencesDto>({
    id: '',
    userId: '',
    theme: 'light',
    language: 'es-ES',
    defaultReminderMinutes: 15,
    firstDayOfWeek: 1,
    timeFormat: '24h',
    dateFormat: 'dd/MM/yyyy',
    workStartTime: '09:00',
    workEndTime: '18:00',
    workDaysOfWeek: [1, 2, 3, 4, 5],
    exerciseDaysOfWeek: [],
    nlpKeywords: [],
    notificationsEnabled: true
  });

  const [newKeyword, setNewKeyword] = useState('');

  useEffect(() => {
    loadUserData();
  }, []);

  const loadUserData = async () => {
    try {
      setIsLoading(true);
      setError('');

      const [profileData, preferencesData] = await Promise.all([
        apiService.getUserProfile(),
        apiService.getUserPreferences()
      ]);

      setProfile(profileData);
      setPreferences(preferencesData);
    } catch (err) {
      console.error('Error loading user data:', err);
      setError('Error al cargar la configuración');
    } finally {
      setIsLoading(false);
    }
  };

  const handleSaveProfile = async () => {
    try {
      setIsSaving(true);
      setError('');
      setSuccessMessage('');

      const payload: UpdateUserProfileDto = {
        name: profile.name,
        email: profile.email,
        timezone: profile.timezone
      };

      const updatedProfile = await apiService.updateUserProfile(payload);
      setProfile(updatedProfile);
      setSuccessMessage('Perfil actualizado correctamente');

      setTimeout(() => setSuccessMessage(''), 3000);
    } catch (err) {
      console.error('Error saving profile:', err);
      setError('Error al guardar el perfil');
    } finally {
      setIsSaving(false);
    }
  };

  const handleSavePreferences = async () => {
    try {
      setIsSaving(true);
      setError('');
      setSuccessMessage('');

      const payload: UpdateUserPreferencesDto = {
        theme: preferences.theme,
        language: preferences.language,
        defaultReminderMinutes: preferences.defaultReminderMinutes,
        firstDayOfWeek: preferences.firstDayOfWeek,
        timeFormat: preferences.timeFormat,
        dateFormat: preferences.dateFormat,
        workStartTime: preferences.workStartTime,
        workEndTime: preferences.workEndTime,
        workDaysOfWeek: preferences.workDaysOfWeek,
        exerciseDaysOfWeek: preferences.exerciseDaysOfWeek,
        nlpKeywords: preferences.nlpKeywords,
        notificationsEnabled: preferences.notificationsEnabled
      };

      const updatedPreferences = await apiService.updateUserPreferences(payload);
      setPreferences(updatedPreferences);

      // Always sync theme to ensure localStorage and backend are in sync
      if (updatedPreferences.theme) {
        setTheme(updatedPreferences.theme);
        console.log('Theme synced with backend:', updatedPreferences.theme);
      }

      setSuccessMessage('Preferencias actualizadas correctamente');
      setTimeout(() => setSuccessMessage(''), 3000);
    } catch (err) {
      console.error('Error saving preferences:', err);
      setError('Error al guardar las preferencias');
    } finally {
      setIsSaving(false);
    }
  };

  const toggleWorkDay = (dayId: number) => {
    setPreferences(prev => ({
      ...prev,
      workDaysOfWeek: prev.workDaysOfWeek?.includes(dayId)
        ? prev.workDaysOfWeek.filter(d => d !== dayId)
        : [...(prev.workDaysOfWeek || []), dayId]
    }));
  };

  const toggleExerciseDay = (dayId: number) => {
    setPreferences(prev => ({
      ...prev,
      exerciseDaysOfWeek: prev.exerciseDaysOfWeek?.includes(dayId)
        ? prev.exerciseDaysOfWeek.filter(d => d !== dayId)
        : [...(prev.exerciseDaysOfWeek || []), dayId]
    }));
  };

  const addKeyword = () => {
    if (newKeyword.trim() && !preferences.nlpKeywords?.includes(newKeyword.trim())) {
      setPreferences(prev => ({
        ...prev,
        nlpKeywords: [...(prev.nlpKeywords || []), newKeyword.trim()]
      }));
      setNewKeyword('');
    }
  };

  const removeKeyword = (keyword: string) => {
    setPreferences(prev => ({
      ...prev,
      nlpKeywords: prev.nlpKeywords?.filter(k => k !== keyword) || []
    }));
  };

  if (isLoading) {
    return (
      <div className="settings-screen">
        <div className="settings-loading">
          <p>Cargando configuración...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="settings-screen">
      <header className="settings-header">
        <div className="settings-header-content">
          <div className="settings-title-group">
            <Settings className="settings-icon" size={24} />
            <div>
              <h1 className="settings-title">Configuración</h1>
              <p className="settings-subtitle">Personaliza tu experiencia con Aurora</p>
            </div>
          </div>
          <div className="settings-actions">
            <Button
              onClick={activeTab === 'profile' ? handleSaveProfile : handleSavePreferences}
              disabled={isSaving}
              className="save-button"
            >
              <Save size={16} />
              <span>{isSaving ? 'Guardando...' : 'Guardar cambios'}</span>
            </Button>
          </div>
        </div>
      </header>

      {error && (
        <div className="settings-error">
          <p>{error}</p>
        </div>
      )}

      {successMessage && (
        <div className="settings-success">
          <p>{successMessage}</p>
        </div>
      )}

      <div className="settings-tabs">
        {tabs.map(tab => {
          const IconComponent = tab.icon;
          return (
            <button
              key={tab.id}
              className={`settings-tab ${activeTab === tab.id ? 'active' : ''}`}
              onClick={() => setActiveTab(tab.id)}
            >
              <IconComponent size={16} />
              <span>{tab.label}</span>
            </button>
          );
        })}
      </div>

      <div className="settings-content">
        {/* PERFIL TAB */}
        {activeTab === 'profile' && (
          <div className="settings-panel">
            <Card className="settings-card">
              <div className="card-header">
                <User size={20} />
                <h2>Información Personal</h2>
              </div>
              <div className="card-content">
                <div className="form-group">
                  <label htmlFor="name">Nombre completo</label>
                  <Input
                    id="name"
                    type="text"
                    value={profile.name}
                    onChange={(e) => setProfile(prev => ({ ...prev, name: e.target.value }))}
                    placeholder="Aurora Usuario"
                  />
                </div>

                <div className="form-group">
                  <label htmlFor="email">Email</label>
                  <Input
                    id="email"
                    type="email"
                    value={profile.email}
                    onChange={(e) => setProfile(prev => ({ ...prev, email: e.target.value }))}
                    placeholder="usuario@aurora.app"
                  />
                </div>

                <div className="form-group">
                  <label htmlFor="timezone">Zona horaria</label>
                  <select
                    id="timezone"
                    className="settings-select"
                    value={profile.timezone || 'America/Mexico_City'}
                    onChange={(e) => setProfile(prev => ({ ...prev, timezone: e.target.value }))}
                  >
                    {TIMEZONES.map(tz => (
                      <option key={tz.value} value={tz.value}>{tz.label}</option>
                    ))}
                  </select>
                </div>
              </div>
            </Card>
          </div>
        )}

        {/* PREFERENCIAS TAB */}
        {activeTab === 'preferences' && (
          <div className="settings-panel">
            <Card className="settings-card">
              <div className="card-header">
                <Clock size={20} />
                <h2>Horarios de Trabajo</h2>
              </div>
              <div className="card-content">
                <div className="time-inputs">
                  <div className="form-group">
                    <label htmlFor="workStart">Hora inicio</label>
                    <Input
                      id="workStart"
                      type="time"
                      value={preferences.workStartTime || '09:00'}
                      onChange={(e) => setPreferences(prev => ({ ...prev, workStartTime: e.target.value }))}
                    />
                  </div>
                  <div className="form-group">
                    <label htmlFor="workEnd">Hora fin</label>
                    <Input
                      id="workEnd"
                      type="time"
                      value={preferences.workEndTime || '18:00'}
                      onChange={(e) => setPreferences(prev => ({ ...prev, workEndTime: e.target.value }))}
                    />
                  </div>
                </div>

                <div className="form-group">
                  <label>Días laborales</label>
                  <div className="weekday-selector">
                    {WEEKDAYS.map(day => (
                      <button
                        key={day.id}
                        type="button"
                        className={`weekday-button ${preferences.workDaysOfWeek?.includes(day.id) ? 'active' : ''}`}
                        onClick={() => toggleWorkDay(day.id)}
                        title={day.label}
                      >
                        {day.short}
                      </button>
                    ))}
                  </div>
                </div>

                <div className="form-group">
                  <label>Días preferidos para ejercicio</label>
                  <div className="weekday-selector">
                    {WEEKDAYS.map(day => (
                      <button
                        key={day.id}
                        type="button"
                        className={`weekday-button ${preferences.exerciseDaysOfWeek?.includes(day.id) ? 'active' : ''}`}
                        onClick={() => toggleExerciseDay(day.id)}
                        title={day.label}
                      >
                        {day.short}
                      </button>
                    ))}
                  </div>
                </div>
              </div>
            </Card>

            <Card className="settings-card">
              <div className="card-header">
                <Globe size={20} />
                <h2>Formato y Región</h2>
              </div>
              <div className="card-content">
                <div className="form-group">
                  <label htmlFor="firstDay">Primer día de la semana</label>
                  <select
                    id="firstDay"
                    className="settings-select"
                    value={preferences.firstDayOfWeek}
                    onChange={(e) => setPreferences(prev => ({ ...prev, firstDayOfWeek: Number(e.target.value) }))}
                  >
                    <option value={0}>Domingo</option>
                    <option value={1}>Lunes</option>
                    <option value={6}>Sábado</option>
                  </select>
                </div>

                <div className="form-group">
                  <label htmlFor="timeFormat">Formato de hora</label>
                  <select
                    id="timeFormat"
                    className="settings-select"
                    value={preferences.timeFormat}
                    onChange={(e) => setPreferences(prev => ({ ...prev, timeFormat: e.target.value as '12h' | '24h' }))}
                  >
                    <option value="12h">12 horas (AM/PM)</option>
                    <option value="24h">24 horas</option>
                  </select>
                </div>

                <div className="form-group">
                  <label>Tema</label>
                  <div className="theme-toggle">
                    <button
                      type="button"
                      className={`theme-option ${preferences.theme === 'light' ? 'active' : ''}`}
                      onClick={() => setPreferences(prev => ({ ...prev, theme: 'light' }))}
                    >
                      <Sun size={16} />
                      <span>Claro</span>
                    </button>
                    <button
                      type="button"
                      className={`theme-option ${preferences.theme === 'dark' ? 'active' : ''}`}
                      onClick={() => setPreferences(prev => ({ ...prev, theme: 'dark' }))}
                    >
                      <Moon size={16} />
                      <span>Oscuro</span>
                    </button>
                  </div>
                </div>
              </div>
            </Card>
          </div>
        )}

        {/* CATEGORÍAS TAB */}
        {activeTab === 'categories' && (
          <div className="settings-panel">
            <CategoryManagement />
          </div>
        )}

        {/* IA & NLP TAB */}
        {activeTab === 'ai-nlp' && (
          <div className="settings-panel">
            <Card className="settings-card">
              <div className="card-header">
                <MessageSquare size={20} />
                <h2>Palabras Clave Personales</h2>
              </div>
              <div className="card-content">
                <p className="card-description">
                  Agrega palabras clave que te ayuden a mejorar el reconocimiento de eventos por lenguaje natural
                </p>

                <div className="keyword-input-group">
                  <Input
                    type="text"
                    value={newKeyword}
                    onChange={(e) => setNewKeyword(e.target.value)}
                    onKeyPress={(e) => e.key === 'Enter' && addKeyword()}
                    placeholder="Ej: gimnasio, reunión, compras..."
                  />
                  <Button onClick={addKeyword} disabled={!newKeyword.trim()}>
                    Agregar
                  </Button>
                </div>

                {preferences.nlpKeywords && preferences.nlpKeywords.length > 0 && (
                  <div className="keyword-list">
                    {preferences.nlpKeywords.map(keyword => (
                      <span key={keyword} className="keyword-badge">
                        {keyword}
                        <button
                          type="button"
                          onClick={() => removeKeyword(keyword)}
                          className="keyword-remove"
                          aria-label={`Eliminar ${keyword}`}
                        >
                          ×
                        </button>
                      </span>
                    ))}
                  </div>
                )}
              </div>
            </Card>
          </div>
        )}

        {/* NOTIFICACIONES TAB */}
        {activeTab === 'notifications' && (
          <div className="settings-panel">
            <Card className="settings-card">
              <div className="card-header">
                {preferences.notificationsEnabled ? <Bell size={20} /> : <BellOff size={20} />}
                <h2>Notificaciones</h2>
              </div>
              <div className="card-content">
                <div className="notification-toggle">
                  <div className="toggle-info">
                    <h3>Activar notificaciones</h3>
                    <p>Recibe recordatorios de tus eventos programados</p>
                  </div>
                  <label className="switch">
                    <input
                      type="checkbox"
                      checked={preferences.notificationsEnabled}
                      onChange={(e) => setPreferences(prev => ({ ...prev, notificationsEnabled: e.target.checked }))}
                    />
                    <span className="slider"></span>
                  </label>
                </div>

                {preferences.notificationsEnabled && (
                  <div className="form-group">
                    <label htmlFor="reminderTime">Tiempo de recordatorio predeterminado</label>
                    <select
                      id="reminderTime"
                      className="settings-select"
                      value={preferences.defaultReminderMinutes}
                      onChange={(e) => setPreferences(prev => ({ ...prev, defaultReminderMinutes: Number(e.target.value) }))}
                    >
                      <option value={5}>5 minutos antes</option>
                      <option value={15}>15 minutos antes</option>
                      <option value={30}>30 minutos antes</option>
                      <option value={60}>1 hora antes</option>
                      <option value={120}>2 horas antes</option>
                      <option value={1440}>1 día antes</option>
                    </select>
                  </div>
                )}
              </div>
            </Card>
          </div>
        )}
      </div>
    </div>
  );
};

export default SettingsScreen;
