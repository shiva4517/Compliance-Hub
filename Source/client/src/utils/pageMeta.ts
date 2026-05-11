import { matchPath } from 'react-router-dom';

const BRAND_NAME = 'Compliance Hub';

const routeTitles = [
  { path: '/login', title: 'Login' },
  { path: '/change-password', title: 'Change Password' },
  { path: '/dashboard', title: 'Dashboard' },
  { path: '/companies', title: 'Companies' },
  { path: '/groups', title: 'Groups' },
  { path: '/users', title: 'Users' },
  { path: '/sync-regulations', title: 'Sync Regulations' },
  { path: '/scheduler', title: 'Scheduler History' },
  { path: '/customers', title: 'Customers' },
  { path: '/customers/:id', title: 'Customer Details' },
  { path: '/employees', title: 'Employees' },
  { path: '/employees/:id', title: 'Employee Details' },
  { path: '/departments', title: 'Departments' },
  { path: '/company-divisions', title: 'Company Divisions' },
  { path: '/districts', title: 'Company Districts' },
  { path: '/notifications', title: 'Notifications' },
  { path: '/notifications/:id', title: 'Notification Details' },
  { path: '/reports', title: 'Reports' },
  { path: '/customer-details', title: 'Dashboard' },
  { path: '/employee-dashboard', title: 'Dashboard' },
  { path: '/regulatory-assignments', title: 'Regulatory Assignments' },
  { path: '/regulatory-assignments/:id', title: 'Regulatory Assignment Details' },
  { path: '/regulations', title: 'Regulations' },
  { path: '/subscriptions', title: 'Subscriptions' },
  { path: '/change-notices', title: 'Change Notices' },
  { path: '/change-notices/:id', title: 'Change Notice Details' },
  { path: '/grievances', title: 'Grievances' },
  { path: '/agent', title: 'AI Agent' },
  { path: '/admin-agent', title: 'AI Agent' },
];

export function getPageTitle(pathname: string) {
  const matchedRoute = routeTitles.find((route) =>
    matchPath({ path: route.path, end: true }, pathname),
  );

  return matchedRoute ? `${matchedRoute.title} - ${BRAND_NAME}` : BRAND_NAME;
}

export { BRAND_NAME };
