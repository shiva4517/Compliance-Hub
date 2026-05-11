import { useForm } from 'react-hook-form';
import type { User, UserRole } from '../../types';

interface UserFormData {
  firstName: string;
  lastName: string;
  email: string;
  password?: string;
  phoneNumber?: string;
  title?: string;
  role: UserRole;
  securityGroupId?: string;
  customerId?: string;
  refId?: string;
  isActive?: boolean;
}

interface UserFormProps {
  defaultValues?: Partial<User>;
  onSubmit: (data: UserFormData) => void;
  onCancel: () => void;
  isLoading?: boolean;
  submitLabel?: string;
  isEdit?: boolean;
}

export default function UserForm({ defaultValues, onSubmit, onCancel, isLoading, submitLabel = 'Create User', isEdit }: UserFormProps) {
  const { register, handleSubmit, formState: { errors } } = useForm<UserFormData>({
    defaultValues: {
      firstName: defaultValues?.firstName ?? '',
      lastName: defaultValues?.lastName ?? '',
      email: defaultValues?.email ?? '',
      password: '',
      phoneNumber: defaultValues?.phoneNumber ?? '',
      title: defaultValues?.title ?? '',
      role: defaultValues?.role ?? 'Employee',
      securityGroupId: defaultValues?.securityGroupId ?? '',
      refId: defaultValues?.refId ?? '',
      isActive: defaultValues?.isActive ?? true,
    }
  });

  const handleFormSubmit = (data: UserFormData) => {
    onSubmit({
      ...data,
      securityGroupId: data.securityGroupId || undefined,
      customerId: data.customerId || undefined,
      refId: data.refId || undefined,
      password: data.password || undefined,
    });
  };

  return (
    <form onSubmit={handleSubmit(handleFormSubmit)} className="space-y-4">
      <div className="grid grid-cols-2 gap-4">
        <div>
          <label className="form-label">First Name <span className="text-red-500">*</span></label>
          <input {...register('firstName', { required: 'Required' })} className="form-input" />
          {errors.firstName && <p className="text-red-500 text-xs mt-1">{errors.firstName.message}</p>}
        </div>
        <div>
          <label className="form-label">Last Name <span className="text-red-500">*</span></label>
          <input {...register('lastName', { required: 'Required' })} className="form-input" />
          {errors.lastName && <p className="text-red-500 text-xs mt-1">{errors.lastName.message}</p>}
        </div>
      </div>

      <div>
        <label className="form-label">Email <span className="text-red-500">*</span></label>
        <input {...register('email', { required: 'Required', pattern: { value: /^\S+@\S+$/i, message: 'Invalid email' } })}
          type="email" className="form-input" disabled={isEdit} />
        {errors.email && <p className="text-red-500 text-xs mt-1">{errors.email.message}</p>}
      </div>

      {/* {!isEdit && (
        <div>
          <label className="form-label">Password</label>
          <input {...register('password', {
            minLength: { value: 8, message: 'Min 8 characters' },
            pattern: { value: /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)/, message: 'Must contain uppercase, lowercase, and digit' }
          })} type="password" className="form-input" placeholder="Leave blank to auto-generate" />
          {errors.password && <p className="text-red-500 text-xs mt-1">{errors.password.message}</p>}
          <div className="flex items-start gap-1.5 mt-1.5 text-xs text-blue-600">
            <Info size={12} className="mt-0.5 shrink-0" />
            <span>A temporary password will be generated and emailed to the user if left blank.</span>
          </div>
        </div>
      )} */}

      <div className="grid grid-cols-2 gap-4">
        <div>
          <label className="form-label">Phone Number</label>
          <input {...register('phoneNumber')} className="form-input" />
        </div>
        {/* <div>
          <label className="form-label">Title</label>
          <input {...register('title')} className="form-input" />
        </div> */}
      </div>

      {/* <div className="grid grid-cols-2 gap-4">
        <div>
          <label className="form-label">Role <span className="text-red-500">*</span></label>
          <select {...register('role', { required: 'Required' })} className="form-input">
            {UserRoles.map(r => <option key={r} value={r}>{r}</option>)}
          </select>
        </div>
        <div>
          <label className="form-label">Employee / Reference ID</label>
          <input {...register('refId')} className="form-input" placeholder="Optional" />
        </div>
      </div> */}

      {/* <div className="grid grid-cols-2 gap-4">
        <div>
          <label className="form-label">Security Group</label>
          <select {...register('securityGroupId')} className="form-input">
            <option value="">-- Auto-assign --</option>
            {groups.map(g => (
              <option key={g.id} value={g.id}>{g.groupName}</option>
            ))}
          </select>
        </div>
        <div>
          <label className="form-label">Customer</label>
          <select {...register('customerId')} className="form-input">
            <option value="">-- None --</option>
            {customers.map(c => (
              <option key={c.id} value={c.id}>{c.customerName}</option>
            ))}
          </select>
        </div>
      </div> */}

      {isEdit && (
        <label className="flex items-center gap-2 cursor-pointer">
          <input {...register('isActive')} type="checkbox" className="w-4 h-4 rounded" />
          <span className="text-sm text-gray-700">Active</span>
        </label>
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
