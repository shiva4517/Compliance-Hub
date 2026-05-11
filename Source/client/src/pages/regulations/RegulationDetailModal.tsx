import { useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Bot } from 'lucide-react';
import Modal from '../../components/common/Modal';
import { regulationService } from '../../services/regulationService';
import { lookupService } from '../../services/lookupService';
import { handleApiError } from '../../services/api';

interface Props {
  regulationId: string;
  sectionLabel: string;
  mode: 'view' | 'edit';
  onClose: () => void;
  onSaveSuccess?: (sectionLabel: string) => void;
}

interface FormData {
  description: string;
  condition: string;
  suggestedTask: string;
  frequencyTypeId: string;
  dueDateTypeId: string;
}

function toPlainText(content: string | null | undefined) {
  if (!content) {
    return '';
  }

  let normalized = content;
  const originsIndex = normalized.indexOf('{"origins"');
  if (originsIndex >= 0) {
    normalized = normalized.slice(0, originsIndex);
  }

  normalized = normalized
    .replace(/<[^>]+>/g, ' ')
    .replace(/&nbsp;/gi, ' ')
    .replace(/&amp;/gi, '&')
    .replace(/&lt;/gi, '<')
    .replace(/&gt;/gi, '>')
    .replace(/\s+/g, ' ')
    .trim();

  normalized = normalized
    .replace(/\s*§\s*/g, '§ ')
    .replace(/\s+\(\s*([a-z])\s*\)/gi, '\n\n($1) ')
    .replace(/([.?!])\s+\(\s*([a-z])\s*\)/gi, '$1\n\n($2) ')
    .replace(/\s{2,}/g, ' ')
    .replace(/\n{3,}/g, '\n\n')
    .trim();

  return normalized;
}

export default function RegulationDetailModal({ regulationId, sectionLabel, mode, onClose, onSaveSuccess }: Props) {
  const queryClient = useQueryClient();

  const { data: detail, isLoading: loadingDetail } = useQuery({
    queryKey: ['regulation-detail', regulationId],
    queryFn: () => regulationService.getDetail(regulationId),
  });

  const { data: sectionContent, isLoading: loadingSectionContent } = useQuery({
    queryKey: ['regulation-section-content', regulationId],
    queryFn: () => regulationService.getSectionContent(regulationId),
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
    enabled: mode === 'edit',
  });

  const {
    register,
    handleSubmit,
    reset,
    setValue,
    getValues,
    formState: { isSubmitting },
  } = useForm<FormData>();

  useEffect(() => {
    if (detail) {
      reset({
        description: detail.description ?? '',
        condition: detail.condition ?? '',
        suggestedTask: detail.suggestedTask ?? '',
        frequencyTypeId: detail.frequencyTypeId ?? '',
        dueDateTypeId: detail.dueDateTypeId ?? '',
      });
    }
  }, [detail, reset]);

  const mutation = useMutation({
    mutationFn: (data: FormData) =>
      regulationService.upsertDetail(regulationId, {
        description: data.description || undefined,
        condition: data.condition || undefined,
        suggestedTask: data.suggestedTask || undefined,
        frequencyTypeId: data.frequencyTypeId || undefined,
        dueDateTypeId: data.dueDateTypeId || undefined,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['regulation-detail', regulationId] });
      onClose();
      onSaveSuccess?.(sectionLabel);
    },
  });

  const suggestMutation = useMutation({
    mutationFn: () =>
      regulationService.suggestDetail(regulationId, {
        description: getValues('description'),
        condition: getValues('condition'),
        suggestedTask: getValues('suggestedTask'),
      }),
    onSuccess: suggestion => {
      setValue('description', suggestion.description, { shouldDirty: true });
      setValue('condition', suggestion.condition, { shouldDirty: true });
      setValue('suggestedTask', suggestion.suggestedTask, { shouldDirty: true });

      const matchedFrequencyType = frequencyTypes.find(
        frequencyType => frequencyType.name.toLowerCase() === suggestion.frequencyType?.toLowerCase(),
      );
      const matchedDueDateType = dueDateTypes.find(
        dueDateType => dueDateType.name.toLowerCase() === suggestion.dueDateType?.toLowerCase(),
      );

      if (matchedFrequencyType) {
        setValue('frequencyTypeId', matchedFrequencyType.id, { shouldDirty: true });
      }

      if (matchedDueDateType) {
        setValue('dueDateTypeId', matchedDueDateType.id, { shouldDirty: true });
      }
    },
  });

  const fieldClass =
    'w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500';
  const labelClass = 'block text-sm font-medium text-gray-700 mb-1';
  const regulationText = toPlainText(sectionContent);
  const aiError = suggestMutation.isError ? handleApiError(suggestMutation.error) : null;

  const handleAskAi = () => {
    if (!aiConnection) {
      return;
    }

    suggestMutation.mutate();
  };

  const renderContentPanel = () => (
    <div>
      <p className={labelClass}>Regulation Content</p>
      <div className="max-h-40 overflow-y-auto rounded-lg border border-gray-200 bg-gray-50 px-3 py-3 text-sm text-gray-700 whitespace-pre-wrap">
        {loadingSectionContent
          ? 'Loading regulation content...'
          : regulationText || 'No regulation content available for this section.'}
      </div>
    </div>
  );

  return (
    <Modal
      isOpen
      onClose={onClose}
      title={mode === 'view' ? 'View Section Detail' : 'Edit Section Detail'}
      subtitle={sectionLabel}
      size="lg"
    >
      {loadingDetail ? (
        <div className="flex justify-center py-8">
          <div className="h-8 w-8 animate-spin rounded-full border-b-2 border-blue-600" />
        </div>
      ) : mode === 'view' ? (
        <div className="space-y-4">
          {renderContentPanel()}
          <div>
            <p className={labelClass}>Description</p>
            <p className="min-h-[60px] rounded-lg bg-gray-50 px-3 py-2 text-sm text-gray-800">
              {detail?.description || <span className="italic text-gray-400">Not set</span>}
            </p>
          </div>
          <div>
            <p className={labelClass}>Condition</p>
            <p className="min-h-[60px] rounded-lg bg-gray-50 px-3 py-2 text-sm text-gray-800">
              {detail?.condition || <span className="italic text-gray-400">Not set</span>}
            </p>
          </div>
          <div>
            <p className={labelClass}>Suggested Task</p>
            <p className="min-h-[60px] rounded-lg bg-gray-50 px-3 py-2 text-sm text-gray-800">
              {detail?.suggestedTask || <span className="italic text-gray-400">Not set</span>}
            </p>
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <p className={labelClass}>Frequency Type</p>
              <p className="rounded-lg bg-gray-50 px-3 py-2 text-sm text-gray-800">
                {detail?.frequencyTypeName || <span className="italic text-gray-400">Not set</span>}
              </p>
            </div>
            <div>
              <p className={labelClass}>Due Date Type</p>
              <p className="rounded-lg bg-gray-50 px-3 py-2 text-sm text-gray-800">
                {detail?.dueDateTypeName || <span className="italic text-gray-400">Not set</span>}
              </p>
            </div>
          </div>
        </div>
      ) : (
        <form onSubmit={handleSubmit(data => mutation.mutate(data))} className="space-y-4">
          <div className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-blue-100 bg-blue-50 px-4 py-3">
            <div>
              <p className="text-sm font-medium text-blue-900">AI-assisted drafting</p>
              <p className="text-xs text-blue-700">
                {aiConnection
                  ? 'Use AI to generate draft values for this regulation section.'
                  : (
                    <>
                      No AI provider connected.{' '}
                      <Link to="/ai-provider-settings" className="font-medium underline hover:text-blue-900">
                        Configure AI Provider
                      </Link>
                    </>
                  )}
              </p>
            </div>
            <div className="flex flex-shrink-0 gap-2">
              <button
                type="button"
                onClick={handleAskAi}
                disabled={!aiConnection || suggestMutation.isPending}
                className="inline-flex items-center gap-2 rounded-lg bg-blue-600 px-3 py-2 text-sm text-white hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-50"
              >
                <Bot size={16} />
                {suggestMutation.isPending ? 'Generating...' : 'AI Suggestion'}
              </button>
            </div>
          </div>

          <p className="text-xs italic text-gray-500">
            Note: The Description, Condition, Suggested Task, Frequency Type and Due Date Type are generated by AI and
            sometimes AI could make mistakes, Please check the content once.
          </p>

          {aiError && (
            <p className="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-600">{aiError}</p>
          )}

          {renderContentPanel()}

          <div>
            <label className={labelClass}>Description</label>
            <textarea {...register('description')} rows={3} className={fieldClass} placeholder="Enter description..." />
          </div>
          <div>
            <label className={labelClass}>Condition</label>
            <textarea {...register('condition')} rows={3} className={fieldClass} placeholder="Enter condition..." />
          </div>
          <div>
            <label className={labelClass}>Suggested Task</label>
            <textarea {...register('suggestedTask')} rows={3} className={fieldClass} placeholder="Enter suggested task..." />
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className={labelClass}>Frequency Type</label>
              <select {...register('frequencyTypeId')} className={fieldClass}>
                <option value="">-- None --</option>
                {frequencyTypes.map(frequencyType => (
                  <option key={frequencyType.id} value={frequencyType.id}>
                    {frequencyType.name}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className={labelClass}>Due Date Type</label>
              <select {...register('dueDateTypeId')} className={fieldClass}>
                <option value="">-- None --</option>
                {dueDateTypes.map(dueDateType => (
                  <option key={dueDateType.id} value={dueDateType.id}>
                    {dueDateType.name}
                  </option>
                ))}
              </select>
            </div>
          </div>

          {mutation.isError && (
            <p className="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-600">
              Failed to save. Please try again.
            </p>
          )}

          <div className="flex justify-end gap-3 pt-2">
            <button type="button" onClick={onClose} className="rounded-lg border border-gray-300 px-4 py-2 text-sm hover:bg-gray-50">
              Cancel
            </button>
            <button
              type="submit"
              disabled={isSubmitting || mutation.isPending}
              className="rounded-lg bg-blue-600 px-4 py-2 text-sm text-white hover:bg-blue-700 disabled:opacity-50"
            >
              {mutation.isPending ? 'Saving...' : 'Save'}
            </button>
          </div>
        </form>
      )}
    </Modal>
  );
}
