import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import logo from "../../assets/ComplianceHub-Logo.png";


export default function LoginPage() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const { login } = useAuth();
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setIsLoading(true);
    try {
      const authUser = await login(email, password);
      navigate(authUser.isForcePasswordChange ? '/change-password' : '/dashboard', { replace: true });
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
      setError(msg ?? 'Invalid email or password.');
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
        {/* <p className="text-sm text-gray-500">Compliance Hub</p> */}
      </div>

      <div className="bg-white rounded-2xl shadow-md p-8 w-full max-w-sm">
        <h2 className="text-xl font-semibold text-gray-900 text-center mb-6">Sign in to your account</h2>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="form-label">Email address</label>
            <input
              type="email"
              value={email}
              onChange={e => setEmail(e.target.value)}
              className="form-input"
              placeholder="admin@compliancehub.com"
              required
            />
          </div>
          <div>
            <label className="form-label">Password</label>
            <input
              type="password"
              value={password}
              onChange={e => setPassword(e.target.value)}
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
            {isLoading ? 'Signing in...' : 'Sign in'}
          </button>
        </form>

        {/* <p className="text-xs text-gray-400 text-center mt-4">
          Default: admin@compliancehub.com / Admin@123
        </p> */}
      </div>
    </div>
  );
}
