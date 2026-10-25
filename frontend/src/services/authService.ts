import { post } from './api';
import { session, type Session } from './session';
import type { User } from '../types/User';

export const authService = {
    async login(tenDangNhap: string, matKhau: string, donVi: string): Promise<User> {
        const res = await post<User & { token?: string }>('/auth/login', { tenDangNhap, matKhau });
        const { token, ...user } = res;
        session.set({ user: user as User, donVi, token });
        return user as User;
    },
    getSession(): Session | null {
        return session.get();
    },
    /** JWT stateless: server không giữ phiên, nên logout = xóa token phía client.
     *  Có gọi /auth/logout (nếu server có) theo kiểu best-effort, lỗi cũng bỏ qua. */
    async logout(): Promise<void> {
        const had = session.get();
        session.clear();                       // xóa trước để mọi request sau đó đều không còn token
        if (had?.token) {
            try {
                await fetch(`${import.meta.env.VITE_API_URL || 'https://localhost:7196/api/v1'}/auth/logout`, {
                    method: 'POST',
                    headers: { Authorization: `Bearer ${had.token}` },
                    keepalive: true,
                });
            } catch { /* bỏ qua */ }
        }
    },
};
