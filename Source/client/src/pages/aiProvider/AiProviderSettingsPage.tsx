import { useEffect, useState } from 'react';
import { useForm, useWatch } from 'react-hook-form';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Bot, KeyRound, PlugZap, X } from 'lucide-react';
import { regulationService } from '../../services/regulationService';
import { handleApiError } from '../../services/api';
import type { AiProvider, AiProviderModelOption, UpsertAiProviderConnectionRequest } from '../../types';

interface FormData {
  provider: AiProvider;
  apiKey: string;
  endpoint: string;
  model: string;
}

interface ToastState {
  type: 'success' | 'error';
  message: string;
}

const PROVIDER_OPTIONS: Array<{ value: AiProvider; label: string; helper: string }> = [
  { value: 'azure-openai', label: 'Azure OpenAI Service', helper: 'Use your Azure endpoint, API key, and deployed model.' },
  { value: 'gemini', label: 'Google Gemini API', helper: 'Use a Gemini API key and choose the model you want for drafts.' },
  { value: 'claude', label: 'Anthropic Claude API', helper: 'Use an Anthropic API key and select the Claude model to reuse.' },
  { value: 'openai', label: 'OpenAI', helper: 'Use your OpenAI API key and choose a model from your account.' },
  { value: 'openrouter', label: 'OpenRouter', helper: 'Use your OpenRouter API key and choose a routed model dynamically.' },
];

export default function AiProviderSettingsPage() {
  const queryClient = useQueryClient();
  const [toast, setToast] = useState<ToastState | null>(null);
  const [debouncedProvider, setDebouncedProvider] = useState<AiProvider>('azure-openai');
  const [debouncedApiKey, setDebouncedApiKey] = useState('');
  const [debouncedEndpoint, setDebouncedEndpoint] = useState('');

  const { data: aiConnection, isLoading } = useQuery({
    queryKey: ['regulation-ai-connection'],
    queryFn: () => regulationService.getAiConnection(),
  });

  const {
    control,
    register,
    handleSubmit,
    reset,
    getValues,
    formState: { isSubmitting },
  } = useForm<FormData>({
    defaultValues: {
      provider: 'azure-openai',
      apiKey: '',
      endpoint: '',
      model: '',
    },
  });

  const selectedProvider = useWatch({ control, name: 'provider' }) ?? 'azure-openai';
  const enteredApiKey = useWatch({ control, name: 'apiKey' }) ?? '';
  const enteredEndpoint = useWatch({ control, name: 'endpoint' }) ?? '';

  useEffect(() => {
    reset({
      provider: aiConnection?.provider ?? 'azure-openai',
      apiKey: '',
      endpoint: aiConnection?.endpoint ?? '',
      model: aiConnection?.model ?? aiConnection?.deploymentName ?? '',
    });
  }, [aiConnection, reset]);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      setDebouncedProvider(selectedProvider);
      setDebouncedApiKey(enteredApiKey.trim());
      setDebouncedEndpoint(enteredEndpoint.trim());
    }, 500);

    return () => window.clearTimeout(timeoutId);
  }, [selectedProvider, enteredApiKey, enteredEndpoint]);

  useEffect(() => {
    if (!toast) {
      return;
    }

    const timeoutId = window.setTimeout(() => {
      setToast(null);
    }, 5000);

    return () => window.clearTimeout(timeoutId);
  }, [toast]);

  const shouldLoadModels = Boolean(
    debouncedApiKey &&
    (debouncedProvider !== 'azure-openai' || debouncedEndpoint),
  );

  const {
    data: liveModels = [],
    isFetching: loadingModels,
    error: modelsError,
  } = useQuery({
    queryKey: ['ai-provider-models', debouncedProvider, debouncedApiKey, debouncedEndpoint],
    queryFn: () =>
      regulationService.getAiModels({
        provider: debouncedProvider,
        apiKey: debouncedApiKey,
        endpoint: debouncedProvider === 'azure-openai' ? debouncedEndpoint : undefined,
      }),
    enabled: shouldLoadModels,
    retry: false,
  });

  const currentModel = aiConnection?.provider === selectedProvider
    ? (aiConnection.model ?? aiConnection.deploymentName ?? '').trim()
    : '';

  const availableModels: AiProviderModelOption[] = [...liveModels];

  if (currentModel && !availableModels.some(option => option.value === currentModel)) {
    availableModels.unshift({ value: currentModel, label: `${currentModel} (Current)` });
  }

  const upsertMutation = useMutation({
    mutationFn: (data: UpsertAiProviderConnectionRequest) => regulationService.upsertAiConnection(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['regulation-ai-connection'] });
      setToast({ type: 'success', message: 'AI configuration saved successfully.' });
      reset({
        provider: getValues('provider'),
        apiKey: '',
        endpoint: getValues('endpoint'),
        model: getValues('model'),
      });
    },
    onError: error => {
      setToast({ type: 'error', message: handleApiError(error) });
    },
  });

  const onSubmit = (data: FormData) => {
    const apiKey = data.apiKey.trim();
    const endpoint = data.endpoint.trim();
    const model = data.model.trim();

    if (!apiKey) {
      setToast({ type: 'error', message: 'Enter an API key to continue.' });
      return;
    }

    if (!model) {
      setToast({ type: 'error', message: 'Select a model for the chosen provider.' });
      return;
    }

    if (data.provider === 'azure-openai' && !endpoint) {
      setToast({ type: 'error', message: 'Azure OpenAI requires an endpoint.' });
      return;
    }

    upsertMutation.mutate({
      provider: data.provider,
      apiKey,
      endpoint: data.provider === 'azure-openai' ? endpoint : undefined,
      deploymentName: data.provider === 'azure-openai' ? model : undefined,
      model,
      apiVersion: undefined,
    });
  };

  const selectedProviderDetails = PROVIDER_OPTIONS.find(option => option.value === selectedProvider);
  const connectedProviderDetails = aiConnection
    ? PROVIDER_OPTIONS.find(option => option.value === aiConnection.provider)
    : null;

  return (
    <div className="max-w-4xl space-y-6">
      {toast && (
        <div
          className={`fixed right-6 top-6 z-[90] min-w-[320px] max-w-[420px] rounded-xl border px-4 py-3 shadow-lg ${
            toast.type === 'error'
              ? 'border-red-200 bg-white text-red-700'
              : 'border-green-200 bg-white text-green-700'
          }`}
        >
          <div className="flex items-start gap-3">
            <p className="flex-1 text-sm font-medium">{toast.message}</p>
            <button
              type="button"
              onClick={() => setToast(null)}
              className="mt-0.5 text-gray-400 hover:text-gray-600"
              aria-label="Close notification"
            >
              <X size={16} />
            </button>
          </div>
        </div>
      )}

      <div>
        <div className="flex items-center gap-2 mb-1">
          <Bot size={20} className="text-blue-800" />
          <h1 className="text-xl font-semibold text-gray-900">AI</h1>
        </div>
        {/* <p className="text-sm text-gray-500">
          Connect one provider for your admin account and reuse it across regulations now and other modules later.
        </p> */}
      </div>

      <div className="grid gap-6 lg:grid-cols-[1.2fr_0.8fr]">
        <div className="card p-6 space-y-5">
          <div className="flex items-start gap-3 rounded-xl border border-blue-100 bg-blue-50 px-4 py-4">
            <PlugZap size={18} className="mt-0.5 text-blue-700" />
            <div>
              <p className="text-sm font-medium text-blue-900">Reusable AI connection</p>
              <p className="text-sm text-blue-700">
                Save the provider once here. The Regulations modal will use this connection automatically.
              </p>
            </div>
          </div>

          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">AI Provider</label>
              <select
                {...register('provider')}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              >
                {PROVIDER_OPTIONS.map(option => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
              <p className="mt-1 text-xs text-gray-500">{selectedProviderDetails?.helper}</p>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">API Key</label>
              <div className="relative">
                <KeyRound size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
                <input
                  {...register('apiKey')}
                  type="password"
                  autoComplete="off"
                  placeholder="Enter provider API key"
                  className="w-full rounded-lg border border-gray-300 py-2 pl-10 pr-3 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>
              <p className="mt-1 text-xs text-gray-500">Your key is stored securely on the server for this admin account.</p>
            </div>

            {selectedProvider === 'azure-openai' && (
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Endpoint</label>
                <input
                  {...register('endpoint')}
                  type="text"
                  autoComplete="off"
                  placeholder="https://your-resource.openai.azure.com"
                  className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>
            )}

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                {selectedProvider === 'azure-openai' ? 'Model / Deployment' : 'Model'}
              </label>
              <select
                {...register('model')}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                disabled={!shouldLoadModels || loadingModels}
              >
                <option value="">
                  {loadingModels
                    ? 'Loading models...'
                    : shouldLoadModels
                      ? 'Select a model'
                      : selectedProvider === 'azure-openai'
                        ? 'Enter API key and endpoint to load models'
                        : 'Enter API key to load models'}
                </option>
                {availableModels.map(option => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
              <p className="mt-1 text-xs text-gray-500">
                {selectedProvider === 'azure-openai'
                  ? 'Models are loaded from your Azure OpenAI resource after you enter the endpoint and API key.'
                  : 'Models are loaded directly from the selected provider after you enter the API key.'}
              </p>
            </div>

            {modelsError && shouldLoadModels && (
              <p className="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-600">
                {handleApiError(modelsError)}
              </p>
            )}

            <div className="flex justify-end">
              <button
                type="submit"
                disabled={isSubmitting || upsertMutation.isPending}
                className="rounded-lg bg-blue-600 px-4 py-2 text-sm text-white hover:bg-blue-700 disabled:opacity-50"
              >
                {upsertMutation.isPending ? 'Saving...' : 'Save AI Config'}
              </button>
            </div>
          </form>
        </div>

        <div className="card p-6 space-y-4">
          <div>
            <p className="text-sm font-medium text-gray-900">Current status</p>
            <p className="mt-1 text-sm text-gray-500">
              {isLoading
                ? 'Checking your saved AI provider...'
                : aiConnection
                  ? `Connected to ${aiConnection.providerDisplayName}.`
                  : 'No AI provider has been connected yet.'}
            </p>
          </div>

          <div className="rounded-xl border border-gray-200 bg-gray-50 px-4 py-4 text-sm text-gray-700 space-y-2">
            <div className="flex justify-between gap-4">
              <span className="text-gray-500">Provider</span>
              <span className="font-medium text-right">{connectedProviderDetails?.label ?? 'Not connected'}</span>
            </div>
            <div className="flex justify-between gap-4">
              <span className="text-gray-500">Model</span>
              <span className="font-medium text-right">{aiConnection?.model ?? aiConnection?.deploymentName ?? 'Not set'}</span>
            </div>
            {aiConnection?.provider === 'azure-openai' && (
              <div className="flex justify-between gap-4">
                <span className="text-gray-500">Endpoint</span>
                <span className="font-medium text-right break-all">{aiConnection.endpoint ?? 'Not set'}</span>
              </div>
            )}
          </div>

          <div className="rounded-xl border border-amber-200 bg-amber-50 px-4 py-4 text-sm text-amber-800">
            Regulations and future AI-enabled modules will use the saved connection automatically.
          </div>
        </div>
      </div>
    </div>
  );
}
