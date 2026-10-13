import { User } from '../../types/User';

export const authHelper = {
  getToken(): string | null {
    return localStorage.getItem('access_token');
  },

  getCurrentUser(): User | null {
    const raw = localStorage.getItem('current_user');
    if (!raw) return null;
    try {
      return JSON.parse(raw);
    } catch {
      return null;
    }
  },

  setCurrentUser(user: User): void {
    localStorage.setItem('current_user', JSON.stringify(user));
  },

  setToken(token: string): void {
    localStorage.setItem('access_token', token);
  },

  clearAuth(): void {
    localStorage.removeItem('access_token');
    localStorage.removeItem('current_user');
  },

  isAuthenticated(): boolean {
    return !!this.getToken();
  },

  isAdmin(): boolean {
    const user = this.getCurrentUser();
    return user?.maVaiTro === 1 || user?.tenVaiTro === 'QuanTriVien';
  },

  isForensicOfficer(): boolean {
    const user = this.getCurrentUser();
    return user?.maVaiTro === 2 || user?.tenVaiTro === 'CanBoGiamDinh';
  },

  isCitizen(): boolean {
    const user = this.getCurrentUser();
    return user?.maVaiTro === 3 || user?.tenVaiTro === 'NguoiDan';
  },
};

export default authHelper;
