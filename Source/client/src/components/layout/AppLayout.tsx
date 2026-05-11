import { Outlet, useLocation } from 'react-router-dom';
import Sidebar from './Sidebar';
import Header from './Header';

const FULL_BLEED_ROUTES = ['/agent', '/admin-agent'];

export default function AppLayout() {
  const { pathname } = useLocation();
  const fullBleed = FULL_BLEED_ROUTES.some((r) => pathname === r || pathname.startsWith(r + '/'));

  return (
    <div className="flex h-screen bg-gray-50">
      <Sidebar />
      <div className="flex-1 flex flex-col overflow-hidden">
        <Header />
        {fullBleed ? (
          <div className="flex-1 overflow-hidden">
            <Outlet />
          </div>
        ) : (
          <main className="flex-1 overflow-auto p-6">
            <Outlet />
          </main>
        )}
      </div>
    </div>
  );
}
