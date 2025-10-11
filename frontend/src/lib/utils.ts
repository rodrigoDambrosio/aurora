/**
 * Utility function to merge class names
 * @param inputs - Class names to merge
 * @returns Merged class names string
 */
export function cn(...inputs: (string | undefined | null | boolean)[]) {
  return inputs.filter(Boolean).join(' ');
}

/**
 * Format a date into "Mes Año" (e.g. "Octubre 2025") honoring Spanish locale casing.
 */
export function formatMonthTitle(date: Date): string {
  const monthFormatter = new Intl.DateTimeFormat('es-ES', { month: 'long' });
  const month = monthFormatter.format(date);
  const capitalizedMonth = month.charAt(0).toUpperCase() + month.slice(1);
  return `${capitalizedMonth} ${date.getFullYear()}`;
}