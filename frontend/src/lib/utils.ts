/**
 * Utility function to merge class names
 * @param inputs - Class names to merge
 * @returns Merged class names string
 */
export function cn(...inputs: (string | undefined | null | boolean)[]) {
  return inputs.filter(Boolean).join(' ');
}