import type { User } from '../types/User';

const KEY = 'qltldt.session';

export interface Session {
    user: User;
    donVi: string;
    token?: string;
}

function decodeExp(token?: string): number | null {
    if (!token) return null;
    try {
        const payload = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
        const exp = JSON.parse(atob(payload)).exp;
        return typeof exp === 'number' ? exp * 1000 : null;
    } catch {
        return null;
    }
}

export const session = {
    get(): Session | null {
        try {
            const raw = sessionStorage.getItem(KEY);
            if (!raw) return null;
            const s = JSON.parse(raw) as Session;
            const exp = decodeExp(s.token);
            if (exp !== null && exp <= Date.now()) {   // token hết hạn => coi như chưa đăng nhập
                sessionStorage.removeItem(KEY);
                return null;
            }
            return s;
        } catch {
            sessionStorage.removeItem(KEY);
            return null;
        }
    },
    set(s: Session) {
        sessionStorage.setItem(KEY, JSON.stringify(s));
    },
    clear() {
        sessionStorage.removeItem(KEY);
    },
    expiresAt(): number | null {
        return decodeExp(this.get()?.token);
    },
};
