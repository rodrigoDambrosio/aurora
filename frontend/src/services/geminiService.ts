const DEFAULT_GEMINI_MODEL = 'gemini-1.5-flash-latest';
const GEMINI_MODEL = import.meta.env.VITE_GEMINI_MODEL || DEFAULT_GEMINI_MODEL;
const GEMINI_API_KEY = import.meta.env.VITE_GEMINI_API_KEY;
const GEMINI_API_URL = `https://generativelanguage.googleapis.com/v1beta/models/${GEMINI_MODEL}:generateContent`;

export type GeminiRole = 'user' | 'assistant';

export interface GeminiMessage {
  role: GeminiRole;
  content: string;
}

export interface GeminiResponseOptions {
  systemPrompt?: string;
  temperature?: number;
  maxOutputTokens?: number;
}

const mapRoleToGemini = (role: GeminiRole): 'user' | 'model' => (role === 'assistant' ? 'model' : 'user');

const extractTextFromResponse = (payload: unknown): string | null => {
  if (!payload || typeof payload !== 'object') {
    return null;
  }

  const candidate = (payload as { candidates?: Array<{ content?: { parts?: Array<{ text?: string }> } }> }).candidates?.[0];
  if (!candidate?.content?.parts || candidate.content.parts.length === 0) {
    return null;
  }

  const parts = candidate.content.parts;
  const combined = parts
    .map((part) => (part && typeof part === 'object' && 'text' in part ? String(part.text ?? '') : ''))
    .join('\n')
    .trim();

  return combined.length > 0 ? combined : null;
};

export async function generateGeminiResponse(
  messages: GeminiMessage[],
  options?: GeminiResponseOptions
): Promise<string> {
  if (!GEMINI_API_KEY) {
    throw new Error('Gemini API key no configurada. Seteá VITE_GEMINI_API_KEY.');
  }

  const contents = messages.map((message) => ({
    role: mapRoleToGemini(message.role),
    parts: [{ text: message.content }]
  }));

  const body: Record<string, unknown> = {
    contents,
    generationConfig: {
      temperature: options?.temperature ?? 0.7,
      topP: 0.95,
      topK: 32,
      maxOutputTokens: options?.maxOutputTokens ?? 512
    }
  };

  if (options?.systemPrompt) {
    body.systemInstruction = {
      role: 'system',
      parts: [{ text: options.systemPrompt }]
    };
  }

  const response = await fetch(`${GEMINI_API_URL}?key=${encodeURIComponent(GEMINI_API_KEY)}`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(body)
  });

  let json: unknown = null;

  try {
    json = await response.json();
  } catch (parseError) {
    throw new Error(`No se pudo parsear la respuesta de Gemini: ${String(parseError)}`);
  }

  if (!response.ok) {
    const errorMessage =
      (json && typeof json === 'object' && 'error' in json && json.error && typeof json.error === 'object' && 'message' in json.error)
        ? String((json.error as { message?: string }).message ?? 'Error desconocido de Gemini')
        : `Gemini devolvió HTTP ${response.status}`;
    throw new Error(errorMessage);
  }

  const text = extractTextFromResponse(json);
  if (!text) {
    throw new Error('Gemini devolvió una respuesta vacía.');
  }

  return text;
}
