import { useForm } from 'react-hook-form';
import { useQuery } from '@tanstack/react-query';
import { useAuth } from '../../contexts/AuthContext';
import type { CompanyDivision } from '../../types';
import { departmentService } from '../../services/departmentService';

interface DivisionFormData {
  departmentId?: string;
  name: string;
  description?: string;
}

interface Props {
  defaultValues?: Partial<CompanyDivision>;
  onSubmit: (data: DivisionFormData) => void;
  onCancel: () => void;
  isLoading?: boolean;
  submitLabel?: string;
  serverError?: string;
}

export default function CompanyDivisionForm({
  defaultValues, onSubmit, onCancel, isLoading, submitLabel = 'Create Division', serverError
}: Props) {
  const { user } = useAuth();
  const companyId = user!.userId;

  const { register, handleSubmit, formState: { errors } } = useForm<DivisionFormData>({
    defaultValues: {
      departmentId: defaultValues?.departmentId ?? '',
      name: defaultValues?.name ?? '',
      description: defaultValues?.description ?? '',
    }
  });

  const { data: deptsPage } = useQuery({
    queryKey: ['departments', companyId],
    queryFn: () => departmentService.getAll(companyId, undefined, 1, 500),
  });
  const departments = (deptsPage?.items ?? []).filter(d => d.isActive);

  const handleFormSubmit = (data: DivisionFormData) => {
    onSubmit({
      ...data,
      departmentId: data.departmentId || undefined,
      description: data.description || undefined,
    });
  };

  return (
    <form onSubmit={handleSubmit(handleFormSubmit)} className="space-y-4">

      <div>
        <label className="form-label">Department</label>
        <select {...register('departmentId')} className="form-input">
          <option value="">-- No department --</option>
          {departments.map(d => (
            <option key={d.id} value={d.id}>{d.name}</option>
          ))}
        </select>
      </div>

      <div>
        <label className="form-label">Division Name <span className="text-red-500">*</span></label>
        <input {...register('name', {
          required: 'Division name is required.',
          maxLength: { value: 200, message: 'Max 200 characters.' }
        })} className="form-input" placeholder="e.g. Engineering, Finance, Operations" />
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
