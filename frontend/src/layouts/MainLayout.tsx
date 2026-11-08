import type { ReactNode } from 'react';
import { useAuth } from '../hooks/useAuth';
import LogoutButton from '../components/common/LogoutButton';

export default function MainLayout({ children }: { children?: ReactNode }) {
    const { user } = useAuth();
    return (
        <div>
            <header style={{ display: 'flex', justifyContent: 'flex-end', gap: 12, padding: 12, alignItems: 'center' }}>
                <span>{user?.hoTen}</span>
                <LogoutButton />
            </header>
            <main>{children}</main>
        </div>
    );
}
