import { useForm } from 'react-hook-form';
import type { Customer } from '../../types';
import type { CustomerFormData } from '../../services/customerService';

interface Props {
  companyId: string;
  defaultValues?: Partial<Customer>;
  onSubmit: (data: CustomerFormData) => void;
  onCancel: () => void;
  isLoading?: boolean;
  submitLabel?: string;
  serverError?: string;
}

export default function CustomerForm({
  companyId, defaultValues, onSubmit, onCancel, isLoading, submitLabel = 'Create Customer', serverError
}: Props) {
  const { register, handleSubmit, formState: { errors } } = useForm<CustomerFormData>({
    mode: 'onBlur',
    defaultValues: {
      companyId,
      customerName: defaultValues?.customerName ?? '',
      primaryContactFirstName: defaultValues?.primaryContactFirstName ?? '',
      primaryContactLastName: defaultValues?.primaryContactLastName ?? '',
      primaryEmail: defaultValues?.primaryEmail ?? '',
      secondaryEmail: defaultValues?.secondaryEmail ?? '',
      phoneNumber: defaultValues?.phoneNumber ?? '',
      mobileNumber: defaultValues?.mobileNumber ?? '',
      primaryAddress: defaultValues?.primaryAddress ?? '',
      primaryCity: defaultValues?.primaryCity ?? '',
      primaryState: defaultValues?.primaryState ?? '',
      primaryPostalCode: defaultValues?.primaryPostalCode ?? '',
      secondaryAddress: defaultValues?.secondaryAddress ?? '',
      secondaryCity: defaultValues?.secondaryCity ?? '',
      secondaryState: defaultValues?.secondaryState ?? '',
      secondaryPostalCode: defaultValues?.secondaryPostalCode ?? '',
    }
  });

  const handleFormSubmit = (data: CustomerFormData) => {
    onSubmit({
      ...data,
      companyId,
      secondaryEmail: data.secondaryEmail || undefined,
      phoneNumber: data.phoneNumber || undefined,
      mobileNumber: data.mobileNumber || undefined,
      primaryAddress: data.primaryAddress || undefined,
      primaryCity: data.primaryCity || undefined,
      primaryState: data.primaryState || undefined,
      primaryPostalCode: data.primaryPostalCode || undefined,
      secondaryAddress: data.secondaryAddress || undefined,
      secondaryCity: data.secondaryCity || undefined,
      secondaryState: data.secondaryState || undefined,
      secondaryPostalCode: data.secondaryPostalCode || undefined,
    });
  };

  return (
    <form onSubmit={handleSubmit(handleFormSubmit)} className="space-y-5">

      {/* Basic Info */}
      <div>
        <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-3">Customer Information</h3>
        <div>
          <label className="form-label">Customer Name <span className="text-red-500">*</span></label>
          <input {...register('customerName', {
            required: 'Customer name is required.',
            minLength: { value: 2, message: 'Must be at least 2 characters.' },
            maxLength: { value: 200, message: 'Max 200 characters.' }
          })} className="form-input" placeholder="e.g. Alcoa Rockdale Smelter" />
          {errors.customerName && <p className="text-red-500 text-xs mt-1">{errors.customerName.message}</p>}
        </div>
      </div>

      {/* Primary Contact */}
      <div>
        <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-3">Primary Contact</h3>
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="form-label">First Name <span className="text-red-500">*</span></label>
            <input {...register('primaryContactFirstName', {
              required: 'First name is required.',
              pattern: { value: /^[A-Za-z\s.'-]{1,}$/, message: 'Use letters only.' },
              maxLength: { value: 100, message: 'Max 100 characters.' }
            })} className="form-input" placeholder="First name" />
            {errors.primaryContactFirstName && <p className="text-red-500 text-xs mt-1">{errors.primaryContactFirstName.message}</p>}
          </div>
          <div>
            <label className="form-label">Last Name <span className="text-red-500">*</span></label>
            <input {...register('primaryContactLastName', {
              required: 'Last name is required.',
              pattern: { value: /^[A-Za-z\s.'-]{1,}$/, message: 'Use letters only.' },
              maxLength: { value: 100, message: 'Max 100 characters.' }
            })} className="form-input" placeholder="Last name" />
            {errors.primaryContactLastName && <p className="text-red-500 text-xs mt-1">{errors.primaryContactLastName.message}</p>}
          </div>
          <div>
            <label className="form-label">Primary Email <span className="text-red-500">*</span></label>
            <input type="email" {...register('primaryEmail', {
              required: 'Primary email is required.',
              pattern: { value: /^[^\s@]+@[^\s@]+\.[^\s@]+$/, message: 'Invalid email format.' },
              maxLength: { value: 200, message: 'Max 200 characters.' }
            })} className="form-input" placeholder="contact@example.com" />
            {errors.primaryEmail && <p className="text-red-500 text-xs mt-1">{errors.primaryEmail.message}</p>}
          </div>
          <div>
            <label className="form-label">Secondary Email</label>
            <input type="email" {...register('secondaryEmail', {
              pattern: { value: /^[^\s@]+@[^\s@]+\.[^\s@]+$/, message: 'Invalid email format.' },
              maxLength: { value: 200, message: 'Max 200 characters.' }
            })} className="form-input" placeholder="alt@example.com" />
            {errors.secondaryEmail && <p className="text-red-500 text-xs mt-1">{errors.secondaryEmail.message}</p>}
          </div>
          <div>
            <label className="form-label">Phone Number</label>
            <input {...register('phoneNumber', {
              maxLength: { value: 20, message: 'Max 20 characters.' },
              pattern: { value: /^[+]?[\d\s().-]{7,20}$/, message: 'Enter a valid phone (7-20 digits, may include + ( ) - spaces).' },
            })}
              className="form-input" placeholder="+1 (555) 000-0000" />
            {errors.phoneNumber && <p className="text-red-500 text-xs mt-1">{errors.phoneNumber.message}</p>}
          </div>
          <div>
            <label className="form-label">Mobile Number</label>
            <input {...register('mobileNumber', {
              maxLength: { value: 20, message: 'Max 20 characters.' },
              pattern: { value: /^[+]?[\d\s().-]{7,20}$/, message: 'Enter a valid mobile number.' },
            })}
              className="form-input" placeholder="+1 (555) 000-0000" />
            {errors.mobileNumber && <p className="text-red-500 text-xs mt-1">{errors.mobileNumber.message}</p>}
          </div>
        </div>
      </div>

    {/* Primary Address */}
<div>
  <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-3">
    Primary Address
  </h3>

  <div className="grid grid-cols-2 gap-4">

    {/* Address */}
    <div className="col-span-2">
      <label className="form-label">
        Street Address <span className="text-red-500">*</span>
      </label>
      <input
        {...register('primaryAddress', {
          required: 'Address is required',
          maxLength: {
            value: 300,
            message: 'Max 300 characters.',
          },
        })}
        className="form-input"
        placeholder="123 Main St"
      />
      {errors.primaryAddress && (
        <p className="text-red-500 text-xs mt-1">
          {errors.primaryAddress.message}
        </p>
      )}
    </div>

    {/* City */}
    <div>
      <label className="form-label">
        City <span className="text-red-500">*</span>
      </label>
      <input
        {...register('primaryCity', {
          required: 'City is required',
          maxLength: {
            value: 100,
            message: 'Max 100 characters.',
          },
          pattern: {
            value: /^[A-Za-z\s.'-]{2,}$/,
            message: 'City must contain only letters.',
          },
        })}
        className="form-input"
        placeholder="Austin"
      />
      {errors.primaryCity && (
        <p className="text-red-500 text-xs mt-1">
          {errors.primaryCity.message}
        </p>
      )}
    </div>

    {/* State */}
    <div>
      <label className="form-label">
        State <span className="text-red-500">*</span>
      </label>
      <input
        {...register('primaryState', {
          required: 'State is required',
          maxLength: {
            value: 50,
            message: 'Max 50 characters.',
          },
          pattern: {
            value: /^[A-Za-z\s.'-]{2,}$/,
            message: 'State must contain only letters.',
          },
        })}
        className="form-input"
        placeholder="TX"
      />
      {errors.primaryState && (
        <p className="text-red-500 text-xs mt-1">
          {errors.primaryState.message}
        </p>
      )}
    </div>

    {/* Postal Code */}
    <div>
      <label className="form-label">
        Postal Code <span className="text-red-500">*</span>
      </label>
      <input
        {...register('primaryPostalCode', {
          required: 'Postal code is required',
          maxLength: {
            value: 10,
            message: 'Max 10 characters.',
          },
          pattern: {
            value: /^[A-Za-z0-9\s-]{5,10}$/,
            message:
              'Postal code must be 5-10 characters (letters, digits, spaces, hyphens)',
          },
        })}
        className="form-input"
        placeholder="78701"
      />
      {errors.primaryPostalCode && (
        <p className="text-red-500 text-xs mt-1">
          {errors.primaryPostalCode.message}
        </p>
      )}
    </div>

  </div>
</div>

      {/* Secondary Address */}
      <div>
        <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-3">
          Secondary Address <span className="font-normal normal-case text-gray-400">(optional)</span>
        </h3>
        <div className="grid grid-cols-2 gap-4">
          <div className="col-span-2">
            <label className="form-label">Street Address</label>
            <input {...register('secondaryAddress', { maxLength: { value: 300, message: 'Max 300 characters.' } })}
              className="form-input" placeholder="456 Oak Ave" />
          </div>
          <div>
            <label className="form-label">City</label>
            <input {...register('secondaryCity', {
              maxLength: { value: 100, message: 'Max 100 characters.' },
              pattern: { value: /^[A-Za-z\s.'-]{2,}$/, message: 'City must contain only letters.' },
            })}
              className="form-input" placeholder="Houston" />
            {errors.secondaryCity && <p className="text-red-500 text-xs mt-1">{errors.secondaryCity.message}</p>}
          </div>
          <div>
            <label className="form-label">State</label>
            <input {...register('secondaryState', {
              maxLength: { value: 50, message: 'Max 50 characters.' },
              pattern: { value: /^[A-Za-z\s.'-]{2,}$/, message: 'State must contain only letters.' },
            })}
              className="form-input" placeholder="TX" />
            {errors.secondaryState && <p className="text-red-500 text-xs mt-1">{errors.secondaryState.message}</p>}
          </div>
          <div>
            <label className="form-label">Postal Code</label>
            <input {...register('secondaryPostalCode', {
              maxLength: { value: 10, message: 'Max 10 characters.' },
              pattern: { value: /^[A-Za-z0-9\s-]{5,10}$/, message: 'Postal code must be 5-10 characters.' },
            })}
              className="form-input" placeholder="77001" />
            {errors.secondaryPostalCode && <p className="text-red-500 text-xs mt-1">{errors.secondaryPostalCode.message}</p>}
          </div>
        </div>
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
