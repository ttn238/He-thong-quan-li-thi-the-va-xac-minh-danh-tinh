import type { ReactNode } from 'react';
import { Navigate, Route, Routes } from 'react-router-dom';
import Login from '../pages/auth/Login';
import UserDashboard from '../pages/dashboard/UserDashboard';
import { authService } from '../services/authService';

function RequireAuth({ children }: { children: ReactNode }) {
    return authService.getSession() ? <>{ children } </> : <Navigate to="/login" replace />;
}

export default function AppRoutes() {
    return (
        <Routes>
        <Route path= "/login" element = {< Login />} />
            < Route path = "/dashboard" element = {< RequireAuth > <UserDashboard /></RequireAuth >} />
                < Route path = "*" element = {< Navigate to = "/login" replace />} />
                    </Routes>
  );
}