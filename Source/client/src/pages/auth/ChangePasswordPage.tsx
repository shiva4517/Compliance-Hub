import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { userService } from '../../services/userService';
import { AlertTriangle } from 'lucide-react';
import logo from "../../assets/ComplianceHub-Logo.png";


export default function ChangePasswordPage() {
  const { clearForcePasswordChange } = useAuth();
  const navigate = useNavigate();
  const [form, setForm] = useState({ currentPassword: '', newPassword: '', confirmPassword: '' });
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setForm(prev => ({ ...prev, [e.target.name]: e.target.value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    if (form.newPassword !== form.confirmPassword) {
      setError('New passwords do not match.');
      return;
    }
    if (form.newPassword.length < 8) {
      setError('New password must be at least 8 characters.');
      return;
    }
    setIsLoading(true);
    try {
      await userService.changePassword(form.currentPassword, form.newPassword);
      clearForcePasswordChange();
      navigate('/dashboard', { replace: true });
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
      setError(msg ?? 'Failed to change password. Please try again.');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gray-100 flex flex-col items-center justify-center">
      <div className="mb-8 flex flex-col items-center gap-2">
        <div className="w-14 h-14 bg-blue-800 rounded-2xl flex items-center justify-center shadow-lg">
          {/* <Shield size={28} className="text-white" /> */}
          <img
    src={logo}
    alt="Compliance Hub Logo"
    className="w-full h-full object-contain"
  />
        </div>
        <h1 className="text-2xl font-bold text-gray-900">Compliance Hub</h1>
      </div>

      <div className="bg-white rounded-2xl shadow-md p-8 w-full max-w-sm">
        <div className="flex items-start gap-2 bg-amber-50 border border-amber-200 rounded-lg px-3 py-2 mb-5 text-sm text-amber-800">
          <AlertTriangle size={16} className="mt-0.5 shrink-0" />
          <span>You must change your password before continuing.</span>
        </div>

        <h2 className="text-xl font-semibold text-gray-900 text-center mb-6">Change Password</h2>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="form-label">Current Password</label>
            <input
              type="password"
              name="currentPassword"
              value={form.currentPassword}
              onChange={handleChange}
              className="form-input"
              placeholder="••••••••"
              required
            />
          </div>
          <div>
            <label className="form-label">New Password</label>
            <input
              type="password"
              name="newPassword"
              value={form.newPassword}
              onChange={handleChange}
              className="form-input"
              placeholder="••••••••"
              required
            />
          </div>
          <div>
            <label className="form-label">Confirm New Password</label>
            <input
              type="password"
              name="confirmPassword"
              value={form.confirmPassword}
              onChange={handleChange}
              className="form-input"
              placeholder="••••••••"
              required
            />
          </div>

          {error && (
            <div className="bg-red-50 text-red-700 text-sm px-3 py-2 rounded-lg border border-red-200">
              {error}
            </div>
          )}

          <button type="submit" disabled={isLoading} className="btn-primary w-full py-2.5 mt-2 text-base">
            {isLoading ? 'Changing...' : 'Change Password'}
          </button>
        </form>
      </div>
    </div>
  );
}
