import type { ReactNode } from 'react';
import { Navigate, Route, Routes } from 'react-router-dom';
import Login from '../pages/auth/Login';
import UserDashboard from '../pages/dashboard/UserDashboard';
import AiResultPage from '../pages/identification/AiResultPage';
import AiResultPage from '../pages/identification/AiResultPage';
import UserManagement from '../pages/users/UserManagement';
import RoleManagement from '../pages/roles/RoleManagement';
import { authService } from '../services/authService';

function RequireAuth({ children }: { children: ReactNode }) {
    return authService.getSession() ? <>{ children } </> : <Navigate to="/login" replace />;
}

export default function AppRoutes() {
    return (
        <Routes>
            <Route path = "*" element = {< Navigate to = "/login" replace />} />
            <Route path= "/login" element = {< Login />} />
            <Route path = "/dashboard" element = {< RequireAuth > <UserDashboard /></RequireAuth >} />
            <Route path = "/identification/ai-result" element = {< RequireAuth > <AiResultPage /></RequireAuth >} />
            <Route path = "/identification/ai-result/:requestId" element = {< RequireAuth > <AiResultPage /></RequireAuth >} />
            <Route path="/users" element={<RequireAuth><UserManagement /></RequireAuth>} />
            <Route path="/admin/users" element={<RequireAuth><UserManagement /></RequireAuth>} />
            <Route path="/roles" element={<RequireAuth><RoleManagement /></RequireAuth>} />
            <Route path="/admin/roles" element={<RequireAuth><RoleManagement /></RequireAuth>} />
            <Route path="*" element={<Navigate to="/login" replace />} />
        </Routes>
    );
}