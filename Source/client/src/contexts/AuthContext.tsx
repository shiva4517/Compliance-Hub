import { createContext, useContext, useEffect, useState, type ReactNode } from 'react';
import type { AuthUser } from '../types';
import { authService } from '../services/authService';

interface AuthContextType {
  user: AuthUser | null;
  login: (email: string, password: string) => Promise<AuthUser>;
  logout: () => void;
  clearForcePasswordChange: () => void;
  isLoading: boolean;
}

const AuthContext = createContext<AuthContextType | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const stored = localStorage.getItem('authUser');
    if (stored) {
      try { setUser(JSON.parse(stored)); } catch { localStorage.clear(); }
    }
    setIsLoading(false);
  }, []);

  const login = async (email: string, password: string) => {
    const response = await authService.login({ email, password });
    const authUser: AuthUser = {
      userId: response.userId,
      email: response.email,
      fullName: response.fullName,
      role: response.role as AuthUser['role'],
      accessToken: response.accessToken,
      refreshToken: response.refreshToken,
      isForcePasswordChange: response.isForcePasswordChange,
    };
    localStorage.setItem('accessToken', response.accessToken);
    localStorage.setItem('refreshToken', response.refreshToken);
    localStorage.setItem('authUser', JSON.stringify(authUser));
    setUser(authUser);
    return authUser;
  };

  const logout = () => {
    localStorage.clear();
    setUser(null);
  };

  const clearForcePasswordChange = () => {
    if (!user) return;
    const updated = { ...user, isForcePasswordChange: false };
    localStorage.setItem('authUser', JSON.stringify(updated));
    setUser(updated);
  };

  return (
    <AuthContext.Provider value={{ user, login, logout, clearForcePasswordChange, isLoading }}>
      {children}
    </AuthContext.Provider>
  );
}

export const useAuth = () => {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
};
