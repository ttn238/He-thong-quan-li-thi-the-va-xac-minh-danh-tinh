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
