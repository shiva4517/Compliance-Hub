import { useForm } from 'react-hook-form';
import type { Company } from '../../types';

type CompanyFormData = Omit<Company, 'id' | 'companyCode' | 'createdAt'>;

interface CompanyFormProps {
  defaultValues?: Partial<Company>;
  onSubmit: (data: CompanyFormData) => void;
  onCancel: () => void;
  isLoading?: boolean;
  submitLabel?: string;
  isEdit?: boolean;
}

export default function CompanyForm({ defaultValues, onSubmit, onCancel, isLoading, submitLabel = 'Create Company', isEdit }: CompanyFormProps) {
  const { register, handleSubmit, formState: { errors } } = useForm<CompanyFormData>({
    mode: 'onBlur',
    defaultValues: {
      companyName: defaultValues?.companyName ?? '',
      primaryEmail: defaultValues?.primaryEmail ?? '',
      secondaryEmail: defaultValues?.secondaryEmail ?? '',
      phoneNumber: defaultValues?.phoneNumber ?? '',
      primaryAddress: defaultValues?.primaryAddress ?? '',
      primaryCity: defaultValues?.primaryCity ?? '',
      primaryState: defaultValues?.primaryState ?? '',
      primaryPostalCode: defaultValues?.primaryPostalCode ?? '',
      secondaryAddress: defaultValues?.secondaryAddress ?? '',
      secondaryCity: defaultValues?.secondaryCity ?? '',
      secondaryState: defaultValues?.secondaryState ?? '',
      secondaryPostalCode: defaultValues?.secondaryPostalCode ?? '',
      websiteUrl: defaultValues?.websiteUrl ?? '',
      isActive: defaultValues?.isActive ?? true,
    }
  });

  const handleFormSubmit = (data: CompanyFormData) => {
    onSubmit({
      ...data,
      secondaryEmail: data.secondaryEmail || undefined,
      phoneNumber: data.phoneNumber || undefined,
      secondaryAddress: data.secondaryAddress || undefined,
      secondaryCity: data.secondaryCity || undefined,
      secondaryState: data.secondaryState || undefined,
      secondaryPostalCode: data.secondaryPostalCode || undefined,
      websiteUrl: data.websiteUrl || undefined,
    });
  };

  return (
    <form onSubmit={handleSubmit(handleFormSubmit)} className="space-y-5">

      {/* Basic Information */}
      <div>
        <h3 className="text-sm font-semibold text-gray-700 mb-3 pb-1 border-b border-gray-100">Basic Information</h3>
        <div className="space-y-3">
          <div>
            <label className="form-label">Company Name <span className="text-red-500">*</span></label>
            <input {...register('companyName', {
              required: 'Company name is required',
              minLength: { value: 2, message: 'Must be at least 2 characters' },
              maxLength: { value: 100, message: 'Must be 100 characters or fewer' },
            })} className="form-input" />
            {errors.companyName && <p className="text-red-500 text-xs mt-1">{errors.companyName.message}</p>}
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="form-label">Primary Email <span className="text-red-500">*</span></label>
              <input {...register('primaryEmail', {
                required: 'Primary email is required',
                pattern: { value: /^\S+@\S+\.\S+$/, message: 'Enter a valid email address' }
              })} type="email" className="form-input" />
              {errors.primaryEmail && <p className="text-red-500 text-xs mt-1">{errors.primaryEmail.message}</p>}
            </div>
            <div>
              <label className="form-label">Secondary Email</label>
              <input {...register('secondaryEmail', {
                pattern: { value: /^\S+@\S+\.\S+$/, message: 'Enter a valid email address' }
              })} type="email" className="form-input" />
              {errors.secondaryEmail && <p className="text-red-500 text-xs mt-1">{errors.secondaryEmail.message}</p>}
            </div>
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="form-label">Phone Number</label>
              <input {...register('phoneNumber', {
                pattern: {
                  value: /^[+]?[\d\s().-]{7,20}$/,
                  message: 'Enter a valid phone number (7-20 digits, may include + ( ) - spaces)',
                },
              })} className="form-input" placeholder="+1 (555) 000-0000" />
              {errors.phoneNumber && <p className="text-red-500 text-xs mt-1">{errors.phoneNumber.message}</p>}
            </div>
            <div>
              <label className="form-label">Website URL</label>
              <input {...register('websiteUrl', {
                pattern: {
                  value: /^https?:\/\/[^\s.]+\.[^\s]{2,}$/i,
                  message: 'Enter a valid URL starting with http:// or https://',
                },
              })} className="form-input" placeholder="https://example.com" />
              {errors.websiteUrl && <p className="text-red-500 text-xs mt-1">{errors.websiteUrl.message}</p>}
            </div>
          </div>
        </div>
      </div>

      {/* Primary Address */}
      <div>
        <h3 className="text-sm font-semibold text-gray-700 mb-3 pb-1 border-b border-gray-100">Primary Address</h3>
        <div className="space-y-3">
          <div>
            <label className="form-label">Address <span className="text-red-500">*</span></label>
            <input {...register('primaryAddress', {
              required: 'Address is required',
              maxLength: { value: 200, message: 'Must be 200 characters or fewer' },
            })} className="form-input" />
            {errors.primaryAddress && <p className="text-red-500 text-xs mt-1">{errors.primaryAddress.message}</p>}
          </div>
          <div className="grid grid-cols-3 gap-4">
            <div>
              <label className="form-label">City <span className="text-red-500">*</span></label>
              <input {...register('primaryCity', {
                required: 'City is required',
                pattern: { value: /^[A-Za-z\s.'-]{2,}$/, message: 'City must contain only letters' },
              })} className="form-input" />
              {errors.primaryCity && <p className="text-red-500 text-xs mt-1">{errors.primaryCity.message}</p>}
            </div>
            <div>
              <label className="form-label">State <span className="text-red-500">*</span></label>
              <input {...register('primaryState', {
                required: 'State is required',
                pattern: { value: /^[A-Za-z\s.'-]{2,}$/, message: 'State must contain only letters' },
              })} className="form-input" />
              {errors.primaryState && <p className="text-red-500 text-xs mt-1">{errors.primaryState.message}</p>}
            </div>
            <div>
              <label className="form-label">Postal Code <span className="text-red-500">*</span></label>
              <input {...register('primaryPostalCode', {
                required: 'Postal code is required',
                pattern: {
                  value: /^[A-Za-z0-9\s-]{5,10}$/,
                  message: 'Postal code must be 5-10 characters (letters, digits, spaces, hyphens)',
                },
              })} className="form-input" maxLength={10} />
              {errors.primaryPostalCode && <p className="text-red-500 text-xs mt-1">{errors.primaryPostalCode.message}</p>}
            </div>
          </div>
        </div>
      </div>

      {/* Secondary Address */}
      <div>
        <h3 className="text-sm font-semibold text-gray-700 mb-3 pb-1 border-b border-gray-100">Secondary Address <span className="text-xs text-gray-400 font-normal">(optional)</span></h3>
        <div className="space-y-3">
          <div>
            <label className="form-label">Address</label>
            <input {...register('secondaryAddress')} className="form-input" />
          </div>
          <div className="grid grid-cols-3 gap-4">
            <div>
              <label className="form-label">City</label>
              <input {...register('secondaryCity', {
                pattern: { value: /^[A-Za-z\s.'-]{2,}$/, message: 'City must contain only letters' },
              })} className="form-input" />
              {errors.secondaryCity && <p className="text-red-500 text-xs mt-1">{errors.secondaryCity.message}</p>}
            </div>
            <div>
              <label className="form-label">State</label>
              <input {...register('secondaryState', {
                pattern: { value: /^[A-Za-z\s.'-]{2,}$/, message: 'State must contain only letters' },
              })} className="form-input" />
              {errors.secondaryState && <p className="text-red-500 text-xs mt-1">{errors.secondaryState.message}</p>}
            </div>
            <div>
              <label className="form-label">Postal Code</label>
              <input {...register('secondaryPostalCode', {
                pattern: {
                  value: /^[A-Za-z0-9\s-]{5,10}$/,
                  message: 'Postal code must be 5-10 characters',
                },
              })} className="form-input" maxLength={10} />
              {errors.secondaryPostalCode && <p className="text-red-500 text-xs mt-1">{errors.secondaryPostalCode.message}</p>}
            </div>
          </div>
        </div>
      </div>

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
