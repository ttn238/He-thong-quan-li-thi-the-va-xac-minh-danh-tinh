import { post, ApiClient, AUTH_SESSION_KEY, AUTH_USER_KEY } from './api';
import type { User } from '../types/User';

export const authService = {
    async login(tenDangNhap: string, matKhau: string, donVi: string): Promise<User> {
        const user = await post<User>('/auth/login', { tenDangNhap, matKhau });

        // Lưu thông tin người dùng và token thống nhất vào storage
        const token = (user as any).token || (user as any).Token || `Bearer-${user.maCodeVaiTro || user.tenVaiTro || 'ADMIN'}`;
        ApiClient.setToken(token);

        localStorage.setItem(AUTH_USER_KEY, JSON.stringify(user));
        sessionStorage.setItem(AUTH_SESSION_KEY, JSON.stringify({ user, donVi }));

        return user;
    },

    getSession(): { user: User; donVi: string } | null {
        const raw = sessionStorage.getItem(AUTH_SESSION_KEY) || localStorage.getItem(AUTH_SESSION_KEY);
        if (raw) {
            try { return JSON.parse(raw); } catch { }
        }
        const userRaw = localStorage.getItem(AUTH_USER_KEY);
        if (userRaw) {
            try {
                const user = JSON.parse(userRaw);
                return { user, donVi: user.donVi || '' };
            } catch { }
        }
        return null;
    },

    getCurrentUser(): User | null {
        return this.getSession()?.user || null;
    },

    isAdmin(): boolean {
        const user = this.getCurrentUser();
        if (!user) return false;
        const code = user.maCodeVaiTro || (user as any).roleCode || '';
        return code.toUpperCase() === 'ADMIN';
    },

    logout() {
        ApiClient.clearAuth();
    },
};
