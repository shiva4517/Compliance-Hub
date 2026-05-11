import { useState } from 'react';
import { NavLink } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import logo from "../../assets/ComplianceHub-Logo.png";
import {
  LayoutDashboard, Users, BookOpen, RefreshCw, Bell, FileText,
  UsersRound, Building2, Network, DatabaseZap, Briefcase,
  MessageSquare, CalendarClock, Globe, UserCircle, ChevronDown, Layers, Bot, Cpu
} from 'lucide-react';

interface NavItem {
  label: string;
  to: string;
  icon: React.ReactNode;
}

interface NavSection {
  title: string;
  items: NavItem[];
  roles: string[];
}

const navSections: NavSection[] = [
  // ── SuperAdmin ──────────────────────────────────────────────────────────────
  {
    title: 'Overview',
    roles: ['SuperAdmin'],
    items: [
      { label: 'Dashboard', to: '/dashboard', icon: <LayoutDashboard size={16} /> },
    ]
  },
  {
    title: 'Platform Management',
    roles: ['SuperAdmin'],
    items: [
      { label: 'Companies', to: '/companies', icon: <Building2 size={16} /> },
      { label: 'Groups', to: '/groups', icon: <UsersRound size={16} /> },
      { label: 'Users', to: '/users', icon: <Users size={16} /> },
    ]
  },
  {
    title: 'Data & Sync',
    roles: ['SuperAdmin'],
    items: [
      { label: 'Regulations', to: '/regulations', icon: <Globe size={16} /> },
      { label: 'Sync Regulations', to: '/sync-regulations', icon: <DatabaseZap size={16} /> },
      { label: 'Scheduler History', to: '/scheduler', icon: <CalendarClock size={16} /> },
    ]
  },
  {
    title: 'Monitoring',
    roles: ['SuperAdmin'],
    items: [
      { label: 'Grievances', to: '/grievances', icon: <MessageSquare size={16} /> },
    ]
  },
  {
    title: 'AI',
    roles: ['SuperAdmin'],
    items: [
      { label: 'AI',       to: '/ai-provider-settings', icon: <Cpu size={16} /> },
      { label: 'AI Agent', to: '/agent',                 icon: <Bot size={16} /> },
    ]
  },

  // ── Admin ────────────────────────────────────────────────────────────────────
  {
    title: 'Overview',
    roles: ['Admin'],
    items: [
      { label: 'Dashboard', to: '/dashboard', icon: <LayoutDashboard size={16} /> },
    ]
  },
  {
    title: 'Management',
    roles: ['Admin'],
    items: [
      { label: 'Customers', to: '/customers', icon: <FileText size={16} /> },
      { label: 'Employees', to: '/employees', icon: <Briefcase size={16} /> },
      { label: 'Departments', to: '/departments', icon: <Layers size={16} /> },
      { label: 'Company Divisions', to: '/company-divisions', icon: <Network size={16} /> },
      { label: 'Company Districts', to: '/districts', icon: <Network size={16} /> },
    ]
  },
  {
    title: 'Regulations',
    roles: ['Admin'],
    items: [
      { label: 'Regulations', to: '/regulations', icon: <BookOpen size={16} /> },
      { label: 'Subscriptions', to: '/subscriptions', icon: <RefreshCw size={16} /> },
      { label: 'Change Notices', to: '/change-notices', icon: <Bell size={16} /> },
    ]
  },
  {
    title: 'Operations',
    roles: ['Admin'],
    items: [
      { label: 'Notifications', to: '/notifications', icon: <Bell size={16} /> },
      { label: 'Grievances', to: '/grievances', icon: <MessageSquare size={16} /> },
    ]
  },
  {
    title: 'AI',
    roles: ['Admin'],
    items: [
      { label: 'AI',       to: '/ai-provider-settings', icon: <Cpu size={16} /> },
      { label: 'AI Agent', to: '/admin-agent',           icon: <Bot size={16} /> },
    ]
  },

  // ── Customer ─────────────────────────────────────────────────────────────────
  {
    title: 'Dashboard',
    roles: ['Customer'],
    items: [
      { label: 'Dashboard', to: '/customer-details', icon: <UserCircle size={16} /> },
    ]
  },
  {
    title: 'Regulations',
    roles: ['Customer'],
    items: [
      { label: 'Regulations', to: '/regulations', icon: <BookOpen size={16} /> },
      { label: 'Change Notices', to: '/change-notices', icon: <Bell size={16} /> },
      { label: 'Subscriptions', to: '/subscriptions', icon: <RefreshCw size={16} /> },
    ]
  },
  {
    title: 'Support',
    roles: ['Customer'],
    items: [
      { label: 'Grievances', to: '/grievances', icon: <MessageSquare size={16} /> },
    ]
  },

  // ── Employee ──────────────────────────────────────────────────────────────────
  {
    title: 'Dashboard',
    roles: ['Employee'],
    items: [
      { label: 'Dashboard', to: '/employee-dashboard', icon: <UserCircle size={16} /> },
    ]
  },
  {
    title: 'Work',
    roles: ['Employee'],
    items: [
      { label: 'Regulatory Assignments', to: '/regulatory-assignments', icon: <Briefcase size={16} /> },
      { label: 'Change Notices', to: '/change-notices', icon: <Bell size={16} /> },
      { label: 'Regulations', to: '/regulations', icon: <BookOpen size={16} /> },
      { label: 'Grievances', to: '/grievances', icon: <MessageSquare size={16} /> },
    ]
  },
];

export default function Sidebar() {
  const { user } = useAuth();

  const visibleSections = navSections.filter(
    section => user && section.roles.includes(user.role)
  );

  const [collapsed, setCollapsed] = useState<Record<string, boolean>>({});

  const toggleSection = (key: string) => {
    setCollapsed(prev => ({ ...prev, [key]: !prev[key] }));
  };

  return (
    <aside className="w-56 bg-white border-r border-gray-200 min-h-screen flex flex-col">
      <div className="p-4 border-b border-gray-200">
        <div className="flex items-center gap-2">
          <div className="w-8 h-8 rounded-lg flex items-center justify-center overflow-hidden">
            <img
              src={logo}
              alt="Compliance Hub Logo"
              className="w-6 h-6 object-contain"
            />
          </div>
          <span className="font-bold text-gray-900 text-lg">Compliance Hub</span>
        </div>
      </div>

      <nav className="flex-1 overflow-y-auto py-2">
        {visibleSections.map((section) => {
          const key = `${section.title}-${section.roles.join()}`;
          const isCollapsed = collapsed[key] ?? false;

          return (
            <div key={key} className="mb-2">
              <button
                onClick={() => toggleSection(key)}
                className="w-full px-4 py-1.5 text-xs font-semibold text-gray-400 uppercase tracking-wider flex justify-between items-center hover:text-gray-600 transition-colors"
              >
                {section.title}
                <ChevronDown
                  size={12}
                  className={`transition-transform duration-200 ${isCollapsed ? '-rotate-90' : ''}`}
                />
              </button>
              {!isCollapsed && section.items.map((item) => (
                <NavLink
                  key={item.to}
                  to={item.to}
                  className={({ isActive }) =>
                    `flex items-center gap-2.5 px-4 py-2 text-sm transition-colors ${
                      isActive
                        ? 'bg-blue-50 text-blue-800 font-medium border-r-2 border-blue-800'
                        : 'text-gray-600 hover:bg-gray-50 hover:text-gray-900'
                    }`
                  }
                >
                  {item.icon}
                  {item.label}
                </NavLink>
              ))}
            </div>
          );
        })}
      </nav>

      <div className="p-4 border-t border-gray-200">
        <div className="text-xs text-gray-400 text-center">
          © 2026 Compliance Hub
        </div>
      </div>
    </aside>
  );
}
