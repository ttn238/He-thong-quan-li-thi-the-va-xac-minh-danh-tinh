import { useAuth } from '../../hooks/useAuth';
import LogoutButton from '../../components/common/LogoutButton';

export default function UserDashboard() {
    const { user, donVi } = useAuth();
    if (!user) return null;
    return (
        <div style={{ padding: 30, fontSize: 16, lineHeight: 1.7 }}>
            <b>Đăng nhập thành công</b>
            <div>Xin chào: {user.hoTen}</div>
            <div>Vai trò: {user.tenVaiTro}</div>
            <div>Đơn vị: {donVi}</div>
            <LogoutButton style={{ marginTop: 20, padding: '8px 20px' }} />
        </div>
    );
}
