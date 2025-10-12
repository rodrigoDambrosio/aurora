using System.Text.Json;
using Aurora.Application.DTOs.User;
using Aurora.Application.Interfaces;
using Aurora.Domain.Entities;

namespace Aurora.Application.Services;

/// <summary>
/// Servicio para gestión de usuarios y preferencias
/// </summary>
public class UserService : IUserService
{
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<UserPreferences> _preferencesRepository;

    public UserService(
        IRepository<User> userRepository,
        IRepository<UserPreferences> preferencesRepository)
    {
        _userRepository = userRepository;
        _preferencesRepository = preferencesRepository;
    }

    public async Task<UserProfileDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            throw new InvalidOperationException("Usuario no encontrado");
        }

        return new UserProfileDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Timezone = user.Timezone
        };
    }

    public async Task<UserProfileDto> UpdateProfileAsync(
        Guid userId,
        UpdateUserProfileDto dto,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            throw new InvalidOperationException("Usuario no encontrado");
        }

        // Actualizar solo los campos proporcionados
        if (!string.IsNullOrWhiteSpace(dto.Name))
        {
            user.Name = dto.Name;
        }

        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            // Verificar que el email no esté en uso por otro usuario
            var allUsers = await _userRepository.GetAllAsync();
            var existingUser = allUsers.FirstOrDefault(u => u.Email == dto.Email && u.Id != userId);

            if (existingUser != null)
            {
                throw new InvalidOperationException("El email ya está en uso");
            }

            user.Email = dto.Email;
        }

        if (dto.Timezone != null)
        {
            user.Timezone = dto.Timezone;
        }

        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        return new UserProfileDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Timezone = user.Timezone
        };
    }

    public async Task<UserPreferencesDto> GetPreferencesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var allPreferences = await _preferencesRepository.GetAllAsync();
        var preferences = allPreferences.FirstOrDefault(p => p.UserId == userId);

        if (preferences == null)
        {
            // Crear preferencias por defecto si no existen
            preferences = new UserPreferences
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Theme = "light",
                Language = "es-ES",
                DefaultReminderMinutes = 15,
                FirstDayOfWeek = 1,
                TimeFormat = "24h",
                DateFormat = "dd/MM/yyyy",
                NotificationsEnabled = true
            };

            await _preferencesRepository.AddAsync(preferences);
            await _preferencesRepository.SaveChangesAsync();
        }

        return MapToDto(preferences);
    }

    public async Task<UserPreferencesDto> UpdatePreferencesAsync(
        Guid userId,
        UpdateUserPreferencesDto dto,
        CancellationToken cancellationToken = default)
    {
        var allPreferences = await _preferencesRepository.GetAllAsync();
        var preferences = allPreferences.FirstOrDefault(p => p.UserId == userId);

        if (preferences == null)
        {
            // Crear preferencias si no existen
            preferences = new UserPreferences
            {
                Id = Guid.NewGuid(),
                UserId = userId
            };
            await _preferencesRepository.AddAsync(preferences);
        }

        // Actualizar solo los campos proporcionados
        if (dto.Theme != null) preferences.Theme = dto.Theme;
        if (dto.Language != null) preferences.Language = dto.Language;
        if (dto.DefaultReminderMinutes.HasValue) preferences.DefaultReminderMinutes = dto.DefaultReminderMinutes.Value;
        if (dto.FirstDayOfWeek.HasValue) preferences.FirstDayOfWeek = dto.FirstDayOfWeek.Value;
        if (dto.TimeFormat != null) preferences.TimeFormat = dto.TimeFormat;
        if (dto.DateFormat != null) preferences.DateFormat = dto.DateFormat;
        if (dto.WorkStartTime != null) preferences.WorkStartTime = dto.WorkStartTime;
        if (dto.WorkEndTime != null) preferences.WorkEndTime = dto.WorkEndTime;
        if (dto.NotificationsEnabled.HasValue) preferences.NotificationsEnabled = dto.NotificationsEnabled.Value;

        // Serializar arrays a JSON
        if (dto.WorkDaysOfWeek != null)
        {
            preferences.WorkDaysOfWeek = JsonSerializer.Serialize(dto.WorkDaysOfWeek);
        }

        if (dto.ExerciseDaysOfWeek != null)
        {
            preferences.ExerciseDaysOfWeek = JsonSerializer.Serialize(dto.ExerciseDaysOfWeek);
        }

        if (dto.NlpKeywords != null)
        {
            preferences.NlpKeywords = JsonSerializer.Serialize(dto.NlpKeywords);
        }

        if (preferences.Id == Guid.Empty)
        {
            await _preferencesRepository.AddAsync(preferences);
        }
        else
        {
            await _preferencesRepository.UpdateAsync(preferences);
        }

        await _preferencesRepository.SaveChangesAsync();

        return MapToDto(preferences);
    }

    private static UserPreferencesDto MapToDto(UserPreferences preferences)
    {
        return new UserPreferencesDto
        {
            Id = preferences.Id,
            UserId = preferences.UserId,
            Theme = preferences.Theme,
            Language = preferences.Language,
            DefaultReminderMinutes = preferences.DefaultReminderMinutes,
            FirstDayOfWeek = preferences.FirstDayOfWeek,
            TimeFormat = preferences.TimeFormat,
            DateFormat = preferences.DateFormat,
            WorkStartTime = preferences.WorkStartTime,
            WorkEndTime = preferences.WorkEndTime,
            WorkDaysOfWeek = !string.IsNullOrWhiteSpace(preferences.WorkDaysOfWeek)
                ? JsonSerializer.Deserialize<List<int>>(preferences.WorkDaysOfWeek)
                : new List<int>(),
            ExerciseDaysOfWeek = !string.IsNullOrWhiteSpace(preferences.ExerciseDaysOfWeek)
                ? JsonSerializer.Deserialize<List<int>>(preferences.ExerciseDaysOfWeek)
                : new List<int>(),
            NlpKeywords = !string.IsNullOrWhiteSpace(preferences.NlpKeywords)
                ? JsonSerializer.Deserialize<List<string>>(preferences.NlpKeywords)
                : new List<string>(),
            NotificationsEnabled = preferences.NotificationsEnabled
        };
    }
}
