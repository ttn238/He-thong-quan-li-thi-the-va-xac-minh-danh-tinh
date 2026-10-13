import { post } from './api';
import type { User } from '../types/User';

const KEY = 'qltldt.session';

export const authService = {
    async login(tenDangNhap: string, matKhau: string, donVi: string): Promise<User> {
        const user = await post<User>('/auth/login', { tenDangNhap, matKhau });
        sessionStorage.setItem(KEY, JSON.stringify({ user, donVi }));
        return user;
    },
    getSession(): { user: User; donVi: string } | null {
        const raw = sessionStorage.getItem(KEY);
        return raw ? JSON.parse(raw) : null;
    },
    logout() {
        sessionStorage.removeItem(KEY);
    },
};