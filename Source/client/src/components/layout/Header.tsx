import { Bell, MessageSquare, User, LogOut } from 'lucide-react';
import { useAuth } from '../../contexts/AuthContext';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { notificationHistoryService } from '../../services/notificationHistoryService';

export default function Header() {
  const { user, logout } = useAuth();
  const authUser = user;
  const navigate = useNavigate();

  const isAdmin = authUser?.role === 'Admin';

  const { data: unseenCount } = useQuery({
    queryKey: ['notifications-unseen-count', authUser?.userId],
    queryFn: () => notificationHistoryService.getUnseenCount(authUser!.userId),
    enabled: !!authUser?.userId && isAdmin,
    refetchInterval: 60_000,
  });

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <header className="h-14 bg-white border-b border-gray-200 flex items-center justify-between px-6">
      <div />
      <div className="flex items-center gap-3">
        <button
          className="p-2 text-gray-400 hover:text-gray-600 rounded-lg hover:bg-gray-100"
          onClick={() => navigate('/grievances')}
        >
          <MessageSquare size={18} />
        </button>
        {isAdmin && (
          <button
            className="relative p-2 text-gray-400 hover:text-gray-600 rounded-lg hover:bg-gray-100"
            onClick={() => navigate('/notifications')}
          >
            <Bell size={18} />
            {!!unseenCount && unseenCount > 0 && (
              <span className="absolute top-1 right-1 w-4 h-4 bg-red-500 text-white text-[10px] font-bold rounded-full flex items-center justify-center leading-none">
                {unseenCount > 99 ? '99+' : unseenCount}
              </span>
            )}
          </button>
        )}
        <div className="flex items-center gap-2 ml-2">
          <div className="w-8 h-8 bg-gray-200 rounded-full flex items-center justify-center">
            <User size={16} className="text-gray-600" />
          </div>
          <div className="text-sm">
            <div className="font-medium text-gray-900">{user?.fullName}</div>
          </div>
        </div>
        <button
          onClick={handleLogout}
          className="flex items-center gap-1.5 text-sm text-gray-600 hover:text-gray-900 p-2 rounded-lg hover:bg-gray-100"
        >
          <LogOut size={16} />
          Sign out
        </button>
      </div>
    </header>
  );
}
