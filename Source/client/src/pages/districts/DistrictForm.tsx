import { useForm } from 'react-hook-form';
import type { District } from '../../types';

interface FormData {
  name: string;
  description?: string;
}

interface Props {
  defaultValues?: Partial<District>;
  onSubmit: (data: FormData) => void;
  onCancel: () => void;
  isLoading?: boolean;
  submitLabel?: string;
  serverError?: string;
}

export default function DistrictForm({
  defaultValues, onSubmit, onCancel, isLoading, submitLabel = 'Create District', serverError
}: Props) {
  const { register, handleSubmit, formState: { errors } } = useForm<FormData>({
    defaultValues: {
      name: defaultValues?.name ?? '',
      description: defaultValues?.description ?? '',
    }
  });

  return (
    <form onSubmit={handleSubmit(data => onSubmit({ ...data, description: data.description || undefined }))}
      className="space-y-4">

      <div>
        <label className="form-label">District Name <span className="text-red-500">*</span></label>
        <input {...register('name', {
          required: 'District name is required.',
          maxLength: { value: 200, message: 'Max 200 characters.' }
        })} className="form-input" placeholder="e.g. North District, Downtown, East Zone" />
        {errors.name && <p className="text-red-500 text-xs mt-1">{errors.name.message}</p>}
      </div>

      <div>
        <label className="form-label">Description</label>
        <textarea {...register('description', {
          maxLength: { value: 1000, message: 'Max 1000 characters.' }
        })} rows={3} className="form-input resize-none" placeholder="Optional description..." />
        {errors.description && <p className="text-red-500 text-xs mt-1">{errors.description.message}</p>}
      </div>

      {serverError && (
        <div className="bg-red-50 text-red-700 text-sm px-3 py-2 rounded-lg border border-red-200">
          {serverError}
        </div>
      )}

      <div className="flex justify-end gap-3 pt-2 border-t border-gray-200">
        <button type="button" onClick={onCancel} className="btn-secondary">Cancel</button>
        <button type="submit" disabled={isLoading} className="btn-primary">
          {isLoading ? 'Saving...' : submitLabel}
        </button>
      </div>
    </form>
  );
}
