import Modal from './Modal';
import { AlertTriangle } from 'lucide-react';

interface ConfirmDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: () => void;
  title: string;
  message: string;
  confirmLabel?: string;
  isLoading?: boolean;
}

export default function ConfirmDialog({
  isOpen, onClose, onConfirm, title, message, confirmLabel = 'Delete', isLoading
}: ConfirmDialogProps) {
  return (
    <Modal isOpen={isOpen} onClose={onClose} title={title} size="sm">
      <div className="flex gap-4">
        <div className="w-10 h-10 bg-red-100 rounded-full flex items-center justify-center shrink-0">
          <AlertTriangle size={20} className="text-red-600" />
        </div>
        <div>
          <p className="text-sm text-gray-600">{message}</p>
          <div className="flex gap-3 mt-4 justify-end">
            <button onClick={onClose} className="btn-secondary">Cancel</button>
            <button onClick={onConfirm} disabled={isLoading} className="btn-danger">
              {isLoading ? 'Deleting...' : confirmLabel}
            </button>
          </div>
        </div>
      </div>
    </Modal>
  );
}
