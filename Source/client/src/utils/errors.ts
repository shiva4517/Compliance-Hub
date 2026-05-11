export function extractErrorMessage(err: unknown, fallback: string): string {
  const data = (err as { response?: { data?: { message?: string; error?: string; errors?: unknown } } })?.response?.data;
  if (data?.message) return data.message;
  if (data?.error) return data.error;
  if (Array.isArray(data?.errors) && data.errors.length) {
    const first = data.errors[0];
    if (typeof first === 'string') return first;
    if (first && typeof first === 'object' && 'message' in first) {
      return String((first as { message: unknown }).message);
    }
  }
  if (data?.errors && typeof data.errors === 'object') {
    const values = Object.values(data.errors as Record<string, unknown>);
    const firstArr = values.find(v => Array.isArray(v) && v.length) as string[] | undefined;
    if (firstArr) return String(firstArr[0]);
  }
  if (err instanceof Error && err.message) return err.message;
  return fallback;
}
