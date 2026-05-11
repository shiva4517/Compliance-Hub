import { Construction } from 'lucide-react';

export default function PlaceholderPage({ title }: { title: string }) {
  return (
    <div>
      <h1 className="text-2xl font-bold text-gray-900 mb-1">{title}</h1>
      <div className="mt-12 flex flex-col items-center justify-center text-gray-400 gap-3">
        <Construction size={48} />
        <p className="text-lg font-medium">Under Construction</p>
        <p className="text-sm">This module is coming soon.</p>
      </div>
    </div>
  );
}
