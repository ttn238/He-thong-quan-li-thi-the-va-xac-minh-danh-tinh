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
    if (!user) return false;
    return user.maVaiTro === 1 ||
           user.maCodeVaiTro === 'ADMIN' ||
           user.tenVaiTro?.includes('Quản trị') ||
           user.tenVaiTro === 'QuanTriVien';
  },

  isOfficer(): boolean {
    const user = this.getCurrentUser();
    if (!user) return false;
    return user.maVaiTro === 2 ||
           user.maCodeVaiTro === 'CONG_AN' ||
           user.tenVaiTro?.includes('Công an') ||
           user.tenVaiTro === 'CanBoGiamDinh';
  },

  isApprover(): boolean {
    const user = this.getCurrentUser();
    if (!user) return false;
    return user.maVaiTro === 3 ||
           user.maCodeVaiTro === 'CONG_AN_DUYET';
  },

  isMorgue(): boolean {
    const user = this.getCurrentUser();
    if (!user) return false;
    return user.maVaiTro === 4 ||
           user.maCodeVaiTro === 'NHA_XAC' ||
           user.tenVaiTro?.includes('nhà xác');
  },

  isCitizen(): boolean {
    const user = this.getCurrentUser();
    if (!user) return false;
    return user.maVaiTro === 5 ||
           user.maCodeVaiTro === 'NGUOI_DAN' ||
           user.tenVaiTro?.includes('Người dân') ||
           user.tenVaiTro === 'NguoiDan';
  },

  hasRole(roles: (string | number)[]): boolean {
    const user = this.getCurrentUser();
    if (!user) return false;

    return roles.some((r) => {
      if (typeof r === 'number') return user.maVaiTro === r;
      return user.maCodeVaiTro === r || user.tenVaiTro === r;
    });
  },
};

export default authHelper;
