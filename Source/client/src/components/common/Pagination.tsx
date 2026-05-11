interface PaginationProps {
  pageNumber: number;
  totalPages: number;
  onPageChange: (page: number) => void;
}

export default function Pagination({ pageNumber, totalPages, onPageChange }: PaginationProps) {
  if (totalPages <= 1) return null;

  return (
    <div className="flex items-center justify-center gap-2 mt-4">
      <button
        onClick={() => onPageChange(pageNumber - 1)}
        disabled={pageNumber <= 1}
        className="px-3 py-1.5 text-sm border border-gray-300 rounded-lg disabled:opacity-40 hover:bg-gray-50"
      >
        Previous
      </button>
      <span className="text-sm text-gray-600">Page {pageNumber} of {totalPages}</span>
      <button
        onClick={() => onPageChange(pageNumber + 1)}
        disabled={pageNumber >= totalPages}
        className="px-3 py-1.5 text-sm border border-gray-300 rounded-lg disabled:opacity-40 hover:bg-gray-50"
      >
        Next
      </button>
    </div>
  );
}
