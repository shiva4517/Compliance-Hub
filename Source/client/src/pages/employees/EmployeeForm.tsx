import { useForm } from 'react-hook-form';
import { useQuery } from '@tanstack/react-query';
import type { Employee } from '../../types';
import type { CreateEmployeePayload, UpdateEmployeePayload } from '../../services/employeeService';
import { departmentService } from '../../services/departmentService';
import { companyDivisionService } from '../../services/companyDivisionService';
import { districtService } from '../../services/districtService';

type EmployeeFormData = {
  firstName: string;
  lastName: string;
  primaryEmail: string;
  secondaryEmail?: string;
  phoneNumber?: string;
  mobileNumber?: string;
  departmentId?: string;
  companyDivisionId?: string;
  companyDistrictId?: string;
  primaryAddress: string;
  primaryCity: string;
  primaryState: string;
  primaryPostalCode: string;
  secondaryAddress?: string;
  secondaryCity?: string;
  secondaryState?: string;
  secondaryPostalCode?: string;
  isActive: boolean;
};

interface EmployeeFormProps {
  companyId: string;
  defaultValues?: Partial<Employee>;
  onSubmit: (data: CreateEmployeePayload | UpdateEmployeePayload) => void;
  onCancel: () => void;
  isLoading?: boolean;
  submitLabel?: string;
  isEdit?: boolean;
}

export default function EmployeeForm({
  companyId, defaultValues, onSubmit, onCancel, isLoading, submitLabel = 'Create Employee', isEdit
}: EmployeeFormProps) {
  const { register, handleSubmit, watch, formState: { errors } } = useForm<EmployeeFormData>({
    mode: 'onBlur',
    defaultValues: {
      firstName: defaultValues?.firstName ?? '',
      lastName: defaultValues?.lastName ?? '',
      primaryEmail: defaultValues?.primaryEmail ?? '',
      secondaryEmail: defaultValues?.secondaryEmail ?? '',
      phoneNumber: defaultValues?.phoneNumber ?? '',
      mobileNumber: defaultValues?.mobileNumber ?? '',
      departmentId: defaultValues?.departmentId ?? '',
      companyDivisionId: defaultValues?.companyDivisionId ?? '',
      companyDistrictId: defaultValues?.companyDistrictId ?? '',
      primaryAddress: defaultValues?.primaryAddress ?? '',
      primaryCity: defaultValues?.primaryCity ?? '',
      primaryState: defaultValues?.primaryState ?? '',
      primaryPostalCode: defaultValues?.primaryPostalCode ?? '',
      secondaryAddress: defaultValues?.secondaryAddress ?? '',
      secondaryCity: defaultValues?.secondaryCity ?? '',
      secondaryState: defaultValues?.secondaryState ?? '',
      secondaryPostalCode: defaultValues?.secondaryPostalCode ?? '',
      isActive: defaultValues?.isActive ?? true,
    }
  });

  const selectedDepartmentId = watch('departmentId');
  const selectedDivisionId = watch('companyDivisionId');

  const { data: departmentsData } = useQuery({
    queryKey: ['departments', companyId],
    queryFn: () => departmentService.getAll(companyId, undefined, 1, 500),
    enabled: !!companyId,
  });

  const { data: divisionsData } = useQuery({
    queryKey: ['company-divisions', companyId, selectedDepartmentId],
    queryFn: () => companyDivisionService.getAll(companyId, undefined, 1, 500),
    enabled: !!companyId && !!selectedDepartmentId,
  });

  const { data: districtsData } = useQuery({
    queryKey: ['districts', companyId],
    queryFn: () => districtService.getAll(companyId, undefined, 1, 500),
    enabled: !!companyId,
  });

  const activeDepartments = departmentsData?.items.filter(d => d.isActive) ?? [];
  const activeDivisions = (divisionsData?.items ?? []).filter(
    d => d.isActive && (!selectedDepartmentId || d.departmentId === selectedDepartmentId)
  );
  const activeDistricts = districtsData?.items.filter(d => d.isActive) ?? [];

  const handleFormSubmit = (data: EmployeeFormData) => {
    onSubmit({
      ...data,
      companyId,
      departmentId: data.departmentId || undefined,
      companyDivisionId: data.companyDivisionId || undefined,
      companyDistrictId: data.companyDistrictId || undefined,
      secondaryEmail: data.secondaryEmail || undefined,
      phoneNumber: data.phoneNumber || undefined,
      mobileNumber: data.mobileNumber || undefined,
      secondaryAddress: data.secondaryAddress || undefined,
      secondaryCity: data.secondaryCity || undefined,
      secondaryState: data.secondaryState || undefined,
      secondaryPostalCode: data.secondaryPostalCode || undefined,
    } as CreateEmployeePayload);
  };

  return (
    <form onSubmit={handleSubmit(handleFormSubmit)} className="space-y-5">

      {/* Assignment */}
      <div>
        <h3 className="text-sm font-semibold text-gray-700 mb-3 pb-1 border-b border-gray-100">Assignment</h3>
        <div className="grid grid-cols-3 gap-4">
          <div>
            <label className="form-label">Department</label>
            <select {...register('departmentId')} className="form-input">
              <option value="">Select department...</option>
              {activeDepartments.map(d => (
                <option key={d.id} value={d.id}>{d.name}</option>
              ))}
            </select>
          </div>
          <div>
            <label className="form-label">Division</label>
            <select {...register('companyDivisionId')} className="form-input"
              disabled={!selectedDepartmentId}>
              <option value="">Select division...</option>
              {activeDivisions.map(d => (
                <option key={d.id} value={d.id}>{d.name}</option>
              ))}
            </select>
          </div>
          <div>
            <label className="form-label">District</label>
            <select {...register('companyDistrictId')} className="form-input"
              disabled={!selectedDivisionId}>
              <option value="">Select district...</option>
              {activeDistricts.map(d => (
                <option key={d.id} value={d.id}>{d.name}</option>
              ))}
            </select>
          </div>
        </div>
      </div>

      {/* Personal Information */}
      <div>
        <h3 className="text-sm font-semibold text-gray-700 mb-3 pb-1 border-b border-gray-100">Personal Information</h3>
        <div className="space-y-3">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="form-label">First Name <span className="text-red-500">*</span></label>
              <input {...register('firstName', {
                required: 'First name is required',
                pattern: { value: /^[A-Za-z\s.'-]{1,}$/, message: 'Use letters only' },
                maxLength: { value: 100, message: 'Must be 100 characters or fewer' },
              })} className="form-input" />
              {errors.firstName && <p className="text-red-500 text-xs mt-1">{errors.firstName.message}</p>}
            </div>
            <div>
              <label className="form-label">Last Name <span className="text-red-500">*</span></label>
              <input {...register('lastName', {
                required: 'Last name is required',
                pattern: { value: /^[A-Za-z\s.'-]{1,}$/, message: 'Use letters only' },
                maxLength: { value: 100, message: 'Must be 100 characters or fewer' },
              })} className="form-input" />
              {errors.lastName && <p className="text-red-500 text-xs mt-1">{errors.lastName.message}</p>}
            </div>
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="form-label">Primary Email <span className="text-red-500">*</span></label>
              <input {...register('primaryEmail', {
                required: 'Primary email is required',
                pattern: { value: /^\S+@\S+\.\S+$/, message: 'Enter a valid email address' }
              })} type="email" className="form-input" disabled={isEdit} />
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
                pattern: { value: /^[+]?[\d\s().-]{7,20}$/, message: 'Enter a valid phone number' },
              })} className="form-input" placeholder="+1 (555) 000-0000" />
              {errors.phoneNumber && <p className="text-red-500 text-xs mt-1">{errors.phoneNumber.message}</p>}
            </div>
            <div>
              <label className="form-label">Mobile Number</label>
              <input {...register('mobileNumber', {
                pattern: { value: /^[+]?[\d\s().-]{7,20}$/, message: 'Enter a valid mobile number' },
              })} className="form-input" placeholder="+1 (555) 000-0000" />
              {errors.mobileNumber && <p className="text-red-500 text-xs mt-1">{errors.mobileNumber.message}</p>}
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
        <h3 className="text-sm font-semibold text-gray-700 mb-3 pb-1 border-b border-gray-100">
          Secondary Address <span className="text-xs text-gray-400 font-normal">(optional)</span>
        </h3>
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
