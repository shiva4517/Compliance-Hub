import { X } from 'lucide-react';
import type { ReactNode } from 'react';

interface ModalProps {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  subtitle?: string;
  children: ReactNode;
  size?: 'sm' | 'md' | 'lg' | 'xl';
  /** Optional content rendered next to the title (e.g. an action button). */
  headerExtra?: ReactNode;
}

const sizeMap = {
  sm: 'max-w-md',
  md: 'max-w-lg',
  lg: 'max-w-2xl',
  xl: 'max-w-4xl',
};

export default function Modal({ isOpen, onClose, title, subtitle, children, size = 'lg', headerExtra }: ModalProps) {
  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-black/40" />
      <div className={`relative bg-white rounded-xl shadow-xl w-full ${sizeMap[size]} mx-4 max-h-[90vh] overflow-y-auto`}>
        <div className="p-6 border-b border-gray-200">
          <button type="button" onClick={onClose} className="absolute top-4 right-4 text-gray-400 hover:text-gray-600">
            <X size={20} />
          </button>
          <div className="flex items-start justify-between gap-3 pr-8">
            <div>
              <h2 className="text-xl font-semibold text-gray-900">{title}</h2>
              {subtitle && <p className="text-sm text-gray-500 mt-1">{subtitle}</p>}
            </div>
            {headerExtra && <div className="flex-shrink-0">{headerExtra}</div>}
          </div>
        </div>
        <div className="p-6">{children}</div>
      </div>
    </div>
  );
}
