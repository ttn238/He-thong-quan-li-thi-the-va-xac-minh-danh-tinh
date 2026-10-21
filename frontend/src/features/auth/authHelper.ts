import type { User } from '../../types/User';
import { session } from '../../services/session';

export const authHelper = {
  getToken(): string | null {
    return session.get()?.token ?? null;
  },

  getCurrentUser(): User | null {
    return session.get()?.user ?? null;
  },

  clearAuth(): void {
    session.clear();
  },

  isAuthenticated(): boolean {
    return !!session.get();
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
