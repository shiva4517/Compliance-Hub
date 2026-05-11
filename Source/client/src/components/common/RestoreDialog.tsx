import { AlertTriangle } from 'lucide-react';

interface RestoreDialogProps {
  isOpen: boolean;
  message: string;
  onRestore: () => void;
  onCreateNew: () => void;
  onClose: () => void;
  isLoading?: boolean;
}

export default function RestoreDialog({ isOpen, message, onRestore, onCreateNew, onClose, isLoading }: RestoreDialogProps) {
  if (!isOpen) return null;
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
      <div className="bg-white rounded-xl shadow-xl w-full max-w-md mx-4 p-6">
        <div className="flex items-start gap-3 mb-4">
          <AlertTriangle size={22} className="text-yellow-500 mt-0.5 shrink-0" />
          <div>
            <h2 className="text-base font-semibold text-gray-900">Record Already Exists</h2>
            <p className="text-sm text-gray-600 mt-1">{message}</p>
            <p className="text-sm text-gray-600 mt-2">
              Would you like to <strong>restore</strong> the existing record with the new details, or <strong>create a new</strong> separate record?
            </p>
          </div>
        </div>
        <div className="flex justify-end gap-3 mt-5">
          <button onClick={onClose} disabled={isLoading} className="btn-secondary">
            Cancel
          </button>
          <button onClick={onCreateNew} disabled={isLoading} className="btn-secondary">
            Create New
          </button>
          <button onClick={onRestore} disabled={isLoading} className="btn-primary">
            {isLoading ? 'Restoring...' : 'Restore'}
          </button>
        </div>
      </div>
    </div>
  );
}
