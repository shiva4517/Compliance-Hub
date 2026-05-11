import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { ChevronDown, ChevronUp, Eye, Search } from 'lucide-react';
import { useAuth } from '../../contexts/AuthContext';
import { changeNoticeService } from '../../services/changeNoticeService';

function formatDateTime(value: string) {
  return new Date(value).toLocaleString('en-US', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    timeZone: 'UTC',
    timeZoneName: 'short',
  });
}

function SortIcon({ col, sortBy, sortDir }: { col: string; sortBy: string; sortDir: string }) {
  if (col !== sortBy) return null;
  return sortDir === 'asc'
    ? <ChevronUp size={13} className="inline ml-0.5" />
    : <ChevronDown size={13} className="inline ml-0.5" />;
}

export default function ChangeNoticesPage() {
  const navigate = useNavigate();
  const { user } = useAuth();

  const role = user?.role ?? 'Admin';
  // Employee uses their SecurityUser.userId which points to Employee.Id
  // Customer uses their SecurityUser.userId which points to Customer.Id
  const referenceId =
    role === 'Employee' || role === 'Customer' ? user?.userId : undefined;

  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  const [sortBy, setSortBy] = useState('ChangedAt');
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>('desc');
  const [page, setPage] = useState(1);

  const { data, isLoading } = useQuery({
    queryKey: ['change-notices', role, referenceId, search, fromDate, toDate, page],
    queryFn: () =>
      changeNoticeService.getAll({
        role,
        referenceId,
        search: search || undefined,
        fromDate: fromDate || undefined,
        toDate: toDate || undefined,
        pageNumber: page,
        pageSize: 20,
      }),
    enabled: !!user,
  });

  const items = data?.items ?? [];

  // Client-side sort for Admin (server returns all sorted by ChangedAt desc; allow re-sort)
  const sorted = role === 'Admin'
    ? [...items].sort((a, b) => {
        let diff = 0;
        if (sortBy === 'ChangedAt') diff = new Date(a.changedAt).getTime() - new Date(b.changedAt).getTime();
        else if (sortBy === 'SectionNumber') diff = a.sectionNumber.localeCompare(b.sectionNumber);
        return sortDir === 'asc' ? diff : -diff;
      })
    : items;

  const handleSearch = () => {
    setSearch(searchInput);
    setPage(1);
  };

  const handleSort = (col: string) => {
    if (col === sortBy) setSortDir(d => d === 'asc' ? 'desc' : 'asc');
    else { setSortBy(col); setSortDir('desc'); }
    setPage(1);
  };

  const clearFilters = () => {
    setSearchInput(''); setSearch(''); setFromDate(''); setToDate(''); setPage(1);
  };

  const hasFilters = !!(search || fromDate || toDate);

  const subtitle =
    role === 'Customer'
      ? 'Change notices for your subscribed regulations.'
      : role === 'Employee'
        ? 'Change notices for your assigned regulatory subscriptions.'
        : 'All regulation change notices across the system.';

  const columns = role === 'Admin'
    ? ['Gov. Entity', 'Agency', 'Category', 'Type', 'Subtype', 'Section No.', 'Section Title', 'Prev Ver.', 'Curr Ver.', 'Changed At', 'Actions']
    : ['Gov. Entity', 'Agency', 'Section No.', 'Section Title', 'Prev Ver.', 'Curr Ver.', 'Changed At', 'Actions'];

  const sortableMap: Record<string, string> = {
    'Section No.': 'SectionNumber',
    'Changed At': 'ChangedAt',
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between flex-wrap gap-3">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Change Notices</h1>
          <p className="text-sm text-gray-500 mt-0.5">{subtitle}</p>
        </div>
      </div>

      {/* Filters */}
      <div className="card p-4 flex flex-wrap gap-4 items-end">
        <div className="flex-1 min-w-56">
          <label className="block text-xs text-gray-500 mb-1">Search</label>
          <div className="relative">
            <Search size={15} className="absolute left-2.5 top-1/2 -translate-y-1/2 text-gray-400" />
            <input
              type="text"
              placeholder="Gov. entity, agency, section..."
              value={searchInput}
              onChange={e => setSearchInput(e.target.value)}
              onKeyDown={e => e.key === 'Enter' && handleSearch()}
              className="pl-8 pr-3 py-1.5 text-sm border border-gray-300 rounded-lg w-full focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
        </div>
        <div>
          <label className="block text-xs text-gray-500 mb-1">From Date</label>
          <input
            type="date"
            value={fromDate}
            onChange={e => { setFromDate(e.target.value); setPage(1); }}
            className="form-input py-1.5 text-sm"
          />
        </div>
        <div>
          <label className="block text-xs text-gray-500 mb-1">To Date</label>
          <input
            type="date"
            value={toDate}
            onChange={e => { setToDate(e.target.value); setPage(1); }}
            className="form-input py-1.5 text-sm"
          />
        </div>
        <div className="flex gap-2">
          <button onClick={handleSearch} className="btn-primary py-1.5 px-3 text-sm">Search</button>
          {hasFilters && (
            <button onClick={clearFilters} className="text-sm text-blue-600 hover:underline px-2">Clear</button>
          )}
        </div>
      </div>

      {/* Table */}
      <div className="card overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="bg-gray-50 border-b border-gray-200">
                {columns.map(label => {
                  const col = sortableMap[label];
                  return (
                    <th
                      key={label}
                      onClick={col ? () => handleSort(col) : undefined}
                      className={`text-left px-3 py-3 font-medium text-gray-600 whitespace-nowrap text-xs uppercase tracking-wide ${col ? 'cursor-pointer hover:text-gray-900 select-none' : ''}`}
                    >
                      {label}
                      {col && <SortIcon col={col} sortBy={sortBy} sortDir={sortDir} />}
                    </th>
                  );
                })}
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {isLoading ? (
                <tr>
                  <td colSpan={columns.length} className="text-center text-gray-400 py-10">Loading...</td>
                </tr>
              ) : sorted.length === 0 ? (
                <tr>
                  <td colSpan={columns.length} className="text-center text-gray-400 py-10">No change notices found.</td>
                </tr>
              ) : sorted.map(notice => (
                <tr key={notice.id} className="hover:bg-gray-50 transition-colors">
                  <td className="px-3 py-3 text-gray-800 max-w-[140px] truncate" title={notice.governmentEntityName}>
                    {notice.governmentEntityName}
                  </td>
                  <td className="px-3 py-3 text-gray-600 max-w-[120px] truncate" title={notice.agencyName}>
                    {notice.agencyName}
                  </td>
                  {role === 'Admin' && (
                    <>
                      <td className="px-3 py-3 text-gray-600 max-w-[120px] truncate" title={notice.regulationCategoryName}>
                        {notice.regulationCategoryName}
                      </td>
                      <td className="px-3 py-3 text-gray-600 max-w-[120px] truncate" title={notice.regulationTypeName}>
                        {notice.regulationTypeName}
                      </td>
                      <td className="px-3 py-3 text-gray-500 max-w-[100px] truncate" title={notice.regulationSubtypeName ?? ''}>
                        {notice.regulationSubtypeName ?? '—'}
                      </td>
                    </>
                  )}
                  <td className="px-3 py-3 font-mono text-gray-800 whitespace-nowrap">{notice.sectionNumber}</td>
                  <td className="px-3 py-3 text-gray-700 max-w-[180px] truncate" title={notice.sectionTitle}>
                    {notice.sectionTitle}
                  </td>
                  <td className="px-3 py-3 text-center text-gray-500">{notice.archivedVersion}</td>
                  <td className="px-3 py-3 text-center font-semibold text-blue-700">{notice.newVersion}</td>
                  <td className="px-3 py-3 text-gray-500 text-xs whitespace-nowrap">{formatDateTime(notice.changedAt)}</td>
                  <td className="px-3 py-3">
                    <button
                      onClick={() => navigate(`/change-notices/${notice.id}`)}
                      className="text-blue-600 hover:text-blue-800 p-1 rounded"
                      title="View details"
                    >
                      <Eye size={16} />
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Pagination */}
      {data && data.totalPages > 1 && (
        <div className="flex items-center justify-between text-sm text-gray-600">
          <span>Page {page} of {data.totalPages} ({data.totalCount} total)</span>
          <div className="flex gap-2">
            <button
              onClick={() => setPage(p => Math.max(1, p - 1))}
              disabled={!data.hasPreviousPage}
              className="btn-primary py-1 px-3 disabled:opacity-50"
            >
              Prev
            </button>
            <button
              onClick={() => setPage(p => p + 1)}
              disabled={!data.hasNextPage}
              className="btn-primary py-1 px-3 disabled:opacity-50"
            >
              Next
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
