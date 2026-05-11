import { useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { Bot } from 'lucide-react';
import Modal from '../../components/common/Modal';
import { subscriptionService } from '../../services/subscriptionService';
import { regulationService } from '../../services/regulationService';
import { lookupService } from '../../services/lookupService';
import { handleApiError } from '../../services/api';

interface Props {
  subscriptionId: string;
  onClose: () => void;
}

interface FormData {
  description: string;
  condition: string;
  suggestedTask: string;
  frequencyTypeId: string;
  dueDateTypeId: string;
  minValue: string;
  maxValue: string;
}

const fieldClass = 'w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500';
const labelClass = 'block text-sm font-medium text-gray-700 mb-1';

export default function SubscriptionEditTaskModal({ subscriptionId, onClose }: Props) {
  const qc = useQueryClient();

  const { data: detail } = useQuery({
    queryKey: ['subscription-detail', subscriptionId],
    queryFn: () => subscriptionService.getDetail(subscriptionId),
  });

  const { data: frequencyTypes = [] } = useQuery({
    queryKey: ['frequency-types'],
    queryFn: lookupService.getFrequencyTypes,
  });

  const { data: dueDateTypes = [] } = useQuery({
    queryKey: ['due-date-types'],
    queryFn: lookupService.getDueDateTypes,
  });

  const { data: aiConnection } = useQuery({
    queryKey: ['regulation-ai-connection'],
    queryFn: () => regulationService.getAiConnection(),
  });

  const { register, handleSubmit, reset, setValue, getValues, formState: { isSubmitting } } = useForm<FormData>({
    defaultValues: {
      description: '', condition: '', suggestedTask: '',
      frequencyTypeId: '', dueDateTypeId: '', minValue: '', maxValue: '',
    },
  });

  useEffect(() => {
    if (detail) {
      reset({
        description: detail.description ?? '',
        condition: detail.condition ?? '',
        suggestedTask: detail.suggestedTask ?? '',
        frequencyTypeId: detail.frequencyTypeId ?? '',
        dueDateTypeId: detail.dueDateTypeId ?? '',
        minValue: detail.minValue?.toString() ?? '',
        maxValue: detail.maxValue?.toString() ?? '',
      });
    }
  }, [detail, reset]);

  const upsertMutation = useMutation({
    mutationFn: (form: FormData) =>
      subscriptionService.upsertDetail(subscriptionId, {
        description: form.description || undefined,
        condition: form.condition || undefined,
        suggestedTask: form.suggestedTask || undefined,
        frequencyTypeId: form.frequencyTypeId || undefined,
        dueDateTypeId: form.dueDateTypeId || undefined,
        minValue: form.minValue === '' ? undefined : Number(form.minValue),
        maxValue: form.maxValue === '' ? undefined : Number(form.maxValue),
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['subscription-detail', subscriptionId] });
      onClose();
    },
  });

  const suggestMutation = useMutation({
    mutationFn: () => {
      const minStr = getValues('minValue');
      const maxStr = getValues('maxValue');
      return subscriptionService.suggestDetail(subscriptionId, {
        description: getValues('description'),
        condition: getValues('condition'),
        suggestedTask: getValues('suggestedTask'),
        minValue: minStr === '' ? undefined : Number(minStr),
        maxValue: maxStr === '' ? undefined : Number(maxStr),
      });
    },
    onSuccess: s => {
      setValue('description', s.description, { shouldDirty: true });
      setValue('condition', s.condition, { shouldDirty: true });
      setValue('suggestedTask', s.suggestedTask, { shouldDirty: true });
      const f = frequencyTypes.find(x => x.name.toLowerCase() === s.frequencyType?.toLowerCase());
      const d = dueDateTypes.find(x => x.name.toLowerCase() === s.dueDateType?.toLowerCase());
      if (f) setValue('frequencyTypeId', f.id, { shouldDirty: true });
      if (d) setValue('dueDateTypeId', d.id, { shouldDirty: true });
      // Data Range — AI now populates these when the condition is a numeric threshold.
      setValue('minValue', s.minValue != null ? String(s.minValue) : '', { shouldDirty: true });
      setValue('maxValue', s.maxValue != null ? String(s.maxValue) : '', { shouldDirty: true });
    },
  });

  const aiError = suggestMutation.isError ? handleApiError(suggestMutation.error) : null;
  const saveError = upsertMutation.isError ? handleApiError(upsertMutation.error) : null;

  return (
    <Modal isOpen onClose={onClose} title="Edit Task Detail" size="lg">
      <form onSubmit={handleSubmit(form => upsertMutation.mutate(form))} className="space-y-4">
        <div className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-blue-100 bg-blue-50 px-4 py-3">
          <div>
            <p className="text-sm font-medium text-blue-900">AI-assisted drafting</p>
            <p className="text-xs text-blue-700">
              {aiConnection ? 'Use AI to generate draft values for this subscription task.' : (
                <>No AI provider connected. <Link to="/ai-provider-settings" className="font-medium underline hover:text-blue-900">Configure AI Provider</Link></>
              )}
            </p>
          </div>
          <button
            type="button"
            onClick={() => aiConnection && suggestMutation.mutate()}
            disabled={!aiConnection || suggestMutation.isPending}
            className="inline-flex items-center gap-2 rounded-lg bg-blue-600 px-3 py-2 text-sm text-white hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-50">
            <Bot size={16} />
            {suggestMutation.isPending ? 'Generating...' : 'AI Suggestion'}
          </button>
        </div>
        {aiError && <p className="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-600">{aiError}</p>}

        <p className="text-xs italic text-gray-500">
          Note: Description, Condition, Suggested Task, Frequency Type, Due Date Type and Data Range may be generated by AI.
          AI can make mistakes — please review the values before saving.
        </p>

        <div>
          <label className={labelClass}>Description</label>
          <textarea {...register('description')} rows={3} className={fieldClass} placeholder="Enter description..." />
        </div>
        <div>
          <label className={labelClass}>Condition</label>
          <textarea {...register('condition')} rows={3} className={fieldClass} placeholder="Enter condition..." />
        </div>

        {/* Data Range — appears directly below Condition; applies to condition-related configurations. */}
        <fieldset className="rounded-lg border border-gray-200 p-3">
          <legend className="px-2 text-xs font-semibold text-gray-600 uppercase tracking-wide">Data Range</legend>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className={labelClass}>Min Value</label>
              <input type="number" step="any" {...register('minValue')} className={fieldClass} placeholder="e.g. 0" />
            </div>
            <div>
              <label className={labelClass}>Max Value</label>
              <input type="number" step="any" {...register('maxValue')} className={fieldClass} placeholder="e.g. 100" />
            </div>
          </div>
        </fieldset>

        <div>
          <label className={labelClass}>Suggested Task</label>
          <textarea {...register('suggestedTask')} rows={3} className={fieldClass} placeholder="Enter suggested task..." />
        </div>
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className={labelClass}>Frequency Type</label>
            <select {...register('frequencyTypeId')} className={fieldClass}>
              <option value="">-- None --</option>
              {frequencyTypes.map(f => <option key={f.id} value={f.id}>{f.name}</option>)}
            </select>
          </div>
          <div>
            <label className={labelClass}>Due Date Type</label>
            <select {...register('dueDateTypeId')} className={fieldClass}>
              <option value="">-- None --</option>
              {dueDateTypes.map(d => <option key={d.id} value={d.id}>{d.name}</option>)}
            </select>
          </div>
        </div>

        {saveError && <p className="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-600">{saveError}</p>}

        <div className="flex justify-end gap-3 pt-2">
          <button type="button" onClick={onClose} className="rounded-lg border border-gray-300 px-4 py-2 text-sm hover:bg-gray-50">Cancel</button>
          <button type="submit" disabled={isSubmitting || upsertMutation.isPending}
            className="rounded-lg bg-blue-600 px-4 py-2 text-sm text-white hover:bg-blue-700 disabled:opacity-50">
            {upsertMutation.isPending ? 'Saving...' : 'Save'}
          </button>
        </div>
      </form>
    </Modal>
  );
}
