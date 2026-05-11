import { useForm } from 'react-hook-form';
import type { GovernmentEntity } from '../../types';

interface Props {
  entity?: GovernmentEntity | null;
  onSubmit: (data: { titleNumber: number; titleName: string; source?: string; isSyncEnabled: boolean }) => void;
  isLoading?: boolean;
}

export default function GovernmentEntityForm({ entity, onSubmit, isLoading }: Props) {
  const { register, handleSubmit, formState: { errors } } = useForm({
    defaultValues: {
      titleNumber: entity?.titleNumber ?? 1,
      titleName: entity?.titleName ?? '',
      source: entity?.source ?? '',
      isSyncEnabled: entity?.isSyncEnabled ?? false,
    },
  });

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">Title Number</label>
        <input
          type="number"
          {...register('titleNumber', { required: 'Title number is required', min: { value: 1, message: 'Must be > 0' } })}
          className="form-input"
          disabled={!!entity}
        />
        {errors.titleNumber && <p className="text-red-600 text-xs mt-1">{errors.titleNumber.message}</p>}
      </div>
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">Title Name</label>
        <input
          type="text"
          {...register('titleName', { required: 'Title name is required' })}
          className="form-input"
          placeholder="e.g., Protection of Environment"
        />
        {errors.titleName && <p className="text-red-600 text-xs mt-1">{errors.titleName.message}</p>}
      </div>
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">Source URL</label>
        <input
          type="text"
          {...register('source')}
          className="form-input"
          placeholder="https://www.ecfr.gov/..."
        />
      </div>
      <div className="flex items-center gap-2">
        <input type="checkbox" {...register('isSyncEnabled')} id="syncEnabled" className="h-4 w-4 text-blue-600" />
        <label htmlFor="syncEnabled" className="text-sm font-medium text-gray-700">Enable Automatic Sync</label>
      </div>
      <button type="submit" disabled={isLoading} className="btn-primary w-full">
        {isLoading ? 'Saving...' : entity ? 'Update Title' : 'Add Title'}
      </button>
    </form>
  );
}
