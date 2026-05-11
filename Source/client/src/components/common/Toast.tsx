import { useEffect } from 'react';
import { CheckCircle2, XCircle, X } from 'lucide-react';

export type ToastState = { type: 'success' | 'error'; message: string } | null;

interface ToastProps {
  toast: ToastState;
  onClose: () => void;
  duration?: number;
}

export default function Toast({ toast, onClose, duration = 4000 }: ToastProps) {
  useEffect(() => {
    if (!toast) return;
    const t = setTimeout(onClose, duration);
    return () => clearTimeout(t);
  }, [toast, duration, onClose]);

  if (!toast) return null;

  const isSuccess = toast.type === 'success';
  return (
    <div className="fixed top-5 right-5 z-[9999] animate-in fade-in slide-in-from-top-2">
      <div className={`flex items-start gap-3 min-w-[280px] max-w-md rounded-lg border px-4 py-3 shadow-lg ${
        isSuccess
          ? 'border-green-200 bg-green-50 text-green-800'
          : 'border-red-200 bg-red-50 text-red-800'
      }`}>
        {isSuccess
          ? <CheckCircle2 size={18} className="text-green-600 mt-0.5 shrink-0" />
          : <XCircle size={18} className="text-red-600 mt-0.5 shrink-0" />}
        <p className="text-sm flex-1">{toast.message}</p>
        <button onClick={onClose} className="text-gray-400 hover:text-gray-600 shrink-0">
          <X size={16} />
        </button>
      </div>
    </div>
  );
}
