import { useCallback, useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { authService } from '../services/authService';
import { session } from '../services/session';

/** Trạng thái đăng nhập + logout. Tự đưa về /login khi phiên hết hạn (401 hoặc token quá hạn). */
export function useAuth() {
    const nav = useNavigate();
    const [current, setCurrent] = useState(() => authService.getSession());
    const loggingOut = useRef(false);

    const toLogin = useCallback(() => {
        setCurrent(null);
        nav('/login', { replace: true });
    }, [nav]);

    const logout = useCallback(async () => {
        if (loggingOut.current) return;        // bấm nhiều lần chỉ xử lý 1 lần
        loggingOut.current = true;
        try {
            await authService.logout();
        } finally {
            loggingOut.current = false;
            toLogin();
        }
    }, [toLogin]);

    useEffect(() => {
        const onUnauthorized = () => toLogin();
        window.addEventListener('auth:unauthorized', onUnauthorized);

        // Hẹn giờ đúng lúc token hết hạn
        const exp = session.expiresAt();
        let timer: ReturnType<typeof setTimeout> | undefined;
        if (exp) timer = setTimeout(toLogin, Math.min(Math.max(exp - Date.now(), 0), 2 ** 31 - 1));

        // Đăng xuất ở tab khác (sessionStorage không chia sẻ giữa tab, nhưng giữ cho chắc)
        const onVisible = () => { if (!authService.getSession()) toLogin(); };
        document.addEventListener('visibilitychange', onVisible);

        return () => {
            window.removeEventListener('auth:unauthorized', onUnauthorized);
            document.removeEventListener('visibilitychange', onVisible);
            if (timer) clearTimeout(timer);
        };
    }, [toLogin]);

    return { user: current?.user ?? null, donVi: current?.donVi ?? '', isAuthenticated: !!current, logout };
}
