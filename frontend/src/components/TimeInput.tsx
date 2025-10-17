import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import './TimeInput.css';

interface TimeInputProps {
  value: string; // Formato HH:MM (24h)
  onChange: (value: string) => void;
  timeFormat: '12h' | '24h';
  disabled?: boolean;
  id?: string;
  className?: string;
}

export const TimeInput: React.FC<TimeInputProps> = ({
  value,
  onChange,
  timeFormat,
  disabled = false,
  id,
  className = ''
}) => {
  const [isOpen, setIsOpen] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
  const [manualInput, setManualInput] = useState('');
  const containerRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const dropdownRef = useRef<HTMLDivElement>(null);

  // Generar opciones de tiempo (cada 15 minutos para mayor flexibilidad)
  const timeOptions = useMemo(() => {
    const options: Array<{ value: string; label: string }> = [];

    for (let hour = 0; hour < 24; hour++) {
      for (let minute = 0; minute < 60; minute += 15) {
        const value24h = `${hour.toString().padStart(2, '0')}:${minute.toString().padStart(2, '0')}`;

        let label: string;
        if (timeFormat === '12h') {
          const period = hour >= 12 ? 'PM' : 'AM';
          const displayHour = hour === 0 ? 12 : hour > 12 ? hour - 12 : hour;
          label = `${displayHour}:${minute.toString().padStart(2, '0')} ${period}`;
        } else {
          label = value24h;
        }

        options.push({ value: value24h, label });
      }
    }

    return options;
  }, [timeFormat]);

  // Encontrar la opción más cercana para el scroll (solo para posicionamiento visual)
  const findClosestOption = useCallback((time24h: string): string => {
    if (!time24h) return time24h;

    // Verificar si el valor ya existe en las opciones
    const exists = timeOptions.some(opt => opt.value === time24h);
    if (exists) return time24h;

    // Si no existe, encontrar el intervalo de 15 minutos más cercano
    const [hourStr, minuteStr] = time24h.split(':');
    const hour = parseInt(hourStr, 10);
    const minute = parseInt(minuteStr, 10);

    // Redondear al múltiplo de 15 más cercano
    const roundedMinute = Math.round(minute / 15) * 15;
    const finalMinute = roundedMinute === 60 ? 0 : roundedMinute;
    const finalHour = roundedMinute === 60 ? (hour + 1) % 24 : hour;

    return `${finalHour.toString().padStart(2, '0')}:${finalMinute.toString().padStart(2, '0')}`;
  }, [timeOptions]);

  // Validar formato de hora (HH:MM)
  const isValidTimeFormat = (time: string): boolean => {
    if (!time) return false;
    const timeRegex = /^([0-1]?[0-9]|2[0-3]):([0-5][0-9])$/;
    return timeRegex.test(time);
  };

  // Parsear entrada manual a formato HH:MM
  const parseCustomTime = useCallback((input: string): string | null => {
    if (!input) return null;

    // Intentar parsear diferentes formatos
    // Formato: HH:MM o H:MM
    const match24h = input.match(/^(\d{1,2}):(\d{2})$/);
    if (match24h) {
      const hour = parseInt(match24h[1], 10);
      const minute = parseInt(match24h[2], 10);
      if (hour >= 0 && hour < 24 && minute >= 0 && minute < 60) {
        return `${hour.toString().padStart(2, '0')}:${minute.toString().padStart(2, '0')}`;
      }
    }

    // Formato: H AM/PM o HH:MM AM/PM
    const match12h = input.match(/^(\d{1,2}):?(\d{2})?\s*(am|pm)$/i);
    if (match12h) {
      let hour = parseInt(match12h[1], 10);
      const minute = match12h[2] ? parseInt(match12h[2], 10) : 0;
      const period = match12h[3].toLowerCase();

      if (hour >= 1 && hour <= 12 && minute >= 0 && minute < 60) {
        if (period === 'pm' && hour !== 12) hour += 12;
        if (period === 'am' && hour === 12) hour = 0;
        return `${hour.toString().padStart(2, '0')}:${minute.toString().padStart(2, '0')}`;
      }
    }

    return null;
  }, []);

  // Formatear el valor actual para mostrar
  const formatDisplayValue = useCallback((time24h: string): string => {
    if (!time24h) return '';

    const [hourStr, minuteStr] = time24h.split(':');
    const hour = parseInt(hourStr, 10);
    const minute = parseInt(minuteStr, 10);

    if (timeFormat === '12h') {
      const period = hour >= 12 ? 'PM' : 'AM';
      const displayHour = hour === 0 ? 12 : hour > 12 ? hour - 12 : hour;
      return `${displayHour}:${minute.toString().padStart(2, '0')} ${period}`;
    }

    return time24h;
  }, [timeFormat]);

  // Filtrar opciones según término de búsqueda
  const filteredOptions = useMemo(() => {
    if (!searchTerm) return timeOptions;

    // Si el searchTerm es exactamente el valor actual formateado, mostrar todas las opciones
    // Esto evita el problema de "no se encontraron resultados" cuando el valor actual
    // no está en la lista de opciones (ej: 17:48 vs opciones cada 15 min)
    if (searchTerm === formatDisplayValue(value)) {
      return timeOptions;
    }

    const lower = searchTerm.toLowerCase();
    const filtered = timeOptions.filter(opt => opt.label.toLowerCase().includes(lower));

    // Si el usuario escribió algo con formato válido que no está en la lista,
    // agregar una opción especial "Usar [hora escrita]"
    const customTime = parseCustomTime(searchTerm);
    if (customTime && !filtered.some(opt => opt.value === customTime)) {
      filtered.unshift({
        value: customTime,
        label: `${formatDisplayValue(customTime)}`
      });
    }

    return filtered;
  }, [timeOptions, searchTerm, value, formatDisplayValue, parseCustomTime]);

  // Cerrar dropdown al hacer click fuera
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false);
        setSearchTerm('');
      }
    };

    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside);
    }

    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, [isOpen]);

  // Scroll a la opción más cercana cuando se abre el dropdown
  useEffect(() => {
    if (isOpen && dropdownRef.current && value) {
      const closestValue = findClosestOption(value);

      setTimeout(() => {
        const selectedOption = dropdownRef.current?.querySelector(`[data-value="${closestValue}"]`) as HTMLElement;

        if (selectedOption && dropdownRef.current) {
          // Posicionar la opción seleccionada al tope del dropdown
          const optionTop = selectedOption.offsetTop;
          dropdownRef.current.scrollTop = optionTop;
        }
      }, 0);
    }
  }, [isOpen, value, findClosestOption]);

  const handleSelect = (optionValue: string) => {
    onChange(optionValue);
    setIsOpen(false);
    setSearchTerm('');
    setManualInput('');
  };

  const handleIconClick = (e: React.MouseEvent) => {
    console.log('🖱️ Icon clicked, current isOpen:', isOpen);
    e.stopPropagation();
    e.preventDefault();

    if (!disabled) {
      const newOpenState = !isOpen;
      console.log('📝 Setting isOpen to:', newOpenState);

      if (newOpenState) {
        // Al abrir el dropdown, establecer el searchTerm con el valor formateado actual
        setSearchTerm(formatDisplayValue(value));
        setIsOpen(true);
        // Enfocar el input para permitir búsqueda
        setTimeout(() => {
          inputRef.current?.focus();
          inputRef.current?.select(); // Seleccionar el texto para que se pueda reemplazar fácilmente
          console.log('✅ Dropdown opened and input focused');
        }, 0);
      } else {
        // Al cerrar, limpiar el searchTerm
        setSearchTerm('');
        setIsOpen(false);
      }
    }
  };

  const handleInputFocus = () => {
    if (!disabled && !isOpen) {
      // No abrir automáticamente el dropdown al hacer foco
      // Solo permitir entrada manual
    }
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const newValue = e.target.value;

    if (isOpen) {
      // Modo búsqueda en dropdown
      setSearchTerm(newValue);
    } else {
      // Modo entrada manual
      setManualInput(newValue);

      // Validar y actualizar si es un formato válido
      if (isValidTimeFormat(newValue)) {
        // Normalizar el formato (agregar ceros adelante si es necesario)
        const [hourStr, minuteStr] = newValue.split(':');
        const hour = parseInt(hourStr, 10);
        const minute = parseInt(minuteStr, 10);
        const normalized = `${hour.toString().padStart(2, '0')}:${minute.toString().padStart(2, '0')}`;
        onChange(normalized);
      }
    }
  };

  const handleInputBlur = () => {
    // Usar setTimeout para permitir que los clicks en el dropdown se registren primero
    setTimeout(() => {
      // Al perder el foco, validar la entrada manual
      if (manualInput && !isValidTimeFormat(manualInput)) {
        // Si el formato no es válido, revertir al valor anterior
        setManualInput('');
      } else {
        setManualInput('');
      }
    }, 200);
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Escape') {
      setIsOpen(false);
      setSearchTerm('');
    } else if (e.key === 'Enter') {
      if (isOpen && filteredOptions.length > 0) {
        handleSelect(filteredOptions[0].value);
      } else if (!isOpen && manualInput && isValidTimeFormat(manualInput)) {
        // Confirmar entrada manual
        const [hourStr, minuteStr] = manualInput.split(':');
        const hour = parseInt(hourStr, 10);
        const minute = parseInt(minuteStr, 10);
        const normalized = `${hour.toString().padStart(2, '0')}:${minute.toString().padStart(2, '0')}`;
        onChange(normalized);
        setManualInput('');
        inputRef.current?.blur();
      }
    }
  };

  return (
    <div
      ref={containerRef}
      className={`time-input-container ${className}`}
    >
      <div
        className={`time-input-wrapper ${isOpen ? 'open' : ''} ${disabled ? 'disabled' : ''}`}
      >
        <input
          ref={inputRef}
          id={id}
          type="text"
          className="time-input-display"
          value={isOpen ? searchTerm : (manualInput || formatDisplayValue(value))}
          onChange={handleInputChange}
          onFocus={handleInputFocus}
          onBlur={handleInputBlur}
          onKeyDown={handleKeyDown}
          placeholder={timeFormat === '12h' ? '12:00 AM' : '00:00'}
          disabled={disabled}
        />
        <div
          className="time-input-icon-wrapper"
          onMouseDown={(e) => {
            e.preventDefault(); // Prevenir que el input pierda el foco
          }}
          onClick={handleIconClick}
          style={{ cursor: disabled ? 'not-allowed' : 'pointer', display: 'flex', alignItems: 'center' }}
        >
          <svg
            className="time-input-icon"
            width="20"
            height="20"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
          >
            <circle cx="12" cy="12" r="10" />
            <polyline points="12 6 12 12 16 14" />
          </svg>
        </div>
      </div>

      {isOpen && (
        <div ref={dropdownRef} className="time-input-dropdown">
          {filteredOptions.length > 0 ? (
            filteredOptions.map((option) => (
              <div
                key={option.value}
                data-value={option.value}
                className={`time-input-option ${value === option.value ? 'selected' : ''}`}
                onClick={() => handleSelect(option.value)}
              >
                {option.label}
              </div>
            ))
          ) : (
            <div className="time-input-no-results">
              No se encontraron resultados
            </div>
          )}
        </div>
      )}
    </div>
  );
};
