import React, { useState, useEffect, useCallback } from 'react';
import { Role, Permission } from '../../types/Role';
import roleService from '../../services/roleService';
import authHelper from '../../features/auth/authHelper';

const ROLE_COLORS: Record<string, { bg: string; text: string; border: string; icon: string }> = {
  ADMIN: { bg: 'bg-red-50', text: 'text-red-700', border: 'border-red-200', icon: '🛡️' },
  CONG_AN: { bg: 'bg-blue-50', text: 'text-blue-700', border: 'border-blue-200', icon: '👮' },
  CONG_AN_DUYET: { bg: 'bg-purple-50', text: 'text-purple-700', border: 'border-purple-200', icon: '⚖️' },
  NHA_XAC: { bg: 'bg-amber-50', text: 'text-amber-700', border: 'border-amber-200', icon: '🏥' },
  NGUOI_DAN: { bg: 'bg-emerald-50', text: 'text-emerald-700', border: 'border-emerald-200', icon: '👤' },
};

export const RoleManagement: React.FC = () => {
  const [roles, setRoles] = useState<Role[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Modal xem chi tiết quyền của vai trò
  const [selectedRole, setSelectedRole] = useState<Role | null>(null);
  const [permissionModalOpen, setPermissionModalOpen] = useState(false);
  const [permissionLoading, setPermissionLoading] = useState(false);

  const isAdmin = authHelper.isAdmin();

  const fetchRoles = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await roleService.getRoles();
      setRoles(data);
    } catch (err: any) {
      setError(err.message || 'Không thể tải danh sách vai trò.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchRoles();
  }, [fetchRoles]);

  const handleViewPermissions = async (role: Role) => {
    setPermissionLoading(true);
    setPermissionModalOpen(true);
    try {
      const details = await roleService.getRoleById(role.maVaiTro);
      setSelectedRole(details);
    } catch (err: any) {
      alert('Không thể tải chi tiết quyền: ' + err.message);
      setPermissionModalOpen(false);
    } finally {
      setPermissionLoading(false);
    }
  };

  return (
    <div className="p-6 bg-slate-50 min-h-screen">
      {/* Tiêu đề trang */}
      <div className="mb-6 flex flex-col md:flex-row md:items-center md:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-800 tracking-tight">
            QUẢN LÝ VAI TRÒ & PHÂN QUYỀN (ROLES & RBAC PERMISSIONS)
          </h1>
          <p className="text-sm text-slate-500 mt-1">
            Quản trị 5 vai trò hệ thống, ma trận đặc quyền bảo mật và phân bổ thẩm quyền người dùng
          </p>
        </div>
        <div className="flex items-center gap-2">
          <button
            onClick={fetchRoles}
            className="px-4 py-2 bg-white border border-slate-300 rounded-lg text-sm font-semibold text-slate-700 hover:bg-slate-50 shadow-sm transition"
          >
            🔄 Tải lại dữ liệu
          </button>
        </div>
      </div>

      {/* Thông báo lỗi nếu có */}
      {error && (
        <div className="mb-6 p-4 bg-red-50 border border-red-200 text-red-700 text-sm rounded-lg flex items-center justify-between">
          <span>⚠️ {error}</span>
          <button onClick={fetchRoles} className="underline font-semibold ml-2">Thử lại</button>
        </div>
      )}

      {/* Thẻ danh sách 5 Vai trò */}
      {loading ? (
        <div className="flex justify-center items-center py-20 text-slate-500">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-slate-700 mr-3"></div>
          Đang tải danh sách vai trò...
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {roles.map((r) => {
            const style = ROLE_COLORS[r.maCode] || { bg: 'bg-slate-50', text: 'text-slate-700', border: 'border-slate-200', icon: '🔑' };
            return (
              <div
                key={r.maVaiTro}
                className={`bg-white rounded-xl border ${style.border} shadow-sm hover:shadow-md transition p-6 flex flex-col justify-between`}
              >
                <div>
                  <div className="flex items-start justify-between mb-3">
                    <span className="text-3xl">{style.icon}</span>
                    <span className={`px-2.5 py-1 rounded-full text-xs font-bold border ${style.bg} ${style.text} ${style.border}`}>
                      {r.maCode}
                    </span>
                  </div>

                  <h3 className="text-lg font-bold text-slate-800 mb-1">{r.tenVaiTro}</h3>
                  <p className="text-xs text-slate-500 mb-4 line-clamp-2">{r.moTa}</p>

                  <div className="flex items-center justify-between py-2.5 px-3 bg-slate-50 rounded-lg text-xs text-slate-600 mb-4">
                    <span className="font-medium">Số người dùng đang giữ:</span>
                    <span className="font-bold text-slate-900 bg-white px-2 py-0.5 rounded border border-slate-200 shadow-2xs">
                      {r.userCount} tài khoản
                    </span>
                  </div>
                </div>

                <div className="pt-2 border-t border-slate-100 flex items-center justify-end">
                  <button
                    onClick={() => handleViewPermissions(r)}
                    className="w-full py-2 px-3 bg-slate-900 hover:bg-slate-800 text-white rounded-lg text-xs font-semibold shadow-sm transition flex items-center justify-center gap-1.5"
                  >
                    <span>📋 Xem ma trận quyền hạn</span>
                  </button>
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* Modal Xem chi tiết Ma trận phân quyền của vai trò */}
      {permissionModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
          <div className="bg-white rounded-xl shadow-xl w-full max-w-4xl max-h-[85vh] flex flex-col overflow-hidden animate-fadeIn">
            {/* Header Modal */}
            <div className="px-6 py-4 border-b border-slate-200 flex items-center justify-between bg-slate-900 text-white">
              <div>
                <h3 className="text-lg font-bold">
                  MA TRẬN QUYỀN HẠN: {selectedRole?.tenVaiTro?.toUpperCase()} ({selectedRole?.maCode})
                </h3>
                <p className="text-xs text-slate-300 mt-0.5">
                  Chi tiết các quyền (Resource x Action) và phạm vi dữ liệu cho phép (PhamVi)
                </p>
              </div>
              <button
                onClick={() => setPermissionModalOpen(false)}
                className="text-slate-400 hover:text-white text-xl font-bold w-8 h-8 flex items-center justify-center rounded-lg hover:bg-slate-800"
              >
                ✕
              </button>
            </div>

            {/* Body Modal */}
            <div className="p-6 overflow-y-auto flex-1">
              {permissionLoading ? (
                <div className="flex justify-center items-center py-16 text-slate-500">
                  <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-slate-700 mr-3"></div>
                  Đang tải ma trận quyền...
                </div>
              ) : selectedRole?.permissions && selectedRole.permissions.length > 0 ? (
                <div className="overflow-x-auto">
                  <table className="w-full text-left text-sm border-collapse">
                    <thead>
                      <tr className="bg-slate-100 text-slate-700 uppercase text-xs tracking-wider">
                        <th className="py-2.5 px-4 border-b">Tài nguyên (Resource)</th>
                        <th className="py-2.5 px-4 border-b">Hành vi (Action)</th>
                        <th className="py-2.5 px-4 border-b">Mã quyền</th>
                        <th className="py-2.5 px-4 border-b text-center">Phạm vi dữ liệu (Scope)</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100">
                      {selectedRole.permissions.map((p) => (
                        <tr key={p.maQuyen} className="hover:bg-slate-50 transition">
                          <td className="py-2 px-4 font-semibold text-slate-800">{p.resource}</td>
                          <td className="py-2 px-4 text-slate-600">
                            <span className="font-mono text-xs bg-slate-100 px-2 py-0.5 rounded border border-slate-200">
                              {p.action}
                            </span>
                          </td>
                          <td className="py-2 px-4 text-xs text-slate-500">{p.tenQuyen}</td>
                          <td className="py-2 px-4 text-center">
                            <span
                              className={`px-2 py-0.5 rounded-full text-xs font-bold ${
                                p.phamVi === 'ALL'
                                  ? 'bg-emerald-100 text-emerald-800 border border-emerald-200'
                                  : p.phamVi === 'OWN'
                                  ? 'bg-blue-100 text-blue-800 border border-blue-200'
                                  : 'bg-amber-100 text-amber-800 border border-amber-200'
                              }`}
                            >
                              {p.phamVi || 'ALL'}
                            </span>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              ) : (
                <div className="text-center py-12 text-slate-500">
                  Vai trò này chưa có quyền hạn nào được phân bổ trong hệ thống.
                </div>
              )}
            </div>

            {/* Footer Modal */}
            <div className="px-6 py-3 border-t border-slate-200 bg-slate-50 flex items-center justify-between">
              <span className="text-xs text-slate-500">
                Tổng cộng: <strong>{selectedRole?.permissions?.length || 0}</strong> đặc quyền được kích hoạt
              </span>
              <button
                onClick={() => setPermissionModalOpen(false)}
                className="px-4 py-2 bg-slate-200 hover:bg-slate-300 text-slate-800 rounded-lg text-sm font-semibold transition"
              >
                Đóng
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default RoleManagement;
