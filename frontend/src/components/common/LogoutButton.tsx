import { useState } from 'react';
import { useAuth } from '../../hooks/useAuth';

export default function LogoutButton({ className, style }: { className?: string; style?: React.CSSProperties }) {
    const { logout } = useAuth();
    const [busy, setBusy] = useState(false);
    return (
        <button
            type="button"
            className={className}
            style={style}
            disabled={busy}
            onClick={async () => { setBusy(true); await logout(); }}
        >
            {busy ? 'ĐANG ĐĂNG XUẤT...' : 'ĐĂNG XUẤT'}
        </button>
    );
}
