import { useState, useRef, useEffect, useCallback } from 'react';
import { Send, Mic, MicOff, X } from 'lucide-react';

interface Props {
  onSend: (message: string) => void;
  disabled: boolean;
}

// Web Speech API — not in default TS lib, use any-typed shim
// eslint-disable-next-line @typescript-eslint/no-explicit-any
type AnySpeechRecognition = any;

const COUNTDOWN_SECONDS = 1;

export default function MessageInput({ onSend, disabled }: Props) {
  const [text, setText]                   = useState('');
  const [listening, setListening]         = useState(false);
  const [countdown, setCountdown]         = useState<number | null>(null);
  const [voiceSupported]                  = useState(() => {
    if (typeof window === 'undefined') return false;
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const w = window as any;
    return !!(w.SpeechRecognition || w.webkitSpeechRecognition);
  });

  const textareaRef    = useRef<HTMLTextAreaElement>(null);
  const recognitionRef = useRef<AnySpeechRecognition>(null);
  const countdownTimer = useRef<ReturnType<typeof setInterval> | null>(null);
  const pendingText    = useRef('');

  const clearCountdown = useCallback(() => {
    if (countdownTimer.current) {
      clearInterval(countdownTimer.current);
      countdownTimer.current = null;
    }
    setCountdown(null);
  }, []);

  const startCountdown = useCallback((finalText: string) => {
    pendingText.current = finalText;
    setCountdown(COUNTDOWN_SECONDS);

    let remaining = COUNTDOWN_SECONDS;
    countdownTimer.current = setInterval(() => {
      remaining -= 1;
      if (remaining <= 0) {
        clearCountdown();
        // Fire send
        const msg = pendingText.current.trim();
        if (msg) {
          setText('');
          pendingText.current = '';
          onSend(msg);
        }
      } else {
        setCountdown(remaining);
      }
    }, 1000);
  }, [clearCountdown, onSend]);

  const stopListening = useCallback(() => {
    recognitionRef.current?.stop();
    recognitionRef.current = null;
    setListening(false);
  }, []);

  const handleVoiceCancel = () => {
    stopListening();
    clearCountdown();
    // Keep the transcribed text in the box so user can edit
  };

  const startListening = () => {
    if (disabled) return;
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const w = window as any;
    const SR = w.SpeechRecognition ?? w.webkitSpeechRecognition;
    if (!SR) return;

    const rec = new SR();
    rec.continuous      = false;
    rec.interimResults  = true;
    rec.lang            = 'en-US';

    rec.onstart = () => setListening(true);

    rec.onresult = (event: SpeechRecognitionEvent) => {
      let interim = '';
      let final   = '';
      for (let i = event.resultIndex; i < event.results.length; i++) {
        const t = event.results[i][0].transcript;
        if (event.results[i].isFinal) final += t;
        else interim += t;
      }
      const combined = (text + ' ' + (final || interim)).trim();
      setText(combined);
      autoResizeTextarea();

      if (final) {
        stopListening();
        if (combined.trim()) startCountdown(combined);
      }
    };

    rec.onerror = () => {
      setListening(false);
      recognitionRef.current = null;
    };

    rec.onend = () => {
      setListening(false);
      recognitionRef.current = null;
    };

    recognitionRef.current = rec;
    rec.start();
  };

  const autoResizeTextarea = () => {
    const el = textareaRef.current;
    if (!el) return;
    el.style.height = 'auto';
    el.style.height = `${Math.min(el.scrollHeight, 160)}px`;
  };

  const handleSend = () => {
    const trimmed = text.trim();
    if (!trimmed || disabled) return;
    clearCountdown();
    stopListening();
    setText('');
    if (textareaRef.current) textareaRef.current.style.height = 'auto';
    onSend(trimmed);
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  const handleInput = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    setText(e.target.value);
    clearCountdown(); // typing cancels voice auto-send
    autoResizeTextarea();
  };

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      stopListening();
      clearCountdown();
    };
  }, [stopListening, clearCountdown]);

  const micActive = listening || countdown !== null;

  return (
    <div className="border-t border-gray-200 bg-white p-4 shrink-0">

      {/* Countdown strip */}
      {countdown !== null && (
        <div className="flex items-center justify-between bg-blue-50 border border-blue-200 rounded-lg px-3 py-2 mb-2 text-sm text-blue-700">
          <span>
            Sending in <span className="font-bold">{countdown}s</span>…
          </span>
          <button
            onClick={handleVoiceCancel}
            className="flex items-center gap-1 text-xs font-semibold text-blue-600 hover:text-blue-800 transition-colors"
          >
            <X size={12} />
            Cancel
          </button>
        </div>
      )}

      {/* Listening indicator */}
      {listening && (
        <div className="flex items-center gap-2 mb-2 text-xs text-red-600">
          <span className="w-2 h-2 rounded-full bg-red-500 animate-pulse" />
          Listening…
          <button onClick={handleVoiceCancel} className="ml-auto text-gray-500 hover:text-gray-700 flex items-center gap-1">
            <X size={12} /> Cancel
          </button>
        </div>
      )}

      <div className="flex items-end gap-2 bg-gray-50 border border-gray-300 rounded-xl px-3 py-2 focus-within:border-blue-400 focus-within:ring-1 focus-within:ring-blue-400 transition-all">
        <textarea
          ref={textareaRef}
          value={text}
          onChange={handleInput}
          onKeyDown={handleKeyDown}
          placeholder={listening ? 'Listening…' : 'Type a message… (Enter to send)'}
          disabled={disabled}
          rows={1}
          className="flex-1 bg-transparent text-sm text-gray-800 placeholder-gray-400 resize-none outline-none min-h-[24px] max-h-40 disabled:opacity-50"
        />

        {/* Mic button */}
        {voiceSupported && (
          <button
            type="button"
            onClick={micActive ? handleVoiceCancel : startListening}
            disabled={disabled}
            title={micActive ? 'Stop listening' : 'Voice input'}
            className={`p-1.5 rounded-lg transition-colors disabled:opacity-40 shrink-0 ${
              micActive
                ? 'bg-red-100 text-red-600 hover:bg-red-200'
                : 'text-gray-400 hover:bg-gray-200 hover:text-gray-600'
            }`}
          >
            {micActive ? <MicOff size={14} /> : <Mic size={14} />}
          </button>
        )}

        {/* Send button */}
        <button
          onClick={handleSend}
          disabled={disabled || !text.trim()}
          className="p-1.5 rounded-lg bg-blue-600 hover:bg-blue-700 text-white disabled:opacity-40 disabled:cursor-not-allowed transition-colors shrink-0"
        >
          <Send size={14} />
        </button>
      </div>

      <p className="text-xs text-gray-400 mt-1 text-center">
        Enter to send · Shift+Enter for new line{voiceSupported ? ' · 🎤 voice input supported' : ''}
      </p>
    </div>
  );
}
