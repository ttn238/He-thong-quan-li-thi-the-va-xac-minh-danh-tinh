import type { ReactNode } from 'react';
import { Navigate, Route, Routes } from 'react-router-dom';
import Login from '../pages/auth/Login';
import UserDashboard from '../pages/dashboard/UserDashboard';
import UserManagement from '../pages/users/UserManagement';
import RoleManagement from '../pages/roles/RoleManagement';
import { authService } from '../services/authService';

function RequireAuth({ children }: { children: ReactNode }) {
    return authService.getSession() ? <>{ children } </> : <Navigate to="/login" replace />;
}

// Guard chặn người dùng không có vai trò Quản trị viên (ADMIN) truy cập trang cấu hình vai trò
function AdminRoute({ children }: { children: ReactNode }) {
    const session = authService.getSession();
    if (!session) {
        return <Navigate to="/login" replace />;
    }
    if (!authService.isAdmin()) {
        return <Navigate to="/dashboard" replace />;
    }
    return <>{children}</>;
}

export default function AppRoutes() {
    return (
        <Routes>
            <Route path="/login" element={<Login />} />
            <Route path="/dashboard" element={<RequireAuth><UserDashboard /></RequireAuth>} />
            <Route path="/users" element={<AdminRoute><UserManagement /></AdminRoute>} />
            <Route path="/admin/users" element={<AdminRoute><UserManagement /></AdminRoute>} />
            <Route path="/roles" element={<AdminRoute><RoleManagement /></AdminRoute>} />
            <Route path="/admin/roles" element={<AdminRoute><RoleManagement /></AdminRoute>} />
            <Route path="*" element={<Navigate to="/login" replace />} />
        </Routes>
    );
}
