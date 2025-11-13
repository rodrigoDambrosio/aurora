import { useCallback, useEffect, useRef, useState } from 'react';

type SpeechRecognitionResultPayload = {
  text: string;
  isFinal: boolean;
};

type SpeechRecognitionConstructorLike = new () => SpeechRecognitionInstance;

interface SpeechRecognitionAlternativeLike {
  transcript: string;
  confidence?: number;
}

interface SpeechRecognitionResultLike {
  length: number;
  isFinal: boolean;
  [index: number]: SpeechRecognitionAlternativeLike | undefined;
}

interface SpeechRecognitionResultListLike {
  length: number;
  [index: number]: SpeechRecognitionResultLike | undefined;
}

interface SpeechRecognitionEventLike {
  results: SpeechRecognitionResultListLike;
}

interface SpeechRecognitionErrorEventLike {
  error?: string;
  message?: string;
}

interface SpeechRecognitionInstance {
  lang: string;
  continuous: boolean;
  interimResults: boolean;
  maxAlternatives: number;
  start: () => void;
  stop: () => void;
  abort: () => void;
  onstart: (() => void) | null;
  onend: (() => void) | null;
  onerror: ((event: SpeechRecognitionErrorEventLike) => void) | null;
  onresult: ((event: SpeechRecognitionEventLike) => void) | null;
}

interface WindowWithSpeechRecognition extends Window {
  SpeechRecognition?: SpeechRecognitionConstructorLike;
  webkitSpeechRecognition?: SpeechRecognitionConstructorLike;
}

interface UseSpeechRecognitionOptions {
  lang?: string;
  interimResults?: boolean;
  continuous?: boolean;
  onStart?: () => void;
  onEnd?: () => void;
  onError?: (error: string) => void;
  onResult?: (result: SpeechRecognitionResultPayload) => void;
}

interface UseSpeechRecognitionReturn {
  isSupported: boolean;
  isListening: boolean;
  error: string | null;
  transcript: string;
  interimResult: string;
  latestResult: SpeechRecognitionResultPayload | null;
  start: () => void;
  stop: () => void;
  reset: () => void;
}

const sanitizeChunk = (value: string): string => value.replace(/\s+/g, ' ').trim();

export function useSpeechRecognition(options?: UseSpeechRecognitionOptions): UseSpeechRecognitionReturn {
  const { lang = 'es-ES', interimResults = true, continuous = false } = options ?? {};
  const recognitionRef = useRef<SpeechRecognitionInstance | null>(null);
  const onStartRef = useRef<UseSpeechRecognitionOptions['onStart']>(options?.onStart);
  const onEndRef = useRef<UseSpeechRecognitionOptions['onEnd']>(options?.onEnd);
  const onErrorRef = useRef<UseSpeechRecognitionOptions['onError']>(options?.onError);
  const onResultRef = useRef<UseSpeechRecognitionOptions['onResult']>(options?.onResult);

  const [isSupported, setIsSupported] = useState(false);
  const [isListening, setIsListening] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [transcript, setTranscript] = useState('');
  const [interimResult, setInterimResult] = useState('');
  const [latestResult, setLatestResult] = useState<SpeechRecognitionResultPayload | null>(null);

  useEffect(() => {
    onStartRef.current = options?.onStart;
  }, [options?.onStart]);

  useEffect(() => {
    onEndRef.current = options?.onEnd;
  }, [options?.onEnd]);

  useEffect(() => {
    onErrorRef.current = options?.onError;
  }, [options?.onError]);

  useEffect(() => {
    onResultRef.current = options?.onResult;
  }, [options?.onResult]);

  useEffect(() => {
    if (typeof window === 'undefined') {
      return;
    }

    const speechWindow = window as unknown as WindowWithSpeechRecognition;
    const SpeechRecognitionCtor = speechWindow.SpeechRecognition ?? speechWindow.webkitSpeechRecognition;

    if (!SpeechRecognitionCtor) {
      setIsSupported(false);
      return;
    }

    const recognition = new SpeechRecognitionCtor();
    recognition.lang = lang;
    recognition.interimResults = interimResults;
    recognition.continuous = continuous;
    recognition.maxAlternatives = 1;

    recognition.onstart = () => {
      setIsListening(true);
      setError(null);
      onStartRef.current?.();
    };

    recognition.onend = () => {
      setIsListening(false);
      onEndRef.current?.();
    };

    recognition.onerror = (event) => {
      const message = event.error ?? 'unknown-error';
      setError(message);
      setIsListening(false);
      onErrorRef.current?.(message);
    };

    recognition.onresult = (event) => {
      let finalChunk = '';
      let interimChunk = '';
      const resultsLength = event.results?.length ?? 0;

      for (let index = 0; index < resultsLength; index += 1) {
        const result = event.results[index];
        if (!result) continue;

        const alternative = result[0];
        const text = alternative?.transcript ? sanitizeChunk(alternative.transcript) : '';
        if (!text) continue;

        if (result.isFinal) {
          finalChunk = finalChunk ? `${finalChunk} ${text}` : text;
        } else {
          interimChunk = interimChunk ? `${interimChunk} ${text}` : text;
        }
      }

      if (interimChunk) {
        const sanitizedInterim = sanitizeChunk(interimChunk);
        setInterimResult(sanitizedInterim);
        const payload = { text: sanitizedInterim, isFinal: false };
        setLatestResult(payload);
        onResultRef.current?.(payload);
      } else {
        setInterimResult('');
      }

      if (finalChunk) {
        const sanitizedFinal = sanitizeChunk(finalChunk);
        setTranscript((prev) => {
          if (!prev) return sanitizedFinal;
          return sanitizeChunk(`${prev} ${sanitizedFinal}`);
        });
        const payload = { text: sanitizedFinal, isFinal: true };
        setLatestResult(payload);
        onResultRef.current?.(payload);
        setInterimResult('');
      }
    };

    recognitionRef.current = recognition;
    setIsSupported(true);
    setTranscript('');
    setInterimResult('');
    setLatestResult(null);
    setError(null);

    return () => {
      recognition.onstart = null;
      recognition.onend = null;
      recognition.onerror = null;
      recognition.onresult = null;
      try {
        recognition.abort();
      } catch {
        // No-op: stopping an idle recognition instance throws in some browsers.
      }
      recognitionRef.current = null;
      setIsListening(false);
    };
  }, [lang, interimResults, continuous]);

  const start = useCallback(() => {
    if (!recognitionRef.current) {
      setError('not-supported');
      return;
    }

    setError(null);

    try {
      recognitionRef.current.start();
    } catch (startError) {
      const message = startError instanceof Error ? startError.message : 'start-failed';
      setError(message);
    }
  }, []);

  const stop = useCallback(() => {
    if (!recognitionRef.current) {
      return;
    }

    try {
      recognitionRef.current.stop();
    } catch (stopError) {
      const message = stopError instanceof Error ? stopError.message : 'stop-failed';
      setError(message);
    }
  }, []);

  const reset = useCallback(() => {
    setTranscript('');
    setInterimResult('');
    setLatestResult(null);
    setError(null);
  }, []);

  return {
    isSupported,
    isListening,
    error,
    transcript,
    interimResult,
    latestResult,
    start,
    stop,
    reset
  };
}
