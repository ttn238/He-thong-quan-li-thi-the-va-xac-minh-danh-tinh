import { useAuth } from '../../hooks/useAuth';
import LogoutButton from '../../components/common/LogoutButton';
import { Link } from 'react-router-dom';

export default function UserDashboard() {
    const { user, donVi } = useAuth();
    if (!user) return null;

    return (
        <div className="p-8 max-w-4xl mx-auto">
            <div className="bg-white rounded-xl shadow-xs border border-slate-200 p-6 mb-6">
                <h2 className="text-xl font-bold text-slate-900 mb-2">Đăng nhập thành công</h2>
                <div className="text-sm text-slate-600 space-y-1">
                    <div><span className="font-semibold text-slate-700">Họ và tên:</span> {user.hoTen}</div>
                    <div><span className="font-semibold text-slate-700">Vai trò:</span> <span className="inline-block px-2.5 py-0.5 rounded-full text-xs font-semibold bg-blue-50 text-blue-700">{user.tenVaiTro}</span></div>
                    {donVi && <div><span className="font-semibold text-slate-700">Đơn vị:</span> {donVi}</div>}
                </div>
                <div className="mt-4">
                    <LogoutButton style={{ padding: '8px 20px' }} />
                </div>
            </div>

            <h3 className="text-sm font-semibold uppercase tracking-wider text-slate-500 mb-3">Phân hệ nghiệp vụ</h3>
            <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-4">
                <Link
                    to="/storage"
                    className="p-5 bg-white rounded-xl border border-slate-200 hover:border-blue-500 hover:shadow-md transition group block"
                >
                    <div className="text-2xl mb-2">🏢</div>
                    <div className="font-bold text-slate-900 group-hover:text-blue-600 transition">Quản lý Vị trí Lưu trữ</div>
                    <div className="text-xs text-slate-500 mt-1">Danh mục phân cấp Khu - Tủ - Ngăn lưu trữ thi thể</div>
                </Link>

                <Link
                    to="/users"
                    className="p-5 bg-white rounded-xl border border-slate-200 hover:border-blue-500 hover:shadow-md transition group block"
                >
                    <div className="text-2xl mb-2">👥</div>
                    <div className="font-bold text-slate-900 group-hover:text-blue-600 transition">Quản trị Người dùng</div>
                    <div className="text-xs text-slate-500 mt-1">Quản lý tài khoản, trạng thái và cấp quyền</div>
                </Link>

                <Link
                    to="/roles"
                    className="p-5 bg-white rounded-xl border border-slate-200 hover:border-blue-500 hover:shadow-md transition group block"
                >
                    <div className="text-2xl mb-2">🛡️</div>
                    <div className="font-bold text-slate-900 group-hover:text-blue-600 transition">Quản lý Vai trò & RBAC</div>
                    <div className="text-xs text-slate-500 mt-1">5 vai trò hệ thống và ma trận đặc quyền bảo mật</div>
                </Link>
            </div>
        </div>
    );
}
